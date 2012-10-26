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

using common;
using Diagrams;

#endregion

namespace algorithms.distanceVector.impl.util {
    public class DistanceVector : MapKeyTable<Distance> {
        /// <summary>
        ///   Copy the contents of one DistanceVector to another with a new set of objects
        /// </summary>
        /// <returns></returns>
        internal DistanceVector copy() {
            var v = new DistanceVector();
            foreach (Distance d in this)
                v.Add(d.Target.ID, d.copy());

            return v;
        }
    }
}