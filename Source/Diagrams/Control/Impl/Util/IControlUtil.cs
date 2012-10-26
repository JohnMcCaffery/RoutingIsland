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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using common.framework.interfaces.basic;
using Diagrams.Control.impl.Util;
using Diagrams.Control.impl.Entities;
using StAndrews.NetworkIsland.Control;
using common;
using OpenMetaverse;
using Diagrams.Common;
using common.framework.impl.util;
using common.framework.interfaces.entities;
using System.Xml;
using System.Drawing;

namespace Diagrams.Control.impl.Module {
    public interface IControlUtil {
        IPrim HostPrim {
            get;
        }

        IModel Model {
            get;
        }

        bool Paused {
            get;
            set;
        }

        int Wait {
            get;
            set;
        }

        ITopologyManager Topology {
            get;
        }

        ISequenceManager Record {
            get;
        }

        /// <summary>
        /// Perform the specified action on every node mapped by the module.
        /// </summary>
        /// <param name="doThis">The delegate function to be performed on each node.</param>
        void ForAllNodes(string name, UUID id, Action<IControlNode> doThis);

        void Say(string msg);

        /// <summary>
        /// Check whether a node and a link are connected.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="link">The link.</param>
        bool AreConnected(UUID node, UUID link);

        /// <summary>
        /// Find out whether two nodes are linked
        /// </summary>
        /// <param name="from">The first entity to check for a _link</param>
        /// <param name="to">The second entity to check for a _link</param>
        bool AreLinked(UUID from, UUID to);

        /// <summary>
        /// Get the two nodes at either end of a _link
        /// </summary>
        /// <param name="link">The _link to get the nodes at either end of.</param>
        Pair<IControlNode, IControlNode> GetEnds(UUID link);

        /// <summary>
        /// Get a _link given the two nodes at either end
        /// </summary>
        /// <param name="from">The entity at one end of the _link.</param>
        /// <param name="to">The entity at the other end of the _link</param>
        IControlLink GetLink(UUID from, UUID to);

        /// <summary>
        /// Get a link given its ID.
        /// </summary>
        /// <param name="link">The ID of the link.</param>
        IControlLink GetLink(UUID link);

        /// <summary>
        /// Get a list of all links that are linked to a given entity. The list is indexed by the ID of the neighbour at the other end of the _link.
        /// </summary>
        IKeyTable<IControlLink> GetLinks(UUID node);

        /// <summary>
        /// Get a node given its ID.
        /// </summary>
        /// <param name="node">The ID of the node.</param>
        IControlNode GetNode(UUID node);

        /// <summary>
        /// Get all the neighbours of a given entity. The list is indexed by the ID of the _link going to the neighbour.
        /// </summary>
        IKeyTable<IControlNode> GetNeighbours(UUID node);

        /// <summary>
        /// Find out whether an ID references a entity
        /// </summary>
        bool IsNode(UUID id);

        /// <summary>
        /// Find out whether an ID references a _link
        /// </summary>
        bool IsLink(UUID id);

        /// <summary>
        /// Find out whether a given _link is attached to a specified _node
        /// </summary>
        /// <param name="node">The _node to check.</param>
        /// <param name="link">The _link which may or may not be attached to _node.</param>
        bool IsNodeLink(UUID node, UUID link);

        /// <summary>
        /// Perform the specified action on every link mapped by the module.
        /// </summary>
        /// <param name="doThis">The delegate function to be performed on each link.</param>
        void ForAllLinks(string name, UUID id, Action<ILink> doThis);

        /// <param name="name">The name of the avatar who is creating the link.</param>
        /// <param name="id">The ID of the avatar who is creating the link.</param>
        ILink AddLink(UUID from, UUID to, Parameters parameters, string name, UUID id, float weight = default(float), bool bidirectional = true);

        /// <param name="ownerName">The name of the avatar who is creating the node.</param>
        /// <param name="ownerID">The ID of the avatar who is creating the node.</param>
        INode AddNode(string name, Parameters parameters, string ownerName, UUID ownerID, Vector3 position = default(Vector3), System.Drawing.Color colour = default(Color));

        void RemoveLink(UUID l, Parameters parameters);

        void RemoveLink(OpenMetaverse.UUID from, OpenMetaverse.UUID to, common.framework.impl.util.Parameters parameters);

        void RemoveNode(OpenMetaverse.UUID n, common.framework.impl.util.Parameters parameters);

        void Clear(string sender, OpenMetaverse.UUID id);
    }
}
