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
using common;
using common.framework.impl.util;
using common.model.framework.interfaces;
using common.framework.interfaces.entities;
using Diagrams.Common;

namespace Diagrams {
    public interface IAlgorithmNode {
        /// <summary>
        /// Triggered whenever the routing table changes. A change can be a target being added or removed or the route to a target changing from one link to another.
        /// Only triggered if the algorithm is marked as the current algorithm
        /// </summary>
        event ForwardingTableChangeDelegate OnRouteChange;
        /// <summary>
        /// What link to route packets along for any given target that this entity knows about. Should be a copy.
        /// </summary>
        IKeyTable<IMLink> ForwardingTable { get; }

        /// <summary>
        /// Whether this is the algorithm currently being run from the entity.
        /// </summary>
        bool IsCurrentAlgorithm { get;  set; }

        /// <summary>
        /// The ID of the node the algorithm is running in.
        /// </summary>
        UUID ID { get; }

        /// <summary>
        /// Get the distance the algorithm has calculated from its node to the given target node.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        float GetDistance(UUID target);

        /// <summary>
        /// Visualise the algorithm running in this entity.
        /// </summary>
        /// <param name="parameters">Any parameters associated with running the algorithm.</param>
        void VisualiseAlgorithm(Parameters parameters);

        /// <summary>
        /// Visualise the algorithm running between this entity and a specified target entity.
        /// </summary>
        /// <param name="to">If the algorithm is to run to a specific target this is the target.</param>
        /// <param name="parameters">Any parameters associated with running the algorithm.</param>
        void VisualiseAlgorithm(UUID to, Parameters parameters);

        /// <summary>
        /// What to do when a packet is received that is to be processed by this algorithm.
        /// </summary>
        /// <param name="packet">The packet the algorithm is to process.</param>
        void ProcessPacket(IMPacket packet);

        /// <summary>
        /// Move to the next step of visualising the algorithm.
        /// </summary>
        //void Step();

        /// <summary>
        /// Stop any processing that is currently happening.
        /// </summary>
        void Stop();
    }
}