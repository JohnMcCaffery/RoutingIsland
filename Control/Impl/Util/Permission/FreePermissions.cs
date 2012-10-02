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
using Diagrams.MRM.Controls;
using common.interfaces.entities;
using OpenMetaverse;
using Diagrams.MRM.Controls.Buttons;
using Diagrams.Framework.Interfaces.Entities.general;

namespace Diagrams.Control.impl.Util {
    /// <summary>
    /// Permission instance that will allow any change.
    /// </summary>
    public class FreePermissions : IPermissions {
        public FreePermissions() {
        }

        #region IPermissions Members

        public bool Authorize(IOwned entity, UUID id) {
            return true;
        }

        #endregion
    }
}
