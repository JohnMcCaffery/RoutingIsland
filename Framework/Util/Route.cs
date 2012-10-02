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
using OpenMetaverse;
using common.framework.interfaces.entities;
using System.Drawing;

namespace Diagrams.Framework.Util {
    public struct Route {
        public string From;
        public UUID FromID;
        public Color FromColour;
        public string Target;
        public UUID TargetID;
        public Color TargetColour;
        public string Hop;
        public UUID HopID;
        public Color HopColour;
        public string Link;
        public UUID LinkID;
        public float Distance;

        public Route(INode from, INode target, INode hop, ILink link, float distance) {
            From = from.Name;
            Target = target.Name;
            Hop = hop.Name;
            Link = link.Name;
            
            FromColour = from.Colour;
            TargetColour = target.Colour;
            HopColour = hop.Colour;

            FromID = from.ID;
            TargetID = target.ID;
            HopID = hop.ID;
            LinkID = link.ID;

            Distance = distance;
        }

        public override string ToString() {
            return Target + " : " + Hop + " : " + Distance;
        }
    }
}
