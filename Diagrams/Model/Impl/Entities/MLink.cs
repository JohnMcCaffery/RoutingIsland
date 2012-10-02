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
using common.framework.abs.wrapper;
using common.framework.interfaces.entities;
using common.model.framework.interfaces;
using OpenMetaverse;

namespace Diagrams {
    public class MLink : LinkWrapper<IMNode>, IMLink {
        private static float _longestLength = 10f;

        private static float LongestLength {
            get {
                return _longestLength;
            }
        }
        private static void CheckLongestLink(float length) {
            if (length > _longestLength) {
                _longestLength = length * 1.5f;
                if (LongestLinkChanged != null)
                    LongestLinkChanged(_longestLength);
            }
        }
        private static Action<float> LongestLinkChanged;

        //TODO LongestLength
        //TODO never supplies a weight that isn't between 0 and 1

        public MLink(ILink link, IMNode from, IMNode to, float weight = default(float))
            : base(link, from, to) {


            base.DistanceWeight = weight == default(float);
            base.Weight = DistanceWeight ? LengthWeight : weight;
            OnLengthChanged += (id, oldLength) => {
                if (DistanceWeight)
                    base.Weight = LengthWeight;
            };
            LongestLinkChanged += newLongest => {
                if (DistanceWeight)
                    base.Weight = LengthWeight;
            };
        }

        private float LengthWeight {
            get { return Length / LongestLength; }
        }

        public override float Length {
            get {
                CheckLongestLink(base.Length);
                return base.Length;
            }
        }

        public override bool DistanceWeight {
            get {
                return base.DistanceWeight;
            }
            set {
                //If the weight has changed either using distance weight or stopping using
                //it so should be distance weight before it is changed
                if (base.DistanceWeight != value) {
                    base.DistanceWeight = value;
                    base.Weight = LengthWeight;
                }
            }
        }

        public override float Weight {
            get {
                return base.Weight;
            }
            set {
                DistanceWeight = false;
                if (value > 1f)
                    base.Weight = 1f;
                else if (value <= 0f)
                    base.Weight = .01f;
                else
                    base.Weight = value;
            }
        }

        public override bool Destroy() {
            return true;
        }
    }
}