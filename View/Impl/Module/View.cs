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
using System.Drawing;
using OpenMetaverse;
using common.framework.impl.util;
using common.model.framework.interfaces;
using core.view.interfaces;
using Diagrams.Common.interfaces.keytable;
using common.Queue;
using common.interfaces.entities;
using core.view.impl.entities;
using common;
using common.config;
using System.Collections.Generic;
using log4net;
using common.framework.interfaces.entities;
using Nini.Config;
using Diagrams.Framework.Util;
using System.Linq;
using JM726.Lib.Static;
using common.framework.interfaces.basic;
using Diagrams.Common;
using System.Threading;
using System.Diagnostics;

namespace Diagrams {
    public class View : AbstractModule<IVNode, IVLink>, IView {
        private const int PACKETS_PER_THREAD = 5;
        internal static ILog PacketLogger = LogManager.GetLogger(typeof(View).FullName + ".Packet");

        private readonly IKeyTable<IEnumerable<UUID>> _displayingBoards;

        private readonly IPrimFactory _factory;
        private readonly IAsynchQueue _moveQ, _deliverQ;
        //private readonly SmartThreadPool _moveQ;
        private readonly float _waitMultiplier;
        private readonly int _displayChannel;
        private readonly bool _autoUpdate;

        private int _wait = 5;
        private bool _cont;

        private Thread _tickThread;

        internal event Action OnTick;

        #region IView Members

        public override int Wait {
            get { return _wait; }
            set { _wait = value; }
        }

        public override bool Paused {
            get {
                return false;
            }
            set { }
        }

        public View(IKeyTableFactory tableFactory, IAsynchQueueFactory queueFactory, IPrimFactory primFactory, IConfigSource config)
            : base(tableFactory, queueFactory) {
            
            _factory = primFactory;
            _displayingBoards = tableFactory.MakeKeyTable<IEnumerable<UUID>>();
            _moveQ = queueFactory.MakeQueue();
            _deliverQ = queueFactory.MakeQueue();
            //_moveQ = new SmartThreadPool(int.MaxValue, 30, 3);
            
            IConfig viewConfig = config.Configs["View"];
            if (viewConfig == null)
                viewConfig = config.Configs[0];

            _wait = viewConfig.GetInt("Wait", _wait);
            _waitMultiplier = viewConfig.GetFloat("WaitMultiplier", 5f);
            _displayChannel = viewConfig.GetInt("ListeningChannel", -43);
            _autoUpdate = viewConfig.GetBoolean("AutoUpdateTables", false);
            int tableResolution = viewConfig.GetInt("TableResolution", -1);
            if (tableResolution > 0)
                VNode.SetTableResolution(tableResolution);

            _moveQ.Start("View Move Queue"/*, viewConfig.GetInt("PacketsPerThread", PACKETS_PER_THREAD)*/);
            _deliverQ.Start("View Deliver Queue");
            //_moveQ.Start();

            VLink.MaxWidth = viewConfig.GetFloat("maxLinkWidth", VLink.MaxWidth);
            VLink.MinWidth = viewConfig.GetFloat("minLinkWidth", VLink.MinWidth);
            VPacket.MaxMovesPerUnit = viewConfig.GetInt("maxNumPacketMovesPerUnit", VPacket.MaxMovesPerUnit);
            VPacket.MinMovesPerUnit = viewConfig.GetInt("minNumPacketMovesPerUnit", VPacket.MinMovesPerUnit);
            VPacket.DefaultMovesPerUnit = viewConfig.GetInt("defaultNumPacketMovesPerUnit", VPacket.DefaultMovesPerUnit);

            _tickThread = new Thread(() => {
                _cont = true;
                int wait = (int)(Wait * _waitMultiplier);
                int tick = 0;
                while (_cont) {
                    Util.Wait(wait, _cont && wait > 0, this);
                    DateTime start = DateTime.Now;
                    if (_cont && OnTick != null)
                        OnTick();
                    wait = (int)((Wait * _waitMultiplier) - DateTime.Now.Subtract(start).TotalMilliseconds);
                    tick++;
                }
            });
            _tickThread.Name = "View Tick Thread";
            _tickThread.Start();

            Logger.Info("View started.");
        }

        #endregion

        protected override IVLink MakeLink(UUID from, UUID to, Parameters parameters, float weight, bool bidirectional) {

            Logger.Debug("Creating link between '" + GetNode(from).Name + "' and '" + GetNode(to).Name + "'.");
            VLink link = new VLink(_factory, GetNode(from), GetNode(to), weight, parameters, bidirectional);
            Logger.Info("Created  '" + link.Name + "'.");
            return link;
        }

        protected override IVNode MakeNode(string name, Parameters parameters, Vector3 position, Color colour) {
            Logger.Debug("Creating node '" + name + "' at '" + position + "'.");
            VNode node = new VNode(_factory, name, position, colour, parameters);
            Logger.Info("Created node '" + node.Name + "' at '" + position + "'.");
            return node;
        }

        public override void Clear() {
            List<UUID> list = new List<UUID>();
            ForAllNodes(node => list.Add(node.ID));
            Clear(list.ToArray());
            base.Clear();
        }

        public override void Clear(params UUID[] nodes) {
            foreach (var n in nodes) {
                if (IsNode(n)) {
                    foreach (var l in GetLinks(n))
                        l.SilentDestroy();
                    GetNode(n).Destroy();
                }
            }
            base.Clear(nodes);
        }

        #region IView Members

        public override void Stop() {
            Logger.Debug("View stopping.");
            _cont = false;
            Util.Wake(this);
            Util.Join(_tickThread);
            base.Stop();
            _moveQ.Stop();
            _deliverQ.Stop();
            Logger.Info("View stopped.");
        }

        public void Send(UUID from, UUID link, IPacket packet) {
            if (packet == null) {
                Logger.Debug("Unable to send null packet.");
                throw new Exception("Unable to send. Packet cannot be null.");
            } else if (!IsNode(from)/* || !GetNode(from).InWorld*/) {
                Logger.Debug("Unable to send " + packet.Name + ". From is not a valid node.");
                throw new Exception("Unable to send '" + packet.Name + "'. From is not a valid node.");
            } else if (!IsLink(link)/* || !GetLink(link).InWorld*/) {
                Logger.Debug("Unable to send " + packet.Name + ". Link is not a valid link.");
                throw new Exception("Unable to send '" + packet.Name + "'. Link is not a valid link.");
            } else if (!IsNodeLink(from, link)) {
                Logger.Debug("Unable to send '" + packet.Name + "'. " + GetLink(link).Name + " is not a link attached to " + GetNode(from).Name + ".");
                throw new Exception("Unable to send '" + packet.Name + "'. " + GetLink(link).Name + " is not a link attached to " + GetNode(from).Name + ".");
            }

            if (packet.Visualise) {
                PacketLogger.Debug("Forwarding '" + packet.Name + "' from '" + GetNode(from).Name + "' along '" + GetLink(link).Name + "' " + (packet.Visualise ? "" : "un") + "visualised.");
                sendPacketVisualise(from, link, packet);
            }  else {
                IVNode to = GetNode(GetLink(link).OtherEnd(from));
                PacketLogger.Debug("Delivering '" + packet.Name + "' to '" + to.Name + "' unvisualised.");
                _deliverQ.QWork("Deliver " + packet.Name + " to " + to.Name, () => {
                    to.PacketReceived(packet);
                    PacketLogger.Info("Delivered '" + packet.Name + "' to '" + to.Name + "' unvisualised.");
                }, true);
            }
        }

        private void sendPacketVisualise(UUID from, UUID l, IPacket packet) {
            IVPacket p = new VPacket(packet, GetNode(from), GetNode(GetLink(l).OtherEnd(from)), GetLink(l), _factory, _moveQ, this);
        }

        #endregion

        /// <summary>
        /// Trigger the node to display its forwarding table.
        /// </summary>
        /// <param name="node">The ID of the node to display the forwarding table for.</param>
        /// <param name="routes">The strings which represent each route the forwarding table knows of.</param>
        public void DisplayForwardingTable(UUID node, Route[] routes, params UUID[] boards) {
            if (!IsNode(node)) 
                return;
            _factory.Host.Say("Displaying forwarding table. " + boards.Length + " buttons selected for " + GetNode(node).Name + ".");
            foreach (UUID board in boards) {
                if (board == UUID.Zero)
                    continue;
                if (_displayingBoards.ContainsKey(board)) {
                    foreach (UUID oldNode in _displayingBoards[board])
                        if (!oldNode.Equals(node) && IsNode(oldNode))
                            GetNode(oldNode).RemoveBoard(board);
                    _displayingBoards[board] = _displayingBoards[board].Concat(new UUID[] { node });
                } else
                    _displayingBoards.Add(board, new UUID[] { node });
            }
            new Thread(() => {
                try {
                    GetNode(node).
                        DisplayForwardingTable(routes, boards.
                        Where(board => !board.Equals(UUID.Zero) && _factory.PrimExists(board)).
                        Select<UUID, IPrim>(board => _factory[board]));
                } catch (Exception e) {
                    Logger.Warn("Problem displaying board. " + e.Message);
                }
            }).Start();
        }

        /// <summary>
        /// Update the forwarding table which is being displayed.
        /// </summary>
        /// <param name="node">The ID of the node to update the forwarding table for.</param>
        /// <param name="routes">The strings which represent each route the forwarding table knows of.</param>
        public void UpdateForwardingTable(UUID node, Route[] routes) {
            if (!IsNode(node) || !_autoUpdate)
                return;
            GetNode(node).UpdateForwardingTable(routes);
        }
    }
}