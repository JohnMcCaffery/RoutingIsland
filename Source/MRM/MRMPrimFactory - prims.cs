/*************************************************************************
Copyright (c) 2012 John McCaffery 

This file is part of Routing Project.

Routing Project is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Routing Project is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Routing Project.  If not, see <http://www.gnu.org/licenses/>.

**************************************************************************/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using OpenMetaverse;
using OpenSim.Region.OptionalModules.Scripting.Minimodule;
using common.framework.interfaces.basic;
using common.interfaces.entities;
using Diagrams.Common;
using common;
using log4net;
using Diagrams.Common.interfaces.keytable;
using common.Queue;
using System.Threading;
using MRM;
using common.config;
using Nini.Config;
using Diagrams.MRM;
using JM726.Lib.Static;
using Diagrams.MRM.Controls.Buttons;
using MRMChatEventArgs=OpenSim.Region.OptionalModules.Scripting.Minimodule.ChatEventArgs;
//using ChatEventArgs=Diagrams.ChatEventArgs;

namespace Diagrams {
    public partial class MRMPrimFactory : IPrimFactory, IMRMPrimFactory {
        private const int MAX_RECYCLED_PRIMS = 10;
        public readonly int CheckWait = 1000;

        internal IWorld World {
            get { return _world; }
        }

        private ILog _logger;

        private IWorld _world;
        private IHost _host;

        private readonly Dictionary<UUID, IObject> _freeObjects;
        private readonly IKeyTable<IPrim> _prims;
        private readonly List<UUID> _createdPrims;
        private readonly IAsynchQueue _factoryQ;

        private Thread _checkThread;

        private IPrim _hostPrim;
        private bool _cont;
        private bool _recycle;


        #region IPrimFactory Members

        public event ChatDelegate OnChat;

        public IPrim this[UUID id] {
            get {
                if (!PrimExists(id))
                    throw new Exception("Unable to look up primitive. Given ID does not resolve to a known primitive.");
                else if (_prims.ContainsKey(id))
                    return _prims[id];
                return new MRMPrim(id, this);
            }
        } 

        public IPrim this[string name] {
            get {
                if (!PrimExists(name))
                    throw new Exception("Unable to look up primitive. " + name + " does not resolve to a known primitive.");
                IEnumerable<IPrim> primsWithName = _prims.Where(prim => prim.Name.Equals(name));
                if (primsWithName.Count() > 1)
                    throw new Exception("Unable to look up primitive. The are more than one primitives called " + name + ". Use GetPrimsWithName() instead.");

                return primsWithName.First();
            }
        }

        public string Owner {
            get { return _world.Avatars.First(avatar => avatar.GlobalID.Equals(_hostPrim.ID)).Name; }
        }

        #endregion

        private OnChatDelegate _chatListener;

        private Dictionary<int, List<ChatDelegate>> _chatListeners;

        public void AddChannelListener(int channel, ChatDelegate chatListener) {
            _factoryQ.QWork("Add chat listener to channel " + channel + ".", () => {
                lock (_chatListeners) {
                    if (!_chatListeners.ContainsKey(channel))
                        _chatListeners.Add(channel, new List<ChatDelegate>());
                    _chatListeners[channel].Add(chatListener);
                }
            });
        }

        public void RemoveChannelListener(int channel, ChatDelegate chatListener) {
            _factoryQ.QWork("Remove chat listener to channel " + channel + ".", () => {
                lock (_chatListeners) {
                    if (_chatListeners.ContainsKey(channel)) {
                        _chatListeners[channel].Remove(chatListener);
                        if (_chatListeners[channel].Count == 0)
                            _chatListeners.Remove(channel);
                    }
                }
            });
        }

        public MRMPrimFactory(IHost host, IWorld world, IAsynchQueueFactory queueFactory, IKeyTableFactory tableFactory, IConfigSource config, UUID hostID) {
            IConfig mrmConfig = config.Configs["MRM"];
            if (mrmConfig == null)
                mrmConfig = config.Configs[0];

            _world = world;
            _host = host;

            _logger = LogManager.GetLogger(typeof(MRMPrimFactory));
            _prims = new MapKeyTable<IPrim>();
            _createdPrims = new List<UUID>();
            _freeObjects = new Dictionary<UUID, IObject>();
            _chatListeners = new Dictionary<int, List<ChatDelegate>>();

            //_factoryQ = queueFactory.MakeQueue();
            _factoryQ = queueFactory.SharedQueue;

             CheckWait = mrmConfig.GetInt("CheckWait", CheckWait);
             _recycle = mrmConfig.GetBoolean("Recycle", true);

             try {
                 _hostPrim = new MRMPrim(hostID, this);
             } catch (Exception e) {
                 _hostPrim = null;
                 throw new Exception("Problem getting Host Prim: " + e.Message + "\n" + e.StackTrace);
             }

             _chatListener += (sender, args) => {
                 _world = sender;
                 if (_chatListeners.ContainsKey(args.Channel))
                     lock (_chatListeners)
                         foreach(var listener in _chatListeners[args.Channel])
                             listener(args.Sender.Name, args.Sender.GlobalID, args.Text, args.Channel);
                 if (OnChat != null)
                     OnChat(args.Sender.Name, args.Sender.GlobalID, args.Text, args.Channel);
             };
             _world.OnChat += _chatListener;

             _checkThread = new Thread(CheckThread);
             _checkThread.Name = "MRMPrimFactory Check Thread";
             _checkThread.Start();


            


            _linkButtons = new Dictionary<uint, TouchButton>();
            _chatButtons = new Dictionary<UUID, TouchButton>();

            _knowButtons = new Dictionary<string,HashSet<UUID>>();
            _pingChannel = mrmConfig.GetInt("PingChannel", -50);
            _ping = mrmConfig.Get("ButtonPing", "Ping");
            _pingAck = mrmConfig.Get("ButtonPingAck", "Pong");
            _chanAck = mrmConfig.Get("ButtonChannelAck", "ACK");
            InitTouchButtons();
        }

        public IEnumerable<IPrim> AllPrims {
            get {
                return _world.Objects.Select<IObject, IPrim>(obj => obj != null && obj.Exists ? new MRMPrim(obj.GlobalID, this) : null);
            }
        }

        #region MRMPrimFactory

        #endregion

        #region WorldFactory

        public bool RemovePrim(UUID id) {
            if (!_prims.ContainsKey(id)) return false;

            IPrim prim = _prims[id];
            if (prim is MRMPrim)
                ((MRMPrim) prim).RemoveListeners();
            prim.Editable = true;
            IObject obj = GetIObject(id);
            if (obj != null && obj.Exists)
                obj.Scale = Vector3.Zero;
            _logger.Debug("Queued " + prim.Name + " to be removed.");
            
            _factoryQ.QWork("Remove Primitive", () => {
                if (!_prims.ContainsKey(id))
                    return;
                lock (_prims) 
                    _prims.Remove(id);

                if (_recycle && _freeObjects.Count < MAX_RECYCLED_PRIMS) {
                    _freeObjects.Add(id, GetIObject(id));
                    _logger.Info("Recycled " + prim.Name + ".");
                } else 
                    WorldRemove(id);
            });
            return true;
        }
        
        public IObject RenewObject(UUID id, Vector3 pos) {
            if (!_prims.ContainsKey(id))
                return null;
            IObject obj = _world.Objects.Create(pos);
            _createdPrims.Add(obj.GlobalID);
            lock (_prims)
                _prims[obj.GlobalID] = _prims[id];
            _createdPrims.Remove(id);
            return obj;
        }

        #endregion

        #region PrimFactory

        public void Shutdown() {
            if (!_factoryQ.IsRunning) {
                _logger.Debug("Unable to shut down prim factory. It has already been stopped.");
                return;
            }
            _world.OnChat -= _chatListener;

            //ClearAll();
            DeleteRecycled();

            _cont = false;
            Util.Join(_checkThread);
            _logger.Info("MRMPrimFactory shut down.");
        }

        public void ClearCreated() {
            _logger.Debug("Removing all created prims.");

            bool recycle = _recycle;
            _recycle = true;
            _factoryQ.Paused = true;

            lock (_createdPrims)
                foreach (UUID prim in _createdPrims)
                    RemovePrim(prim);
            _logger.Debug("Queued " + _factoryQ.QueuedItems + " items for removal.");

            _factoryQ.Paused = false;
            _factoryQ.BlockWhileWorking();
            _logger.Info("Removed all created prims.");
            _recycle = recycle;
        }

        public void DeleteRecycled() {
            lock (_freeObjects) {
                if (_freeObjects.Count > 0) {
                    _logger.Debug("Deleting all recycled prims.");
                    foreach (IObject recycledPrim in _freeObjects.Values)
                        if (recycledPrim.Exists)
                            WorldRemove(recycledPrim.GlobalID);

                    _freeObjects.Clear();
                    _logger.Info("Deleted all recycled prims.");
                } else
                    _logger.Info("No recycled prims exist to delete.");
            }
        }

        public bool PrimExists(UUID id) {
            if (_prims.ContainsKey(id))
                return true;
            if (_freeObjects.ContainsKey(id))
                return false;

            try {
                IObject tmp = _world.Objects[id];
                return tmp != null && tmp.Exists;
            } catch (Exception e) {
                return false;
            }
        }

        public bool PrimExists(string name) {
            return CheckWorldForName(name);
        }

        public List<IPrim> GetPrimsWithName(string name) {
            CheckWorldForName(name);
            return new List<IPrim>(_prims.Where(prim => prim.Name.Equals(name)));
        }

        public IPrim MakePrim(string name, Vector3 position, Color colour = default(Color), Vector3 scale = default(Vector3), PrimType shape = PrimType.Unknown, Quaternion rotation = default(Quaternion)) {
            _logger.Debug("Creating " + name + " at " + position + ".");
            IPrim prim = new MRMPrim(this, name, position, colour, scale, shape, rotation);
            _createdPrims.Add(prim.ID);
            _logger.Info("Created " + prim.Name + ".");
            return prim;
        }

        public IPrim Host {
            get { return _hostPrim; }
        }

        #endregion

        private void WorldRemove(UUID id) {
            string name = "To Remove";
            IObject rem = GetIObject(id);
            try {
                if (rem != null && rem.Exists && _world.Objects.Contains(rem)) {
                    name = rem.Name;
                    rem.Scale = Vector3.Zero;
                    _logger.Debug("Removing " + name + ".");
                    if (_world.Objects.Remove(rem))
                        _logger.Info("Removed " + name + ".");
                    else
                        _logger.Info("Unable to remove " + name + ".");
                }
            } catch (Exception e) {
                //DB.Exception(e, "Error removing " + name, Levels.MODEL);
                _logger.Debug("Error removing " + name + " - " + e.Message);
            }
        }

        /// <summary>
        ///   Get an Opensim IObject Object for the given global ID
        /// </summary>
        /// <param name = "id">The ID of the primitive to get</param>
        /// <returns>The IObject representing tied to the id</returns>
        internal IObject GetIObject(UUID id) {
            try {
                return _world.Objects[id];
            } catch (Exception e) {
                return null;
            }
        }

        private IObject CreateIObject(Vector3 pos) {
            IObject obj = null;
            //Try and recycle if there are any prims waiting to be re-used
            if (_recycle && _freeObjects.Count > 0) {
                foreach (KeyValuePair<UUID, IObject> testObj in new Dictionary<UUID, IObject>(_freeObjects)) {
                    _freeObjects.Remove(testObj.Key);
                    if (testObj.Value.Exists) {
                        obj = testObj.Value;
                        obj.WorldPosition = pos;
                        break;
                    }
                }
            }
            if (obj == null || !obj.Exists)
                obj = _world.Objects.Create(pos);
            return obj;
        }

        private bool CheckWorldForName(string name) {
            IObject tmp = null;
            //foreach (IObject obj in _world.Objects)
            foreach (IObject obj in _world.Objects.GetByName(name))
                if (obj.Exists && !_freeObjects.ContainsKey(obj.GlobalID)) {
                    tmp = obj;
                    new MRMPrim(tmp.GlobalID, this);
                }
            return tmp != null && tmp.Exists;
        }

        internal event Action Update;

        private void CheckThread() {
            _cont = true;
            while (_cont) {
                try {
                    if (Update != null)
                        Update();
                    Util.Wait(CheckWait);
                } catch (Exception e) {
                    _logger.Warn("Problem in MRMPrim factory check thread.", e);
                }
            }
        }

        public IObject RegisterPrim(IPrim prim, string name, Vector3 pos) {
            return RegisterPrim(prim, CreateIObject(pos), name);
        }

        public IObject RegisterPrim(IPrim prim, UUID id) {
            IObject obj = GetIObject(id);
            //If the prim is already registered
            if (_prims.ContainsKey(id) && obj != null)
                return obj;

            //If the prim does not exist in world
            if (obj == null || _freeObjects.ContainsKey(id)) {
                if (_prims.ContainsKey(id))
                    RemovePrim(id);
                throw new Exception("Unable to register primitive. The given ID does not exist in world. " + (obj == null ? "null" : "recycled"));
            }

            //If the prim exists in world but is not registered
            return RegisterPrim(prim, obj, obj.Name);
        }

        public void RegisterPrim(IPrim prim) {
            if (_prims.ContainsKey(prim.ID))
                return;
            lock (_prims)
                _prims.Add(prim.ID, prim);
        }

        private IObject RegisterPrim(IPrim prim, IObject obj, string name) {
            UUID id = obj.GlobalID;

            lock (_prims) 
                _prims.Add(id, prim);
            return obj;
        }
    }
}
