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

using System.Collections.Generic;
namespace common.Queue {
    public interface IAsynchQueueFactory {
        /// <summary>
        /// Statistics across all the queues created by this factory.
        /// </summary>
        IQueueStats Statistics { get; }
        /// <summary>
        /// All the queues this queue has started.
        /// </summary>
        List<IAsynchQueue> Queues { get; }
        /// <summary>
        /// Get a singleton queue.
        /// </summary>
        IAsynchQueue SharedQueue {
            get;
        }

        /// <summary>
        /// Make a queue
        /// </summary>
        /// <returns></returns>
        IAsynchQueue MakeQueue();
        /// <summary>
        /// Shutdown every queue that has been created.
        /// </summary>
        void ShutdownAll();
    }
}