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

using System;
using algorithms.distanceVector.impl.util;
using common.framework.impl.util;
using common.framework.interfaces.entities;
using common.model.framework.interfaces;
using Diagrams;
using System.Drawing;
using diagrams.algorithms.dv;

#endregion

namespace algorithms.distanceVector.impl.entities {
    public class DVPacket : MPacket {
        private readonly bool _highlighting;
        protected DistanceVector _distanceVector;
        private int _ttl;

        public DVPacket(IMNodeInternal from, INode to, DistanceVector distanceVector, bool visualise = false, int TTL = 51)
            : base(from, to, from,new Parameters("Visualise", visualise), DV.DV_NAME, visualise) {

            _distanceVector = distanceVector;
            _highlighting = false;
            _ttl = TTL-1;
            Name = "Distance Vector " + Name;
            Selected = 1d;
        }


        public DistanceVector DistanceVector {
            get { return _distanceVector; }
        }

        internal bool Highlighting {
            get { return _highlighting; }
        }

        public int TTL { 
            get { return _ttl; } 
        }
    }
}