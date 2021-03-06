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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using common.framework.interfaces.entities;

namespace Diagrams.Control.Impl.Entities {
    public class MappableEntity : IEntity {
        private readonly IEntity _entity;
        public MappableEntity(IEntity entity) {
            _entity = entity;
        }

        public override int GetHashCode() {
            return Name.GetHashCode();
        }

        #region IEntity Members

        public OpenMetaverse.UUID ID {
            get { return _entity.ID; }
        }

        public string Name {
            get {
                return _entity.Name;
            }
            set {
                _entity.Name = value;
            }
        }

        #endregion
    }
}
