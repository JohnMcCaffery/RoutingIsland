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
using Diagrams;
using OpenMetaverse;
using common.framework.interfaces.basic;

#endregion

namespace core.view.interfaces {
    /// <summary>
    ///   Properties and fields useful for positioning the _link correctly in the visualisation
    /// </summary>
    public interface IVLink : ILink<IVNode> {
        event EntityDeletedDelegate OnSystemDelete;

        bool InWorld {
            get;
        }

        Vector3 Scale {
            get;
        }

        bool SilentDestroy();
    }
}