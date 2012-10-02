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

using System.Drawing;
using Diagrams;
using OpenMetaverse;
using common.model.framework.interfaces;

#endregion

namespace common.framework.interfaces.entities {
    /// <summary>
    ///   The types of packets a packet can be
    /// </summary>
    public enum PTypes {
        ///A packet representing sending data between two nodes
        data,

        /// <summary>
        ///   A packet which is used in the implementation of some algorithm
        /// </summary>
        Algorithm,
        /*
        /// <summary>
        /// A packet which will Highlight any _link it goes along
        /// </summary>
        Highlight,
        /// <summary>
        /// A packet which will Highlight the route back to the root it came from and flood through to every entity in the system
        /// </summary>
        HighlightAll,
        /// <summary>
        /// A packet which will Reset the state of any entity it passes through and be flooded to every entity in the system
        /// </summary>
        ResetAll,
        /// <summary>
        /// Reset a specific route
        /// </summary>
        Reset,
        /// <summary>
        /// Notify that highlighting is happening without necessarily forcing a Highlight
        /// </summary>
        HighlightNotify,
        /// <summary>
        /// Used when changing a highlighted route, is sent back to the source to notify that the route has been changed and update the trigger variable.
        /// </summary>
        BackwardsNotify*/
    };

    /// <summary>
    ///   The public interface defining all the methods and properties of a packet which will be necessary for functionality within two or more layers.
    /// 
    ///   Changes made through this public interface can be reflected at all levels of the stack.
    /// </summary>
    public interface IMPacket : IPacket {
        /// <summary>
        ///   What type of packet this is
        /// </summary>
        PTypes Type { get; }

        /// <summary>
        ///   The entity to which originally sent this packet
        /// </summary>
        INode Source { get; }

        /// <summary>
        ///   The entity to which this packet is to be sent
        /// </summary>
        INode Destination { get; }

        /// <summary>
        ///   The algorithm this packet is running over
        /// </summary>
        string Algorithm { get; }

        /// <summary>
        ///   ID of the destination entity for this packet
        /// </summary>
        UUID D { get; }

        /// <summary>
        ///   Find out if the algorithm is currently running
        /// </summary>
        IMNodeInternal Hop { get; set; }

        /// <summary>
        ///   ID of the source entity for this packet
        /// </summary>
        UUID S { get; }
    }
}