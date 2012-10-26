#region Namespace imports

using System;
using System.Collections.Generic;
using System.Threading;
using Diagrams;

#endregion

namespace common {
    /// <summary>
    ///   Implementation of the IAsynchQueue interface
    /// TODO IMPLEMENTED
    /// </summary>
    public class AsynchQueue : IAsynchQueue {
        private readonly Object _startLock = new Object();

        /// <summary>
        ///   Whether the system is running
        /// </summary>
        protected bool Cont;

        /// <summary>
        ///   How many items there should be in the queue before new helper threads are spawned
        /// </summary>
        protected int ItemsPerThread = -1;

        /// <summary>
        ///   The _name to give to any threads started
        /// </summary>
        protected string Name;

        /// <summary>
        ///   Whether the system is _paused
        /// </summary>
        protected bool _paused;

        /// <summary>
        /// Thread that is used to run any processes queued.
        /// </summary>
        protected Thread ProcessThread;

        /// <summary>
        ///   The queue of items to process
        /// </summary>
        protected Queue<Process> Q;

        /// <summary>
        ///   Count of how many threads are running
        /// </summary>
        protected int ThreadCount;

        #region Internal Properties

        /// <inheritdoc />
        public bool Paused {
            get { return _paused; }
            set { _paused = value; }
        }

        /// <inheritdoc />
        public bool IsRunning {
            get { return Cont && ProcessThread != null && ProcessThread.IsAlive; }
        }

        /// <inheritdoc />
        public bool IsWorking {
            get { return Cont && ProcessThread != null && ProcessThread.IsAlive && Q.Count != 0; }
        }

        #endregion
        
        #region Internal Methods

        /// <inheritdoc />
        public void Start(String name, int itemsPerThread = -1) {
            ItemsPerThread = itemsPerThread;
            Name = name;

            Q = new Queue<Process>();
            ProcessThread = new Thread(eventThreadMethod) {Name = name};
            ProcessThread.Start();

            Util.Wait(500, !Cont, _startLock);
        }

        /// <inheritdoc />
        public void QItem<TItem>(string name, Action<TItem> processor, TItem item) {
            QProcess(name, () => Process.CreateProcess(name, processor, item));
        }

        public void QWork(string name, Action work) {
            QProcess(name, () => Process.CreateProcess(name, work));
        }

        private void QProcess(string name, Func<Process> createProcess) {
            if (!Cont)
                throw new InvalidOperationException("The queue is not running, unable to queue item");

            bool queued = false;
            while (!queued)
                try {
                    lock (Q)
                        Q.Enqueue(createProcess());
                    queued = true;
                } catch (ArgumentException e) {
                    //The queue was full, wait for it to have more space
                    Util.Wait(1);
                }

            if (!_paused) Util.Wake(this);

            if (ItemsPerThread > 0 && ThreadCount * ItemsPerThread < Q.Count) {
                var extraEventThread = new Thread(extraEventThreadMethod);
                extraEventThread.Name = "Extra " + Name + " " + (ThreadCount + 1);
                extraEventThread.Start();
            }
        }

        /// <inheritdoc />
        public void Stop() {
            lock (Q)
                Q.Clear();
            Cont = false;
            Util.Wake(this);
            ProcessThread.Join();
        }

        /// <inheritdoc />
        public void Step() {
            if (_paused) Util.Wake(this);
        }

        #endregion

        #region Thread

        private void eventThreadMethod() {
            //DB.ThreadStarted();
            ThreadCount++;
            Cont = true;
            Util.Wake(_startLock);
            while (Cont) {
                eventThreadWork();
                Util.Wait(target: this, cont: Cont);
            }
            ThreadCount--;
            //DB.ThreadStopped();
        }

        private void extraEventThreadMethod() {
            //DB.ThreadStarted();
            ThreadCount++;
            //DB.Print("Extra event thread started, thread count = " + threads, Levels.ALGORITHM);
            eventThreadWork();
            ThreadCount--;
            //DB.Print("Extra event thread stopped, thread count = " + threads, Levels.ALGORITHM);
            //DB.ThreadStopped();
        }

        private void eventThreadWork() {
            Process process = default(Process);
            while (Cont && Q.Count > 0) {
                try {
                    try {
                        lock (Q)
                            process = Q.Dequeue();
                    }
                    catch (InvalidOperationException e) {
                        /* Do Nothing */
                    }
                }
                catch (Exception e) {
                    //TODO log
                    //DB.Exception(e, "Error in " + Name, Level);
                }
                if (Cont && process != null)
                    try {
                        process.Run();
                    } catch (Exception e) {
                        //TODO log
                        //DB.Exception(e, "Queue unable to process item: " + e.Message, Level);
                    }
            }
        }

        #endregion
    }
}