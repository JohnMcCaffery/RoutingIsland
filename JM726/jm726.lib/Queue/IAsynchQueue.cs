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
namespace common {
    /// <summary>
    /// Statistics about the usage of a queue.
    /// </summary>
    public interface IQueueStats {
        /// <summary>
        /// The amount of time the queue has been running.
        /// </summary>
        double Time { get; }
        /// <summary>
        /// The amount of time the queue was processing.
        /// </summary>
        double ActiveTime { get; }
        /// <summary>
        /// The amount of time the queue was sleeping.
        /// </summary>
        double SleepTime { get; }
        /// <summary>
        /// The average time spent processing a single task.
        /// </summary>
        double MeanProcessTime { get; }
        /// <summary>
        /// The percentage of time that the process was active.
        /// </summary>
        double PercentageActive { get; }
        /// <summary>
        /// The number of tasks that have been processed.
        /// </summary>
        int ProcessedTasks { get; }
        /// <summary>
        /// The number of tasks that threw exceptions whilst processing.
        /// </summary>
        int FailedTasks { get; }
        /// <summary>
        /// The total number of tasks that have been queued.
        /// </summary>
        int QueuedTasks { get; }
        /// <summary>
        /// The number of tasks that have been queued but not processed.
        /// </summary>
        int UnprocessedTasks { get; }
        /// <summary>
        /// The names of all the tasks that were not processed.
        /// </summary>
        string[] UnprocessedTaskNames { get; }

        /// <summary>
        /// The maximum number of threads the queue ever used for processing.
        /// </summary>
        int MaxHelperThreads { get; }
        /// <summary>
        /// The current number of threads the queue is using for processing.
        /// </summary>
        int CurrentThreadCount { get; }
        /// <summary>
        /// The total number of helper threads that have been spawned so far.
        /// </summary>
        int TotalHelpersSpawned { get; }
        /// <summary>
        /// The mean length of time a helper thread runs for.
        /// </summary>
        double MeanHelperLife { get; }
        /// <summary>
        /// The sum of the length of time each helper thread ran or has been running for.
        /// </summary>
        double TotalHelperTime { get; }

        /// <summary>
        /// Breakdown of the statistics.
        /// </summary>
        string Breakdown { get; }
    }

    /// <summary>
    ///   An asynchronous queue for processing items.
    /// 
    ///   An item is queued and then a seperate thread will process the item using a supplied delegate.
    /// 
    ///   Can be paused, restarted or stepped through.
    /// 
    ///   Can be configured to use multiple threads to process items to speed up the system. If this feature is enabled there is no guarantee that items will be returned in order.
    /// </summary>
    public interface IAsynchQueue {
        /// <summary>
        /// The name of the queue.
        /// </summary>
        string Name { get; }
        /// <summary>
        ///   Set whether the queue is _paused or not. Set to true to pause the queue, false to un pause it.
        /// </summary>
        bool Paused { get; set; }

        /// <summary>
        ///   Set whether the queue is _paused or not. Set to true to pause the queue, false to un pause it.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Get the number of items that are currently queued.
        /// </summary>
        int QueuedItems { get; }

        /// <summary>
        ///   Check whether the system currently has messages queued.
        ///   Will return false even within a call to processDelegate
        /// </summary>
        bool IsWorking { get; }

        /// <summary>
        /// Whether to use a stack or a queue when deciding the order that events are processed. Default is a stack.
        /// </summary>
        bool UseStack { get; set; }

        /// <summary>
        ///   Start the queue processing
        /// </summary>
        /// <param name = "name">The _name of the queue. The thread will be given this _name.</param>
        /// <param name = "itemsPerThread">[Optional]. If set > 0 then when there are more than _itemsPerThread items in the queue a new thread will be spawned to help process the items. 
        ///   By default only 1 thread will run</param>
        /// If this is included there is no guarantee that items will be returned in order.
        void Start(string name, int itemsPerThread = -1);

        /// <summary>
        /// Queue work to be performed asynchronously.
        /// TODO write test
        /// </summary>
        /// <param name="name">The name of the work to be done.</param>
        /// <param name="processor">The method which will do the work.</param>
        void QWork(string name, Action processor);

        /// <summary>
        /// Queue work to be performed asynchronously.
        /// TODO write test
        /// </summary>
        /// <param name="name">The name of the work to be done.</param>
        /// <param name="processor">The method which will do the work.</param>
        /// <param name="highPriority">Whether to queue this event as a high priority event.</param>
        void QWork(string name, Action processor, bool highPriority);

        /// <summary>
        ///   Stop the queue running, all queued items will be lost.
        /// </summary>
        void Stop();

        /// <summary>
        ///   When paused process the next item
        /// </summary>
        void Step();

        /// <summary>
        /// Block until the queue has no more work to do.
        /// If paused should return immediately to avoid lockups.
        /// </summary>
        void BlockWhileWorking();

        /// <summary>
        /// Statistics about how the queue has been performing.
        /// </summary>
        IQueueStats Statistics { get; }
    }
}