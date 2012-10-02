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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using common.model.framework.interfaces;

namespace Diagrams {
    /// <summary>
    /// Delegate for when the route to a given target changes.
    /// </summary>
    /// <param name="target">The target the route to which has changed.</param>
    /// <param name="oldRoute">The link that used to be used to route to target. Will be null if no route previously existed.</param>
    /// <param name="newRoute">The link that is now to be used to route to target. Will be null if the target can no longer be routed to.</param>
    public delegate void ForwardingTableChangeDelegate(string alg, IMNodeInternal target, IMLink oldRoute, IMLink newRoute, float oldDistance, float distance);
}
