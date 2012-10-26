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

using common.framework.impl.util;
using System.Drawing;
using Diagrams;

#endregion

namespace common.framework.interfaces.entities {
    /// <summary>
    ///   Basic interface for any entity in the system. Link, Node and Packet interfaces all extend this interface. Contains the functionality
    ///   which is not relevant to the world but is relevant to entities within the system. Extends the ICoreEntity interface for the functionality
    ///   it has in common with in world primitives
    /// </summary>
    public interface ILogicEntity : IEntity {
        /// <summary>
        /// Triggered whenever this entity is deleted in world.
        /// </summary>
        event EntityDeletedDelegate OnWorldDelete;

        /// <summary>
        /// Triggered whenever this entity is touched.
        /// </summary>
        event EntityTouchedDelegate OnWorldTouch;

        /// <summary>
        /// Triggered whenever this entity is touched.
        /// </summary>
        event EntityMovedDelegate OnWorldMove;
        /// <summary>
        ///   A mechanism used purely to allow further extensibility and a means of communication between the controller and simulation layer.
        ///   This Object may be of any type and define whatever data or methods it wishes. For the algorithms implemented in this project 
        ///   it is never used however however anyone wishing to use this API might find this Object useful for specifying data that is
        ///   unecessary to the core API
        /// </summary>
        Parameters Parameters { get; }

        /// <summary>
        /// The colour the packet should be.
        /// </summary>
        Color Colour { get; set; }

        double Selected {
            get;
            set;
        }

        bool IsSelected {
            get;
            set;
        }

        /// <summary>
        /// The default colour of the prim in the world. When Reset is called Colour with be replaced with this.
        /// </summary>
        Color DefaultColour { get; set; }

        /// <param name="degree">How selected the entity should be if it is selected.</param>
        void ToggleSelected(double degree);

        /// <summary>
        ///   Remove the entity from the world
        /// </summary>
        bool Destroy();

        /// <summary>
        ///   Reset all of the physical characteristics back to its default
        /// </summary>
        void Reset();

        /// <summary>
        ///   Put the msg in world said by the Object
        /// </summary>
        void Say(string msg);

        /// <summary>
        ///   Put the msg in world said by the Object
        /// </summary>
        void Say(int channel, string msg);
    }
}