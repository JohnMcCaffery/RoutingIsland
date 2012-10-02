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
using jm726.lib.wrapper;

namespace jm726.lib.wrapper.spy {
    public interface ISpy<out TToWrap> : IWrapper<TToWrap> where TToWrap : class {
        /// <summary>
        /// Triggered when an event listener is added to an event.
        /// </summary>
        event EventModifyDelegate OnEventAdd;

        /// <summary>
        /// Triggered when an event listener is removed from an event.
        /// </summary>
        event EventModifyDelegate OnEventRemove;

        /// <summary>
        /// Triggered whenever the instance being spied on triggers and event.
        /// </summary>
        event EventTriggeredDelegate OnEventTriggered;

        /// <summary>
        /// Triggered whenever a regular method is called.
        /// </summary>
        event MethodCallDelegate OnMethodCall;

        /// <summary>
        /// Triggered whenever any method is invoked (both special methods such as property getters and setters and regular methods).
        /// </summary>
        event MethodCallDelegate OnMethodEvent;

        /// <summary>
        /// Triggered whenever a properties getter is invoked.
        /// </summary>
        event PropertyInteractDelegate OnPropertyGet;

        /// <summary>
        /// Triggered whenever a properties setter is invoked.
        /// </summary>
        event PropertyInteractDelegate OnPropertySet;
    }
}
