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
using common.framework.interfaces.entities;
using common.framework.abs.wrapper;
using StAndrews.NetworkIsland.Control;
using OpenMetaverse;
using common.framework.impl.util;
using Diagrams.Control.impl.Module;
using Diagrams.MRM.Controls;

namespace Diagrams.Control.impl.Entities {

    /// <summary>
    /// Wrapper for a link which will hash based on the name rather than the ID of the link.
    /// Used so that links can be serialized and recreated in a new instance with different IDs.
    /// </summary>
    public class ControlLink : LinkWrapper<INode>, IControlLink {
        public enum LinkType { Random, Unit, Length };

        private IControlUtil _control;

        private readonly IPermissions _permissions;

        private readonly UUID _owner;

        protected IControlUtil Control {
            get { return _control; }
        }

        public ControlLink(ILink l, INode from, INode to, IControlUtil control, IPermissions permissions)
            : base(l, from, to) {

            _control = control;
            _permissions = permissions;
            _owner = Parameters.Get<UUID>(Diagrams.Control.Impl.Module.Control.OwnerID); ;

            OnWorldTouch += Touched;
            OnWorldDelete += entity => OnDeleted();
        }

        protected virtual void OnDeleted() {
            Control.RemoveLink(ID, new Parameters());
        }

        protected virtual void Touched(UUID entity, TouchEventArgs args) {

        }

        public override int GetHashCode() {
            return Name.GetHashCode();
        }

        public override bool Destroy() {
            return true;
        }

        public override float Weight {
            get {
                return base.Weight;
            }
            set {
                if (value > 0f)
                    base.Weight = value;
                else
                    base.DistanceWeight = true;
            }
        }

        #region IControlLink Members

        public UUID Owner {
            get {
                return _owner;
            }
        }

        public virtual bool Authorize(string name, UUID id) {
            return _permissions.Authorize(this, id);
        }

        #endregion
    }
}
