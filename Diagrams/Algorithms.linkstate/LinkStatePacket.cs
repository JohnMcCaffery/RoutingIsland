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
using OpenMetaverse;
using common;
using common.framework.impl.util;
using common.framework.interfaces.entities;
using common.model.framework.interfaces;
using Diagrams;
using diagrams.algorithms.linkstate;

#endregion

namespace algorithms.dijkstra.impl {
    /// <summary>
    ///   Packet used in the running of Dijkstra's Algorithm
    /// </summary>
    public class LinkStatePacket : MPacket {
        private int _event;

        /// <summary>
        ///   Initialise a Dijkstra's Packet
        /// </summary>
        /// <param name = "hop">The node this packet just came from as opposed to its original source</param>
        /// <param name = "from">The node the packet was sent from</param>
        /// <param name = "to">The node the packet is destined for</param>
        /// <param name = "evt">The AddLink along which the packet is currently travelling</param>
        /// <param name = "view">The view layer which is used to visualise the packet moving</param>
        /// <param name = "visualise">Whether the packet should be visualised</param>
        internal LinkStatePacket(IMNodeInternal hop, INode from, INode to, int evt, bool visualise)
            : base(from, to, hop, new Parameters(), LinkState.LINK_STATE_NAME, visualise) {
            _event = evt;
            Name = "Link IsOn " + Name;
            Selected = 1d;
            Parameters.Set<bool>("Visualise", visualise);
        }

        internal int Event {
            get { return _event; }
        }
    }
}