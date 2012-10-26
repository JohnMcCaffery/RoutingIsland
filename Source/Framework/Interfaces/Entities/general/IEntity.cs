﻿/*************************************************************************
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

using OpenMetaverse;

#endregion

namespace common.framework.interfaces.entities {
    public interface IEntity {
        /// <summary>
        ///   Global ID of the entity
        /// </summary>
        UUID ID { get; }

        /// <summary>
        ///   The name of this entity
        /// </summary>
        string Name { get; set; }
    }
}