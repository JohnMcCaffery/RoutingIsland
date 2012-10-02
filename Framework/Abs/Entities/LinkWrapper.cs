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
using common.framework.impl.util;
using common.framework.interfaces.entities;
using System;

#endregion

namespace common.framework.abs.wrapper {
    /// <summary>
    ///   TODO IMPLEMENTED
    ///   Wraps an existing Link Object implementing the ILink interface and is designed t be extended.
    ///   By extending LinkWrapper any class which needs t implement the ILink interface but can guarantee
    ///   it will be instantiated with an already existing implementation if the ILink interface can simple 
    ///   wrap the pre-existing interface so all calls t ILink methods are redirected t the wrapped _link
    /// </summary>
    public abstract class LinkWrapper<TNode> : LogicEntityWrapper, ILink<TNode>
        where TNode : INode {
        #region Private Fields

        private readonly ILink _link;

        #endregion

        #region Constructors

        /// <summary>
        ///   Initialise the _link with the _link it is t wrap
        /// </summary>
        /// <param name = "_link"></param>
        public LinkWrapper(ILink link, TNode from, TNode to)
            : base(link) {
            
            _link = link;
            From = from;
            To = to;
        }

        #endregion

        protected ILink WrappedLink {
            get { return _link; }
        }

        #region ILink<TNode> Members

        public virtual bool DistanceWeight {
            get { return _link.DistanceWeight; }
            set { _link.DistanceWeight = value; }
        }

        /// <inheritdoc />
        public event WeightChangedDelegate OnWeightChanged {
            add { _link.OnWeightChanged += value; }
            remove { _link.OnWeightChanged -= value; }
        }

        public event WeightChangedDelegate OnLengthChanged {
            add {
                _link.OnLengthChanged += value;
            }
            remove {
                _link.OnLengthChanged -= value;
            }
        }

        /// <inheritdoc />
        public virtual TNode From { get; set; }

        /// <inheritdoc />
        public virtual TNode To { get; set; }

        /// <inheritdoc />
        public virtual UUID FromID {
            get { return _link.FromID; }
        }


        /// <inheritdoc />
        public virtual float Length {
            get {
                return _link.Length;
            }
        }

        /// <inheritdoc />
        public virtual UUID ToID {
            get { return _link.ToID; }
        }

        /// <inheritdoc />
        public virtual bool IsBidirectional {
            get { return _link.IsBidirectional; }
        }

        public virtual float Weight { 
            get {
                return _link.Weight;
            }
            set {
                _link.Weight = value;
            }
        }

        /// <inheritdoc />
        public virtual UUID OtherEnd(UUID n) {
            return _link.OtherEnd(n);
        }

        /// <inheritdoc />
        public TNode OtherEnd(INode n) {
            if (!n.Equals(From) && !n.Equals(To))
                throw new Exception("Cannot return other end. Parameter 'node' must be connected to the link");
            return n.Equals(From) ? To : From;
        }

        #endregion
    }
}