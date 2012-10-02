/*************************************************************************
Copyright (c) 2012 John McCaffery 

This file is part of JohnLib.

JohnLib is free software: you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

JohnLib is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License
along with JohnLib.  If not, see <http://www.gnu.org/licenses/>.

**************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace jm726.lib.wrapper {
    public interface IWrapper<out TToWrap> where TToWrap : class {
        /// <summary>
        /// The instance to interact with in order for the interactions to be spied.
        /// </summary>
        TToWrap Instance { get; }
        /// <summary>
        /// Whether or not to generate events whenever an event is trigged from the instance being spied on.
        /// </summary>
        bool Listen { get; set; }
        /// <summary>
        /// The type of the interface being wrapped.
        /// </summary>
        Type WrappedInterface { get; }
    }
}
