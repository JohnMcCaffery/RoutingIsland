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
using Diagrams.MRM.Controls;
using common.interfaces.entities;
using OpenMetaverse;
using Diagrams.MRM.Controls.Buttons;
using System.Threading;
using Diagrams.Framework.Interfaces.Entities.general;
using Nini.Config;
using common.framework.interfaces.basic;

namespace Diagrams.Control.impl.Util {
    /// <summary>
    /// Permission instance that provides a toggle to allow one user at a time to take control.
    /// If a user presses the toggle they become the owner. If they do not make a change in a given time or press the toggle they relinquish ownership.
    /// Only the current owner is authorized to make changes.
    /// </summary>
    public class LockedPermissions : IPermissions {
        private readonly int _lockWait = 300000;
        private readonly IToggle _lockToggle;

        private bool _Locked { get { return _ownerID != UUID.Zero; } }
        private UUID _ownerID;
        private string _ownerName;
        private bool _updated;
        private IPrim _host;

        public LockedPermissions(IPrimFactory factory, IConfigSource config, IPrim host, Control.Impl.Module.Control control) {
            _ownerID = UUID.Zero;
            _host = host;
            IConfig controlConfig = config.Configs["Control"];
            double fade = controlConfig.GetDouble("LockFade", control.Fade);
            double glow = controlConfig.GetDouble("LockGlow", control.ToggleGlow);
            _lockWait = controlConfig.GetInt("ControlsLockTimeout", 300000);
            _lockToggle = new Toggle(factory.MakeButton("Lock", new LockPermissions(this)), fade, glow);

            _lockToggle.OnToggledOn += (source, args) => SetCurrentOwner(args.ID, args.Name);
            _lockToggle.OnToggledOff += (source, args) => {
                _ownerID = UUID.Zero;
                _host.Say(_ownerName + " relinquished control of sandbox.");
                JM726.Lib.Static.Util.Wake(this);
            };
            _lockToggle.OnTouched += (source, args) => {
                if (_Locked && !args.AvatarID.Equals(_ownerID))
                    _host.Say("Unable to take control of sandbox. Sandbox is currently locked to '" + _ownerName + "'.");
            };
        }

        private void SetCurrentOwner(UUID newOwner, string name) {
            _ownerID = newOwner;
            _ownerName = name;
            _host.Say(_ownerName + " took control of sandbox.");
            Thread t = new Thread(() => {
                _updated = true;
                //Loop until the timeout expires without the owner making any changes
                while (_updated) {
                    _updated = false;
                    JM726.Lib.Static.Util.Wait(_lockWait, true, this);
                }
                if (_Locked) {
                    _host.Say("Current owner ('" + _ownerName + "') inactive. Unlocking sandbox.");
                    _ownerID = UUID.Zero;
                    _lockToggle.IsOn = false;
                }
            });
            t.Name = "Control Panel Lock timeout thread.";
            t.Start();
        }

        #region IPermissions Members

        public bool Authorize(IOwned entity, UUID id) {
            if (id.Equals(_ownerID)) {
                //Reset the timeout every time the user makes a change
                _updated = true;
                JM726.Lib.Static.Util.Wake(this);
                return true;
            }
            return false;
        }

        #endregion

        private class LockPermissions : IPermissions {
            private LockedPermissions _parent;
            public LockPermissions(LockedPermissions parent) {
                _parent = parent;
            }

            #region IPermissions Members

            public bool Authorize(IOwned entity, OpenMetaverse.UUID id) {
                return _parent._ownerID.Equals(UUID.Zero) || id.Equals(_parent._ownerID);
            }

            #endregion
        }
    }
}
