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

using System.Drawing;
using common.framework.impl.util;
using common.framework.interfaces.entities;

#endregion

namespace common.model.framework.interfaces {

    #region Delegate methods

    #endregion

    /// <summary>
    ///   Interface for any properties or methods required by a packet that are only accessed at the Simulation Layer
    /// </summary>
    public interface IPacket : ILogicEntity {

        /// <summary>
        /// Whether to visualise the packet travelling across the network.
        /// </summary>
        bool Visualise {
            get;
            set;
        }
    }
}