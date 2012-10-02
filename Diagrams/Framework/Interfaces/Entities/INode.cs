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

using Diagrams;
using OpenMetaverse;

namespace common.framework.interfaces.entities {
    /// <summary>
    ///   The public interface defining all the methods and properties of a entity which will be necessary for functionality within two or more layers.
    /// 
    ///   Changes made through this public interface can be reflected at all levels of the stack.
    /// </summary>
    public interface INode /* : INode<ILink, INode>*/ : ILogicEntity {
        /// <summary>
        /// Add a @value to this to be notified whenever a packet is received at this entity
        /// </summary>
        event PacketReceivedDelegate OnPacketReceived;

        /// <summary>
        /// The position of the node.
        /// </summary>
        Vector3 Pos {
            get;
            set;
        }
    }
}