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
using common.framework.impl.util;
using common.framework.interfaces.entities;
using OpenMetaverse;

#endregion

namespace common.framework.abs.wrapper {
    /// <summary>
    ///   TODO IMPLEMENTED
    ///   Wraps an existing _link Object implementing the INode interface and is designed to be extended.
    ///   By extending NodeWrapper any class which needs to implement the INode interface but can guarantee
    ///   it will be instantiated with an already existing implementation if the INode interface can simple 
    ///   wrap the pre-existing interface so all calls to INode methods are redirected to the wrapped _link
    /// </summary>
    public abstract class NodeWrapper : LogicEntityWrapper, INode {
        #region Private Fields

        protected readonly INode _node;

        #endregion

        #region Constructors

        /// <summary>
        ///   Initialise the Node with the Node it is to wrap
        /// </summary>
        public NodeWrapper(INode node)
            : base(node) {
            this._node = node;
        }

        #endregion

        #region INode Members

        /// <inheritdoc />
        public event PacketReceivedDelegate OnPacketReceived {
            add { _node.OnPacketReceived += value; }
            remove { _node.OnPacketReceived -= value; }
        }

        public Vector3 Pos {
            get { return _node.Pos; }
            set { _node.Pos = value; }
        }

        #endregion
    }
}