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
using common.framework.interfaces.entities;
using common.model.framework.interfaces;
using common.framework.impl.util;
using Diagrams.Framework.Util;

namespace Diagrams {
    public interface IMNodeExternal : INode {
        /// <summary>
        /// Triggered when the forwarding table for the node changes.
        /// </summary>
        event System.Action<UUID, Route[]> OnForwardingTableChange;

        /// <summary>
        /// The algorithm the node should be running.
        /// </summary>
        string CurrentAlgorithm { set; }

        /// <summary>
        /// The forwarding table for the node.
        /// </summary>
        Route[] ForwardingTableList {
            get;
        }

        /// <summary>
        /// Send a packet across the network starting at this node.
        /// </summary>
        /// <param name="destination">The destination to send the packet to.</param>
        /// <param name="message">The message to contain in the packet.</param>
        void Send(IMNodeExternal destination, Parameters parameters);

        /// <summary>
        /// Add a _link to this node.
        /// </summary>
        /// <param name="link">The _link to add.</param>
        void AddLink(IMLink link, Parameters parameters);

        /// <summary>
        /// Remove a _link from this node.
        /// </summary>
        /// <param name="link">The _link to remove.</param>
        void RemoveLink(UUID link, Parameters parameters);

        /// <summary>
        /// Visualise the algorithm running in this entity
        /// </summary>
        /// <param name="parameters">Any parameters associated with running the algorithm.</param>
        void VisualiseRoutingAlgorithm(Parameters parameters);

        /// <summary>
        /// Visualise the algorithm running between this entity and a specified target entity
        /// </summary>
        /// <param name="to">If the algorithm is to run to a specific target this is the target.</param>
        /// <param name="parameters">Any parameters associated with running the algorithm.</param>
        void VisualiseRoutingAlgorithm(UUID to, Parameters parameters);
    }
}