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

namespace Diagrams {
    public interface IModel : IModule {
        /// <summary>
        /// The algorithms which model is configured to work with.
        /// </summary>
        string[] Algorithms { get; }

        string Algorithm { get; }

        /// <summary>
        /// Send a packet.
        /// </summary>
        /// <param name="from">The entity to send the packet from.</param>
        /// <param name="to">The entity to send the packet to.</param>
        /// <param name="parameters">Any _parameters associated with sending the packet.</param>
        void Send(UUID from, UUID to, Parameters parameters);

        /// <summary>
        /// Set the algorithm that is currently running in the model.
        /// </summary>
        /// <param name="algorithm">The algorithm to set as running</param>
        void SetAlgorithm(String algorithm);

        /// <summary>
        /// Show the routing table for a specific entity.
        /// </summary>
        /// <param name="node">The entity to show the forwarding table for.</param>
        /// <param name="parameters">Any parameters associated with how the forwarding table should be displayed.</param>
        void DisplayForwardingTable(UUID node, Parameters parameters, params UUID[] boards);

        /// <summary>
        /// PlayNextEvent to the next PlayNextEvent of the algorithm running.
        /// </summary>
        bool Step();

        /// <summary>
        /// Visualise the current algorithm centred on the specified entity.
        /// </summary>
        /// <param name="at">The entity to visualise the routing algorithm running at.</param>
        /// <param name="parameters">Any _parameters associated with the routing running.</param>
        void VisualiseRouting(UUID at, Parameters parameters);

        /// <summary>
        /// Visualise the current algorithm running between from and to.
        /// </summary>
        /// <param name="from">The entity to run the algorithm from.</param>
        /// <param name="to">The entity to visualise the algorithm running to.</param>
        /// <param name="parameters">Any _parameters associated with the routing running.</param>
        void VisualiseRouting(UUID from, UUID to, Parameters parameters);
    }
}