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

namespace Diagrams {
    public interface IWrappedLink : ILink {
        /// <summary>
        /// Trigger an OnWeightChanged event.
        /// </summary>
        /// <param name="newWeight">The new weight that has been set. Weight is expected to be between 0 and 1.</param>
        void TriggerOnWeightChanged(float newWeight);
    }
}