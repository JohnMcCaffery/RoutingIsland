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
using System.Linq;
using System.Text;
using OpenMetaverse;
using common.framework.interfaces.basic;
using OpenSim.Region.OptionalModules.Scripting.Minimodule;
using MRMTouchEventArgs = OpenSim.Region.OptionalModules.Scripting.Minimodule.TouchEventArgs;
using System.Drawing;
using log4net;

namespace Diagrams.MRM.Controls.Buttons {
    public class TouchButton : IButton {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(MRMPrimFactory));
        private readonly string _name;
        private readonly MRMPrimFactory _factory;
        private readonly IPermissions _permissions;
        private readonly List<EntityTouchedDelegate> _listeners;
        private readonly HashSet<UUID> _tracked = new HashSet<UUID>();

        #region IButton Members

        public IEnumerable<IPrim> Prims {
            get { return _factory.ButtonPrims.ContainsKey(Name) ? _factory.ButtonPrims[Name].Select<UUID, IPrim>(id => _factory[id]) : new IPrim[0]; }
        }

        public virtual event EntityTouchedDelegate OnTouched {
            add {
                _listeners.Add(value);
            }
            remove {
                _listeners.Remove(value);
            }
        }

        public string Name {
            get {
                return _name;
            }
        }

        public TouchButton(MRMPrimFactory factory, string name, IPermissions permissions) {
            _name = name;
            _factory = factory;
            _listeners = new List<EntityTouchedDelegate>();
            _permissions = permissions;

            factory.OnButtonRegistered += (id, registeredName) => {
                if (registeredName.Equals(name))
                    RegisterPrim(factory.GetIObject(id));
            };
            if (factory.ButtonPrims.ContainsKey(name)) {
                IEnumerable<UUID> objects = factory.ButtonPrims[Name].ToArray();
                foreach (var obj in objects.Select<UUID, IObject>(id => factory.GetIObject(id)))
                    RegisterPrim(obj);
            }
        }

        public TouchButton(MRMPrimFactory factory, string name, IPermissions permissions, params UUID[] ids) {
            _name = name;
            _factory = factory;
            _listeners = new List<EntityTouchedDelegate>();
            _permissions = permissions;

            foreach (var obj in ids.Select<UUID, IObject>(id => factory.GetIObject(id)))
                RegisterPrim(obj);
        }

        private void RegisterPrim(IObject obj) {
            lock (this) {
                if (obj == null)
                    return;
                if (_tracked.Contains(obj.GlobalID))
                    return;
                else 
                    _tracked.Add(obj.GlobalID);
            }
            try {
                obj.OnTouch += (source, args) => {
                    TouchEventArgs e = new TouchEventArgs();
                    e.AvatarID = args.Avatar.GlobalID;
                    e.AvatarName = args.Avatar.Name;
                    e.AvatarPosition = args.Avatar.WorldPosition;
                    e.TouchPosition = args.TouchPosition;
                    Touched(source.GlobalID, e);
                };

                _factory.RegisterLinkButton(obj, this);
            } catch (Exception e) {
                _factory.RegisterChatButton(obj.GlobalID, this);
                obj.Say(e.Message);
            }
        }

        private void Touched(UUID source, TouchEventArgs args) {
            if (!_permissions.Authorize(_factory[source], args.AvatarID)) {
                return;
            }
            _logger.Debug(Name + " touched.");
            foreach (var listener in _listeners)
                listener(source, args);
        }

        internal void TriggerTouched(string text, UUID id) {
            string[] msg = text.Split(':');
            if (msg.Length == 5) {
                TouchEventArgs args = new TouchEventArgs();
                args.AvatarID = UUID.Parse(msg[1]);
                args.AvatarName = msg[2];
                args.AvatarPosition = Vector3.Parse(msg[3]);
                args.TouchPosition = Vector3.Parse(msg[4]);
                Touched(id, args);
            }
        }

        internal void TriggerTouched(IObject entity, MRMTouchEventArgs args) {
            TouchEventArgs e = new TouchEventArgs();
            e.AvatarID = args.Avatar.GlobalID;
            e.AvatarName = args.Avatar.Name;
            e.AvatarPosition = args.Avatar.WorldPosition;
            e.TouchPosition = args.TouchPosition;
            Touched(entity.GlobalID, e);
        }

        public void Dispose() {
        }

        #endregion

        public override string ToString() {
            return Name;
        }
    }
}
