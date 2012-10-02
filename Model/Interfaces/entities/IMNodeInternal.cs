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

using common;
using common.framework.interfaces.entities;
using common.model.framework.interfaces;
using Diagrams.Common;
using OpenMetaverse;
using common.framework.impl.util;

namespace Diagrams {
    /// <summary>
    /// Delegate for new link events in the model layer.
    /// </summary>
    /// <param name="node">The node that had the link added.</param>
    /// <param name="link">The link that was just added</param>
    public delegate void LinkAddedDelegate(UUID node, IMLink link, Parameters parameters);

    /// <summary>
    /// Delegate for link remove events in the model layer.
    /// </summary>
    /// <param name="node">The node that had the link removed.</param>
    /// <param name="link">The link that was just removed</param>
    public delegate void LinkRemovedDelegate(UUID node, IMLink link, Parameters parameters);


    public interface IMNodeInternal : INode {
        /// <summary>
        /// Table of all links out of the entity. Should be a copy of the real table.
        /// </summary>
        IKeyTable<IMLink> Links { get; }

        /// <summary>
        /// Table of all neighbours of the entity. Should be a copy of the real table.
        /// </summary>
        IKeyTable<IMNodeInternal> Neighbours { get; }

        /// <summary>
        /// Triggered when the weight of one of the links attached to this entity changes.
        /// </summary>
        event WeightChangedDelegate OnWeightChange;

        /// <summary>
        /// Triggered when a new link is added to the entity.
        /// </summary>
        event LinkAddedDelegate OnLinkAdded;

        /// <summary>
        /// Triggered when a link is removed from this entity.
        /// </summary>
        event LinkRemovedDelegate OnLinkRemoved;

        void TriggerHighlight(string alg, IMNodeInternal target);

        void TriggerHighlightAll(string alg);

        void TriggerHighlightReset();

        void Reset(string algorithm);
    }
}