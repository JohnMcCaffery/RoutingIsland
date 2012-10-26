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

using OpenMetaverse;
using common.model.framework.interfaces;
using core.view.interfaces;
using common.framework.impl.util;
using Diagrams.Framework.Util;

namespace Diagrams {
    public interface IView : IModule {
        /// <summary>
        /// Send a packet across the network
        /// </summary>
        /// <param name="from">The senderID of the entity sending the packet</param>
        /// <param name="link">The ID of the _link to send the packet along</param>
        /// <param name="packet">The packet to send</param>
        void Send(UUID from, UUID link, IPacket packet);

        /// <summary>
        /// Trigger the node to display its forwarding table.
        /// </summary>
        /// <param name="routes">The strings which represent each route the forwarding table knows of.</param>
        /// <param name="node">The ID of the node to display the forwarding table for.</param>
        void DisplayForwardingTable(UUID node, Route[] routes, params UUID[] boards);

        /// <summary>
        /// Update the forwarding table which is being displayed.
        /// </summary>
        /// <param name="routes">The strings which represent each route the forwarding table knows of.</param>
        /// <param name="node">The ID of the node to update the forwarding table for.</param>
        void UpdateForwardingTable(UUID node, Route[] routes);
    }
}