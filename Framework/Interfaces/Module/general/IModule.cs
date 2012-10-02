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

using System.Drawing;
using OpenMetaverse;
using common.framework.impl.util;
using common.framework.interfaces.entities;

namespace Diagrams {
    public interface IModule {
        /// <summary>
        /// Set the speed at which events run
        /// </summary>
        int Wait {
            get;
            set;
        }

        bool Paused {
            get;
            set;
        }
        /// <summary>
        /// Create a new entity to be managed
        /// </summary>
        /// <param name="name">The _name of the entity</param>
        /// <param name="creator">The user who created the entity</param>
        /// <param name="parameters">Any _parameters associated with the entity</param>
        /// <param name="position">Where the entity should be created in world</param>
        /// <param name="colour">What colour the entity should be in world</param>
        INode AddNode(string name, Parameters parameters, Vector3 position = default(Vector3), Color colour = default(Color));

        /// <summary>
        /// Link two nodes
        /// </summary>
        /// <param name="from">The ID of the entity that the _link starts at</param>
        /// <param name="to">The ID of the entity that the _link goes to</param>
        /// <param name="parameters">Any _parameters associated with the new _link</param>
        /// <param name="weight">The weight associated with the _link</param>
        /// <param name="bidirectional">Whether the _link can transfer packets in both directions. Default is true</param>
        /// 
        ILink AddLink(UUID from, UUID to, Parameters parameters, float weight = default(float), bool bidirectional = true);

        /// <summary>
        /// Remove a _link from the manager
        /// </summary>
        /// <param name="link">The _link to remove</param>
        /// <param name="parameters">Any _parameters associated with the deletion</param>
        void RemoveLink(UUID link, Parameters parameters);

        /// <summary>
        /// Remove a _link from the manager given the two nodes which it links.
        /// </summary>
        /// <param name="from">The entity at one end of the _link</param>
        /// <param name="to">The entity at the other end of the _link</param>
        /// <param name="parameters">Any _parameters associated with the deletion</param>
        void RemoveLink(UUID from, UUID to, Parameters parameters);

        /// <summary>
        /// Remove a entity from the manager. All links associated with the _link must also be deleted.
        /// </summary>
        /// <param name="node">The entity to be removed</param>
        /// <param name="parameters">Any _parameters associated with the deletion</param>
        void RemoveNode(UUID node, Parameters parameters);

        /// <summary>
        /// Stop any threads the manager has started.
        /// </summary>
        void Stop();

        /// <summary>
        /// Clears all data from the module. Does not run any algorithms. Just removes all nodes and links.
        /// </summary>
        void Clear();

        /// <summary>
        /// Clears a range of nodes from the module. Does not run any algorithms. Just removes all nodes and links.
        /// </summary>
        void Clear(params UUID[] nodes);
    }
}