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
using common.framework.abs.wrapper;
using OpenMetaverse;
using common.framework.interfaces.entities;
using Diagrams.Control.impl.Module;
using common.framework.impl.util;
using Diagrams.Control.Impl.Module;
using Diagrams.MRM.Controls;

namespace Diagrams.Control.impl.Entities {
    /// <summary>
    /// Wrapper for a node which will hash based on the name rather than the ID of the node.
    /// Used so that nodes can be serialized and recreated in a new instance with different IDs.
    /// </summary>
    public class ControlNode : NodeWrapper, IControlNode {
        private readonly IControlUtil _control;

        private readonly IPermissions _permissions;

        private readonly UUID _owner;

        protected IControlUtil Control {
            get { return _control; }
        }

        public UUID Owner {
            get {
                return _owner;
            }
        }

        public ControlNode(INode n, IControlUtil control, IPermissions permissions)
            : base(n) {

            _control = control;
            _permissions = permissions;
            _owner = Parameters.Get<UUID>(Diagrams.Control.Impl.Module.Control.OwnerID); ;

            OnWorldTouch += Touched;
            OnWorldDelete += entity => OnDeleted();
        }

        protected virtual void OnDeleted() {
            Control.Record.RemoveNode(ID, new Parameters());
        }

        protected virtual void Touched(UUID entity, TouchEventArgs args) {
        }

        public override int GetHashCode() {
            return Name.GetHashCode();
        }

        public override bool Destroy() {
            foreach (var link in _control.GetLinks(ID))
                _control.Record.RemoveLink(link.ID, new Parameters());
            return true;
        }

        public virtual bool Authorize(string name, UUID id) {
            return _permissions.Authorize(this, id);
        }
    }
}
