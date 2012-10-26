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
using OpenMetaverse;
using common.framework.impl.util;
using common.framework.interfaces.entities;
using Diagrams;
using System.Drawing;
using log4net;

namespace common.framework.abs.full {
    // TODO IMPLEMENTED
    public abstract class LogicEntity : ILogicEntity {
        private readonly ILog Logger;
        public LogicEntity(string name)
            : this(name, Color.White, 0d, new Parameters()) {
        }

        public LogicEntity(string name, Color colour, double selected, Parameters parameters) {
            Logger = LogManager.GetLogger(GetType());
            ID = UUID.Random();

            Name = name;
            Colour = colour;
            DefaultColour = colour;
            Selected = selected;
            IsSelected = selected != 0d;
            Parameters = parameters == null ? new Parameters() : parameters;
        }

        #region ILogicEntity Members

        ///<inheritdoc />
        public virtual Parameters Parameters { get; set; }

        ///<inheritdoc />
        public virtual UUID ID { get; set; }

        ///<inheritdoc />
        public virtual string Name { get; set; }

        /// <summary>
        /// The default colour of the prim in the world. When Reset is called Colour with be replaced with this.
        /// </summary>
        public virtual Color DefaultColour {
            get;
            set;
        }

        public virtual Color Colour {
            get;
            set;
        }

        public virtual double Selected {
            get;
            set;
        }

        public virtual bool IsSelected {
            get;
            set;
        }

        public abstract event EntityDeletedDelegate OnWorldDelete;

        public abstract event EntityTouchedDelegate OnWorldTouch;

        public abstract event EntityMovedDelegate OnWorldMove;

        public abstract void ToggleSelected(double degree);

        public abstract bool Destroy();

        public abstract void Reset();

        public abstract void Say(string msg);

        public abstract void Say(int channel, string msg);

        #endregion

        public override String ToString() {
            return Name + " (" + ID + ")";
        }

        /// <inheritdoc/>
        public override bool Equals(Object o) {
            if (o == null)
                return false;
            if (o is IEntity)
                return ((IEntity)o).ID == ID;
            return false;
        }

        public override int GetHashCode() {
            return ID.GetHashCode();
        }
    }
}