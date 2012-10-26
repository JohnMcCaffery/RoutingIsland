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
using common.framework.impl.util;
using Diagrams.Control.impl.Buttons.ControlButtons;
using Diagrams.Control.Impl.Module;
using common.framework.interfaces.entities;
using OpenMetaverse;
using Diagrams.MRM.Controls;

namespace Diagrams.Control.impl.Entities {
    public class SandboxControlNode : ControlNode {

        private readonly SandboxControl _control;

        protected new SandboxControl Control {
            get { return _control; }
        }

        public SandboxControlNode(INode n, Vector3 pos, SandboxControl control, IPermissions permissions)
            : base(n, control, permissions) {
            
            _control = control;
        }

        protected override void OnDeleted() {
            Control.RemoveNode(ID, GetParameters(Owner));
        }

        protected Parameters GetParameters(UUID id) {
            return new Parameters("Visualise", Control.State.GetToggleState(SandboxControl.ShowUpdatePackets, id));
        }

        protected void RemoveThis(string name, UUID id) {
            Control.Record.RemoveNode(ID, GetParameters(id));
            //Control.ResetState(name, id);
        }

        protected void RemoveLink(UUID otherEnd, string name, UUID id) {
            RemoveLink(ID, otherEnd, name, id);
        }

        protected void RemoveLink(UUID start, UUID otherEnd, string name, UUID id) {
            Control.Record.RemoveLink(otherEnd, start, GetParameters(id));
            Control.State.ResetState(name, id);
        }

        protected void AddLink(UUID otherEnd, string name, UUID id) {
            Control.AddLink(otherEnd, ID, GetParameters(id), name, id, SandboxControlLink.GetWeight(id));
            Control.State.ResetState(name, id);
        }

        protected void VisualiseRouting(UUID target, string name, UUID id) {
            Control.Model.VisualiseRouting(target, ID, GetParameters(id));
            Control.State.ResetState(name, id);
        }

        protected void VisualiseRouting(string name, UUID id) {
            Control.Model.VisualiseRouting(ID, GetParameters(id));
            Control.State.ResetState(name, id);
        }

        protected void DisplayForwardingTable(string name, UUID id) {
            Control.DisplayForwardingTable(ID, name, id);
            Control.State.ResetState(name, id);
        }

        public override bool Destroy() {
            foreach (var link in _control.GetLinks(ID))
                _control.Record.RemoveLink(link.ID, GetParameters(Owner));
            return true;
        }
    }
}
