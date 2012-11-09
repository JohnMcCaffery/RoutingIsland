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
using System.Drawing;
using Diagrams;
using OpenMetaverse;
using common.framework.abs.wrapper;
using common.framework.impl.util;
using common.interfaces.entities;
using core.view.interfaces;
using common.framework.interfaces.entities;
using common.framework.interfaces.basic;
using System.Diagnostics;

namespace core.view.impl.entities {
    // TODO IMPLEMENTED
    public class VLink : PrimWrapper, IVLink {
        private static readonly Vector3 ZAxis = new Vector3(0f, 0f, 1f);
        public static float MinWidth = .05f;
        public static float MaxWidth = .4f;
        private float _weight;
        private bool _distanceWeight = true;

        public VLink(IPrimFactory primFactory, IVNode from, IVNode to, float weight = default(float), Parameters parameters = null, bool bidirectional = true)
            : base(primFactory, GetPos(from.Pos, to.Pos), "Link between " + from.Name + " and " + to.Name, Color.White, 0d, parameters) {
            
            From = from;
            To = to;
            IsBidirectional = bidirectional;
            Parameters = parameters == null ? new Parameters() : parameters;

            Weight = weight;

            from.OnWorldMove += (id, oldPos, newPos) => LengthChanged(oldPos, To.Pos);
            to.OnWorldMove += (id, oldPos, newPos) => LengthChanged(From.Pos, oldPos);
            from.OnAPIMove += (id, oldPos, newPos) => LengthChanged(oldPos, To.Pos);
            to.OnAPIMove += (id, oldPos, newPos) => LengthChanged(From.Pos, oldPos);

            Position();
        }

        protected override IPrim InitPrim(IPrimFactory primFactory, Vector3 position) {
            IPrim p = primFactory.MakePrim(Name, position, DefaultColour, new Vector3(0f, 0f, .001f), PrimType.Cylinder);
            p.Editable = false;
            return p;
        }

        private void LengthChanged(Vector3 oldFrom, Vector3 oldTo) {
            Position();
            if (OnLengthChanged != null && !Length.Equals(Vector3.Distance(oldFrom, oldTo)))
                OnLengthChanged(ID, Length);
        }

        #region IVLink Members

        /// <inheritdoc />
        public event WeightChangedDelegate OnWeightChanged;

        /// <inheritdoc />
        public event WeightChangedDelegate OnLengthChanged;

        /// <inheritdoc />
        public IVNode From { get; set; }

        /// <inheritdoc />
        public IVNode To { get; set; }

        /// <inheritdoc />
        public UUID FromID {
            get { return From.ID; }
        }

        /// <inheritdoc />
        public UUID ToID {
            get { return To.ID; }
        }

        public bool InWorld {
            get { return Prim.InWorld; }
        }

        /// <inheritdoc />
        public bool IsBidirectional { get; set; }

        public bool DistanceWeight {
            get {
                return _distanceWeight;
            }
            set {
                _distanceWeight = value;
                if (_distanceWeight != value)
                    Prim.Scale = GetScale(From, To, value, _weight, Length);
                
            }
        }

        public float Length {
            get { return Vector3.Distance(From.Pos, To.Pos); }
        }

        /// <inheritdoc />
        public float Weight {
            get { return _weight; }
            set {
                float oldWeight = Weight;
                _weight = value;
                Prim.Scale = GetScale(From, To, DistanceWeight, value, Length);
                if (OnWeightChanged != null && !_weight.Equals(oldWeight))
                    OnWeightChanged(ID, _weight);
            }
        }

        /// <inheritdoc />
        public UUID OtherEnd(UUID n) {
            if (!n.Equals(To.ID) && !n.Equals(From.ID))
                throw new Exception("Cannot return other end. Parameter 'node' must be connected to link.");
            return From.ID.Equals(n) ? To.ID : From.ID;
        }

        /// <inheritdoc />
        public IVNode OtherEnd(INode node) {
            if (!node.Equals(To) && !node.Equals(From))
                throw new Exception("Cannot return other end. Parameter 'node' must be connected to link.");
            return From.Equals(node) ? To : From;
        }

        #endregion

        /// <summary>
        /// Position the link between its two nodes
        /// </summary>
        private void Position() {
            Prim.Pos = GetPos(From.Pos, To.Pos);
            Prim.Rotation = GetRotatation(From, To);
            Prim.Scale = GetScale(From, To, DistanceWeight, _weight, Length);
        }

        internal static Vector3 GetPos(Vector3 from, Vector3 to) {
            Vector3 diff = Vector3.Subtract(to, from);
            Vector3 midDiff = Vector3.Divide(diff, 2f);
            return Vector3.Add(from, midDiff);
        }

        internal static Vector3 GetScale(IVNode from, IVNode to, bool distanceWeight, float weight, float length) {
            float width = MinWidth;

            if (!distanceWeight) {
                float weightInverse = (weight > 1f ? 1f : weight);
                float widthRange = (MaxWidth - MinWidth) / 1;
                float zeroedWidth = weightInverse * widthRange;
                width = MinWidth + zeroedWidth;
            } 

            if (width < MinWidth)
                width = MinWidth;
            else if (width > MaxWidth)
                width = MaxWidth;

            return new Vector3(width, width, length);
        }

        internal static Quaternion GetRotatation(IVNode from, IVNode to) {
            Vector3 vector = Vector3.Normalize(Vector3.Subtract(from.Pos, to.Pos));
            Vector3 cross = Vector3.Cross(vector, ZAxis);
            float angle = (float)Math.Acos(Vector3.Dot(vector, ZAxis));
            return Quaternion.CreateFromAxisAngle(cross, angle * -1f);
        }

        public override bool Destroy() {
            Prim.Editable = true;
            bool ret = Prim.Destroy();
            if (OnSystemDelete != null)
                OnSystemDelete(ID);
            return ret;
        }

        public bool SilentDestroy() {
            return Prim.Destroy();
        }

        public Vector3 Scale {
            get { return Prim.Scale; }
        }

        public event EntityDeletedDelegate OnSystemDelete;
    }
}