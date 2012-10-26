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

using common.framework.interfaces.basic;
using common.model.framework.interfaces;

#endregion

namespace core.view.interfaces {
    /// <summary>
    ///   The properties and methods used to visualise a packet travelling across the network
    /// </summary>
    public interface IVPacket : IPacket {
        bool InWorld {
            get;
        }

        /// <summary>
        ///   Print information about what caused the packet to drop and where it was
        /// </summary>
        /// <param name = "msg"></param>
        void Dropped(string msg);
    }
}