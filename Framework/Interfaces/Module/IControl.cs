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

using OpenMetaverse;

namespace common.framework.interfaces.layers {
    /// <summary>
    ///   public interface for controllers which control the system as a whole. Controller handles the controller functions in the Model View Controller model.
    /// 
    ///   Controller works in terms of MRM types (IObject, Vector3) and has no knowledge of the date structures / classes that make up the model.
    /// </summary>
    public interface IControl {
        /// <summary>
        ///   Clear
        /// </summary>
        /// <param name = "senderID">The ID of the object requesting the Clear</param>
        /// <param name = "sender">The sender of the user requesting the Clear</param>
        void Clear(string sender, UUID senderID);

        /// <summary>
        /// Stop any threads the manager has started.
        /// </summary>
        void Stop();
    }
}