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

using common.framework.interfaces.entities;
using common.model.framework.interfaces;
using Diagrams;
using System;

#endregion

namespace algorithms.distanceVector.impl.util {
    public class Distance {
        private readonly IMNodeInternal target;
        private float distance;
        private IMNodeInternal hop;
        private IMLink link;

        public Distance(IMNodeInternal target, IMNodeInternal hop, IMLink link, float distance) {
            if (target == null)
                throw new Exception("Target node must be set when initialising distance to target");
            if (hop == null)
                throw new Exception("First hop must be set when initialising distance to target");
            if (link == null)
                throw new Exception("First hop link must be set when initialising distance to target");
            //if (distance <= 0)
            //    throw new AlgorithmException("Distance must be positive when initialising distance to target");

            this.target = target;
            this.hop = hop;
            this.link = link;
            this.distance = distance;
        }

        public Distance(Distance distance, IMNodeInternal hop, IMLink link, float hopWeight)
            : this(distance.Target, hop, link, distance.Dist + hopWeight) {}

        public IMNodeInternal Target {
            get { return target; }
        }

        public IMNodeInternal Hop {
            get { return hop; }
        }

        public IMLink Link {
            get { return link; }
        }

        public float Dist {
            get { return distance; }
        }

        internal Distance copy() {
            return new Distance(target, hop, link, distance);
        }

        internal void update(float dist) {
            distance = dist;
        }

        internal void update(IMNodeInternal hop, IMLink link, float dist) {
            if (hop == null)
                throw new Exception("First hop must be set when updating distance to target");
            if (link == null)
                throw new Exception("First hop link must be set when updating distance to target");
            //if (distance <= 0)
            //    throw new AlgorithmException("Distance must be positive when updating distance to target");

            this.hop = hop;
            this.link = link;
            update(dist);
        }

        public override string ToString() {
            return target.Name + " via " + hop.Name + " along " + link.Name + " is distance " + distance;
        }
    }
}