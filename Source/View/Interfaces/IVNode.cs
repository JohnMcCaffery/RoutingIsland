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

using common.framework.interfaces.entities;
using common.model.framework.interfaces;
using OpenMetaverse;
using Diagrams;
using Diagrams.Framework.Util;
using common.framework.interfaces.basic;
using System.Collections.Generic;

#endregion

namespace core.view.interfaces {
    /// <summary>
    ///   Store information about a entity useful for positioning it in the virtual world
    /// </summary>
    public interface IVNode : INode {
        /// <summary>
        /// To be called when a packet arrives at the entity
        /// </summary>
        /// <param name="packet">The packet that this _node is to receive</param>
        void PacketReceived(IPacket packet);

        /// <summary>
        /// Trigger the node to display its forwarding table.
        /// </summary>
        /// <param name="routes">The strings which represent each route the forwarding table knows of.</param>
        /// <param name="boards">The board to print the routing table on.</param>
        void DisplayForwardingTable(Route[] routes, IEnumerable<IPrim> boards);

        /// <summary>
        /// Remove the specified board from the list of boards this node updates.
        /// </summary>
        /// <param name="id"></param>
        void RemoveBoard(UUID id);

        /// <summary>
        /// Update the forwarding table which is being displayed.
        /// </summary>
        /// <param name="routes">The strings which represent each route the forwarding table knows of.</param>
        void UpdateForwardingTable(Route[] routes);

        /// <summary>
        /// Triggered whenever an API call causes the node to move.
        /// </summary>
        event EntityMovedDelegate OnAPIMove;
    }
}