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

using Diagrams;
using OpenMetaverse;

#endregion

namespace common.framework.interfaces.entities {
    public interface ILink /* : ILink<INode, ILink>*/ : ILogicEntity {
        /// <summary>
        ///   The entity at one end of the _link
        /// </summary>
        UUID FromID { get; }

        /// <summary>
        ///   The entity at the other end of the _link
        /// </summary>
        UUID ToID { get; }

        /// <summary>
        ///   Whether this _link can send packets in both directions or just one
        /// </summary>
        bool IsBidirectional { get; }

        /// <summary>
        ///   Get the weighting for this _link. Weight is expected to be between 0 and 1.
        /// </summary>
        float Weight { get; set; }

        /// <summary>
        /// The length of the link in world.
        /// </summary>
        float Length {
            get;
        }

        /// <summary>
        /// 
        /// </summary>
        bool DistanceWeight { get; set; }

        /// <summary>
        /// Listen to this to be notified whenever the weight of a _link changes
        /// </summary>
        event WeightChangedDelegate OnWeightChanged;

        /// <summary>
        /// Listen to this to be notified whenever the weight of a _link changes
        /// </summary>
        event WeightChangedDelegate OnLengthChanged;

        /// <summary>
        ///   Get the entity at the other end of the _link from n
        /// </summary>
        /// <param name = "n">The entity to be checked against</param>
        /// <returns>The entity at the other end of the _link from n</returns>
        UUID OtherEnd(UUID n);
    }

    /// <summary>
    ///   The public interface defining all the methods and properties of a _link which will be necessary for functionality within two or more layers.
    /// 
    ///   Changes made through this public interface can be reflected at all levels of the stack.
    /// </summary>
    public interface ILink<out TNode> : ILink where TNode : INode {
        /// <summary>
        /// The node at the start of this link
        /// </summary>
        TNode From { get; }

        /// <summary>
        /// The node at the end of this link
        /// </summary>
        TNode To { get; }

        /// <summary>
        /// Get the node at the other end of the link from the specified node
        /// </summary>
        /// <param name="n">The node at 'this' end of the link</param>
        /// <returns>The node at the end of the link opposite n</returns>
        TNode OtherEnd(INode n);
    }
}