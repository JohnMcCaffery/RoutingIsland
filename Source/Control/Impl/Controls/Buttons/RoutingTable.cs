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
using Diagrams.MRM.Controls.Buttons;
using common.interfaces.entities;
using OpenMetaverse;
using Diagrams.Control.impl.Module;
using Diagrams.Control.Impl.Module;
using common.framework.impl.util;
using common.framework.interfaces.entities;
using common.framework.interfaces.basic;
using Diagrams.Control.Impl.Entities;
using System.Drawing;

namespace Diagrams.Control.Impl.Controls.Buttons {
    public class RoutingTable {
        private HashSet<UUID> _selectedButtons;
        private IPrimFactory _factory;
        private Module.Control _control;
        private readonly IButton _button;
        private readonly HashSet<UUID> _prims = new HashSet<UUID>();

        public RoutingTable(IButton button, Module.Control control, IPrimFactory primFactory) {
            _selectedButtons = new HashSet<UUID>();
            _factory = primFactory;
            _control = control;
            _button = button;

            foreach (var prim in button.Prims) {
                control.Record.MakeMapped<IEntity>(new MappableEntity(prim));
                prim.Glow = 0d;
                if (prim.IsAttachment) prim.Colour = Color.LightGray;
            }

            button.OnTouched += (source, args) => {
                IPrim p = primFactory[source];
                if (_selectedButtons.Contains(source)) {
                    p.Glow = 0d;
                    if (p.IsAttachment) p.Colour = Color.LightGray;
                    _selectedButtons.Remove(source);
                    if (_selectedButtons.Count == 0)
                        control.State.ResetState(args.AvatarName, args.AvatarID);
                } else {
                    string state = control.State.GetState(args.AvatarName, args.AvatarID);
                    if (state.Equals(SandboxControl.EPSelected) || state.Equals(SandboxControl.RouterSelected)) {
                        ILogicEntity selectedNode = control.State.GetSelectedEntity(args.AvatarName, args.AvatarID);
                        _control.HostPrim.Say("Displaying forwarding table for " + selectedNode.Name);
                        control.Model.DisplayForwardingTable(selectedNode.ID, new Parameters(), source);
                        control.State.ResetState(args.AvatarName, args.AvatarID);
                    } else {
                        _selectedButtons.Add(source);
                        if (!_prims.Contains(source)) {
                            control.Record.MakeMapped<IEntity>(new MappableEntity(primFactory[source]));
                            _prims.Add(source);
                        }
                        p.Glow = control.ToggleGlow;
                        if (p.IsAttachment) p.Colour = Color.White;
                        control.State.SetState(SandboxControl.DisplayTableSelected, args.AvatarName, args.AvatarID);
                    }
                }
            };

            control.State.OnStateChange += (source, args) => {
                if (!args.NewState.Equals(SandboxControl.DisplayTableSelected))
                    ResetSelected();
            };
        }

        public void DisplayForwardingTable(UUID node) {
            if (_selectedButtons.Count > 0) {
                //HashSet<UUID> selectedBoards = new HashSet<UUID>(_selectedButtons);
                //_control.Queue.QWork("Display forwarding table.", () => {
                _control.Model.DisplayForwardingTable(node, new Parameters(), _selectedButtons.ToArray());
                //});
                ResetSelected();
            }
        }

        private void ResetSelected() {
            foreach (var prim in _selectedButtons)
                _factory[prim].Glow = 0;
            _selectedButtons.Clear();
        }
    }
}
