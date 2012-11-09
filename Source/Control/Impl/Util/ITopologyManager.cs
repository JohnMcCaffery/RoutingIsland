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

namespace Diagrams.Control.impl.Util {
    public interface ITopologyManager {
        /// <summary>
        /// Load a topology from a specified XML events.
        /// </summary>
        void LoadTopology(string user, UUID userID, string file);

        /// <summary>
        /// Save the current topology to an XML events.
        /// </summary>
        void SaveTopology(string name, UUID id, string file);

        string GetUserFolder(string name);

        /// <summary>
        /// The folder where shared topologies are stored.
        /// </summary>
        string SharedFolder { get; }
    }
}
