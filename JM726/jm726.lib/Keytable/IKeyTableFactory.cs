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

namespace Diagrams.Common.interfaces.keytable {
    public interface IKeyTableFactory {
        /// <summary>
        /// Make a new KeyTable
        /// </summary>
        /// <typeparam name="TValue">The type of value which the key table manages</typeparam>
        /// <returns>The new key table</returns>
        IKeyTable<TValue> MakeKeyTable<TValue>();
    }
}
