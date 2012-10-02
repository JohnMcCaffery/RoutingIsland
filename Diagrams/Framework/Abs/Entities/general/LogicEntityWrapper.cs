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

#region Namespace imports

using System;
using System.Drawing;
using Diagrams;
using OpenMetaverse;
using common.framework.interfaces.basic;
using common.framework.interfaces.entities;
using log4net;
using common.framework.impl.util;

#endregion

namespace common.framework.abs.wrapper {
    /// <summary>
    ///   Abstract class which wraps a pre-existing IEntity. Any class which extends this class only has to take an entity
    ///   as a @delegate for its constructor and pass the entity up to EntityWrappers constructor and it immediatelly inherits
    ///   a full implementation of the IEntity interface
    /// </summary>
    public abstract class LogicEntityWrapper : ILogicEntity {
        #region Private Fields

        /// <summary>
        /// The prim which this object wraps.
        /// </summary>
        private readonly ILogicEntity _entity;

        private readonly ILog _log;

        #endregion

        #region Constructors

        /// <summary>
        ///   Store the entity that this wrapper wraps
        /// </summary>
        /// <param name = "_entity"></param>
        protected LogicEntityWrapper(ILogicEntity e) {
            this._entity = e;
            _log = LogManager.GetLogger(GetType());
        }

        #endregion
                
        protected ILog Logger {
            get { return _log; }
        }

        protected bool DestoryWrapped() {
            return _entity.Destroy();
        }

        #region Prim Members

        /// <summary>
        /// Global ID of the entity
        /// </summary>
        public virtual UUID ID {
            get { return _entity.ID; }
        }

        /// <summary>
        /// The name of this primitive
        /// </summary>
        public virtual string Name {
            get { return _entity.Name; }
            set { _entity.Name = value; }
        }

        /// <summary>
        /// The default colour of the prim in the world. When Reset Colour with be replaced with this.
        /// </summary>
        public virtual Color DefaultColour {
            get { return _entity.DefaultColour; }
            set { _entity.DefaultColour = value; }
        }

        /// <summary>
        /// The colour of the prim in the world
        /// </summary>
        public virtual Color Colour {
            get { return _entity.Colour; }
            set { _entity.Colour = value; }
        }

        /// <summary>
        /// Put the msg in world said by the Object
        /// </summary>
        /// <param name="msg"></param>
        public virtual void Say(string msg) {
            _entity.Say(msg);
        }

        /// <summary>
        /// Put the msg in world said by the Object
        /// </summary>
        /// <param name="msg"></param>
        public virtual void Say(int channel, string msg) {
            _entity.Say(channel, msg);
        }

        /// <summary>
        /// Reset all of the physical characteristics back to its default
        /// </summary>
        public virtual void Reset() {
            _entity.Reset();
        }

        /// <summary>
        /// Do whatever the particular prim needs to remove itself from the module.
        /// </summary>
        /// <returns></returns>
        public abstract bool Destroy();

        #endregion

        public Parameters Parameters {
            get { return _entity.Parameters; }
        }

        public double Selected {
            get {
                return _entity.Selected;
            }
            set {
                _entity.Selected = value;
            }
        }

        public bool IsSelected {
            get {
                return _entity.IsSelected;
            }
            set {
                _entity.IsSelected = value;
            }
        }

        public void ToggleSelected(double degree) {
            _entity.ToggleSelected(degree);
        }

        public event EntityDeletedDelegate OnWorldDelete {
            add {
                _entity.OnWorldDelete += value;
            }
            remove {
                _entity.OnWorldDelete -= value;
            }
        }

        public event EntityTouchedDelegate OnWorldTouch {
            add {
                _entity.OnWorldTouch += value;
            }
            remove {
                _entity.OnWorldTouch -= value;
            }
        }

        public event EntityMovedDelegate OnWorldMove {
            add {
                _entity.OnWorldMove += value;
            }
            remove {
                _entity.OnWorldMove -= value;
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="o">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(Object o) {
            if (o == null)
                return false;
            if (o is IEntity)
                return ((IEntity)o).ID == ID;
            return false;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() {
            return (int)ID.GetULong();
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override String ToString() {
            return Name + " (" + ID + ")";
        }
    }
}