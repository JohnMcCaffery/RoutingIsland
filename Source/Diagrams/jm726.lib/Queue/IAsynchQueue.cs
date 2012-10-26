using System;
namespace common {
    /// <summary>
    ///   An asynchronous queue for processing items.
    /// 
    ///   An item is queued and then a seperate thread will process the item using a supplied delegate.
    /// 
    ///   Can be _paused, restarted or stepped through.
    /// 
    ///   Can be configured to use multiple threads to process items to speed up the system. If this feature is enabled there is no guarantee that items will be returned in order.
    /// </summary>
    public interface IAsynchQueue {
        /// <summary>
        ///   Set whether the queue is _paused or not. Set to true to pause the queue, false to un pause it.
        /// </summary>
        bool Paused { get; set; }

        /// <summary>
        ///   Set whether the queue is _paused or not. Set to true to pause the queue, false to un pause it.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        ///   Check whether the system currently has messages queued.
        ///   Will return false even within a call to processDelegate
        /// </summary>
        bool IsWorking { get; }

        /// <summary>
        ///   Start the queue processing
        /// </summary>
        /// <param name = "name">The _name of the queue. The thread will be given this _name.</param>
        /// <param name = "itemsPerThread">[Optional]. If set > 0 then when there are more than _itemsPerThread items in the queue a new thread will be spawned to help process the items. 
        ///   By default only 1 thread will run</param>
        /// If this is included there is no guarantee that items will be returned in order.
        void Start(string name, int itemsPerThread = -1);

        /// <summary>
        /// Queue an item to be processed asynchronously specifiying the delegate that is to process the item.
        /// </summary>
        /// <typeparam name="TItem">The type of item to process</typeparam>
        /// <param name="name">The name of the process being run</param>
        /// <param name="processor">The delegate which will process the item</param>
        /// <param name="item">The item to process</param>
        void QItem<TItem>(string name, Action<TItem> processor, TItem item);

        /// <summary>
        /// Queue work to be performed asynchronously.
        /// TODO write test
        /// </summary>
        /// <param name="name">The name of the work to be done.</param>
        /// <param name="processor">The method which will do the work.</param>
        void QWork(string name, Action processor);

        /// <summary>
        ///   Stop the queue running, all queued items will be lost.
        /// </summary>
        void Stop();

        /// <summary>
        ///   When paused process the next item
        /// </summary>
        void Step();
    }
}