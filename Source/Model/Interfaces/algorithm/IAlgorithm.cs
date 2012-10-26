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
using common.framework.interfaces.entities;

#endregion

namespace common.model.framework.interfaces {
    /// <summary>
    ///   The delegate definition of the method to be called when the packet is to be sent to the physical layer
    /// </summary>
    /// <param name = "from">The entity to send the packet from</param>
    /// <param name = "link">The _link to send the packet along</param>
    /// <param name = "packet">The packet to deliver</param>
    public delegate void ForwardDelegate(UUID from, UUID link, IPacket packet);

    /// <summary>
    ///   Defines the behaviour of an algorithm
    /// </summary>
    public interface IAlgorithm {
        /// <summary>
        ///   Get the _name of the algorithm
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Move to the next step of visualising the algorithm.
        /// </summary>
        bool Step();

        /// <summary>
        ///   Stop any threads the algorithm needs to Stop ahd make sure no more Start up
        /// </summary>
        void Stop();

        /// <summary>
        ///   Create a entity of the type which implements the algorithm
        /// </summary>
        /// <param _name="entity">The basic information about the entity common to all layers</param>
        /// <param _name="view">The view layer used to visualise this entity in world</param>
        /// <param _name="forwardMethod">A method that can be used to send packets across the network</param>
        /// <param name="node">The node which the algorithm will interact with in order to run.</param>
        /// <param name="forwardMethod">A method the algorithm can use to forward a packet along a link.</param>
        /// <returns>The new entity that has been created</returns>
        IAlgorithmNode MakeNode(IMNodeInternal node, ForwardDelegate forwardMethod);
    }
}