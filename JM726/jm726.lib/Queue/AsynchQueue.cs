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

#region Namespace imports

using System;
using System.Collections.Generic;
using System.Threading;
using Diagrams;
using JM726.Lib.Static;
using System.Linq;
using common.Queue;
using JM726.jm726.lib.Queue;

#endregion

namespace common {
    /// <summary>
    ///   Implementation of the IAsynchQueue interface
    /// TODO IMPLEMENTED
    /// </summary>
    [Serializable]
    public class AsynchQueue : IAsynchQueue, IQueueStats {
        private interface IProcessCollection {
            int Count { get; }
            Process Next { get; }

            void Add (Process process);
            void Add (Process process, bool highPriority);
            void Append (IProcessCollection collection);
            void Clear();
        }

        private class QueueCollection : IProcessCollection {
            private readonly object Busy = new object();
            /// <summary>
            ///   The stack of items to process
            /// </summary>
            private Queue<Process> _eventQLowPriority;

            /// <summary>
            ///   The stack of items to process
            /// </summary>
            private Queue<Process> _eventQHighPriority;

            internal QueueCollection() {
                _eventQLowPriority = new Queue<Process>();
                _eventQHighPriority = new Queue<Process>();
            }

            #region IProcessCollection Members

            public int Count {
                get { return _eventQLowPriority.Count + _eventQHighPriority.Count; }
            }

            public Process Next {
                get {
                    lock (Busy) {
                        if (Count == 0)
                            throw new QueueException();
                        return _eventQHighPriority.Count > 0 ?
                            _eventQHighPriority.Dequeue() :
                            _eventQLowPriority.Dequeue();
                    }
                }
            }

            public void Add(Process process) {
                Add(process, false);
            }

            public void Add(Process process, bool highPriority) {
                bool queued = false;
                while (!queued) {
                    try {
                        lock (Busy)
                            if (highPriority)
                                _eventQHighPriority.Enqueue(process);
                            else
                                _eventQLowPriority.Enqueue(process);
                        queued = true;
                    } catch (ArgumentException e) {
                        //The queue was full, wait for it to have more space
                        Util.Wait(1);
                    }
                }
            }

            public void Append(IProcessCollection collection) {
                while (collection.Count > 0)
                    Add(collection.Next);
            }

            public void Clear() {
                lock (Busy) {
                    _eventQHighPriority.Clear();
                    _eventQLowPriority.Clear();
                }
            }

            #endregion
        }

        private class StackCollection : IProcessCollection {
            private readonly object Busy = new object();
            /// <summary>
            ///   The stack of items to process
            /// </summary>
            private Stack<Process> _eventStackLowPriority;

            /// <summary>
            ///   The stack of items to process
            /// </summary>
            private Stack<Process> _eventStackHighPriority;

            internal StackCollection() {
                _eventStackLowPriority = new Stack<Process>();
                _eventStackHighPriority = new Stack<Process>();
            }

            #region IProcessCollection Members

            public int Count {
                get { return _eventStackLowPriority.Count + _eventStackHighPriority.Count; }
            }

            public Process Next {
                get {
                    lock (Busy) {
                        if (Count == 0)
                            throw new QueueException();
                        return _eventStackHighPriority.Count > 0 ? 
                            _eventStackHighPriority.Pop() :
                            _eventStackLowPriority.Pop();
                    }
                }
            }

            public void Add(Process process) {
                Add(process, false);
            }

            public void Add(Process process, bool highPriority) {
                bool queued = false;
                while (!queued) {
                    try {
                        lock (Busy)
                            if (highPriority)
                                _eventStackHighPriority.Push(process);
                            else
                                _eventStackLowPriority.Push(process);
                        queued = true;
                    } catch (ArgumentException e) {
                        //The queue was full, wait for it to have more space
                        Util.Wait(1);
                    }
                }
            }

            public void Append(IProcessCollection collection) {
                while (collection.Count > 0)
                    Add(collection.Next);
            }

            public void Clear() {
                lock (Busy) {
                    _eventStackHighPriority.Clear();
                    _eventStackLowPriority.Clear();
                }
            }

            #endregion
        }


        private readonly Object _startLock = new Object();

        /// <summary>
        ///   Whether the system is running
        /// </summary>
        protected bool _cont;

        /// <summary>
        ///   How many items there should be in the queue before new helper threads are spawned
        /// </summary>
        protected int ItemsPerThread = -1;

        /// <summary>
        ///   The _name to give to any threads started
        /// </summary>
        protected string _name;

        /// <summary>
        ///   Whether the system is _paused
        /// </summary>
        protected bool _paused;

        /// <summary>
        /// Thread that is used to run any processes queued.
        /// </summary>
        protected Thread ProcessThread;

        /// <summary>
        ///   Count of how many threads are running
        /// </summary>
        protected int ThreadCount;

        private readonly Dictionary<string, string> _threadTasks = new Dictionary<string,string>();
        private readonly List<Thread> _threads = new List<Thread>();

        private readonly object WorkFinished = new object();
        private readonly object WorkQueued = new object();
        private readonly object StepTriggered = new object();
        private readonly object Change = new object();

        private bool _shared;
        private bool _useStack;
        private IProcessCollection _toProcess;

        /// <inheritdoc />
        public bool UseStack {
            get {
                return _useStack;
            }
            set {
                bool wasPaused = Paused;
                Paused = true;
                _useStack = value;
                IProcessCollection oldCollection = _toProcess;
                _toProcess = _useStack ? (IProcessCollection) 
                    new StackCollection() : 
                    new QueueCollection();
                _toProcess.Append(oldCollection);
                Paused = wasPaused;
            }
        }

        /// <inheritdoc />
        public IQueueStats Statistics { get { return this; } }

        /// <inheritdoc />
        public string Name { get { return _name; } }

        /// <inheritdoc />
        public bool Paused {
            get { return _paused; }
            set { 
                _paused = value;
                if (!_paused) {
                    Util.Wake(StepTriggered);
                    Util.Wake(WorkQueued);
                }
            }
        }

        /// <inhertidoc />
        public int QueuedItems {
            get { return EventCount; }
        }

        /// <inheritdoc />
        public bool IsRunning {
            get { return _cont && ProcessThread != null && ProcessThread.IsAlive; }
        }

        /// <inheritdoc />
        public bool IsWorking {
            get { return _cont && ProcessThread != null && ProcessThread.IsAlive && !Finished; }
        }
        
        #region Internal Methods

        public void Start(String name, int itemsPerThread = -1) {
            Start(name, itemsPerThread, false);
        }

        /// <inheritdoc />
        public void Start(String name, int itemsPerThread = -1, bool shared = false) {
            _shared = shared;
            ItemsPerThread = itemsPerThread;
            _name = name;

            _toProcess = new QueueCollection();

            ProcessThread = new Thread(eventThreadMethod) {Name = name};
            ProcessThread.Start();
            _threads.Add(ProcessThread);
            _threadTasks.Add(ProcessThread.Name, "Startup");

            Util.Wait(500, !_cont, _startLock);
        }

        public void QWork(string name, Action work) {
            QProcess(name, () => Process.CreateProcess(name, work), false);
        }

        public void QWork(string name, Action work, bool highPriority) {
            QProcess(name, () => Process.CreateProcess(name, work), highPriority);
        }

        private void QProcess(string name, Func<Process> createProcess, bool highPriority) {
            if (!_cont)
                throw new InvalidOperationException("The queue is not running, unable to queue item.");

            _toProcess.Add(createProcess(), highPriority);

            _tasksQueued++;
            Util.Wake(WorkQueued);

            if (!_paused && ItemsPerThread > 0 && ThreadCount * ItemsPerThread < EventCount) {
                var extraEventThread = new Thread(extraEventThreadMethod);
                extraEventThread.Name = "Extra " + _name + " " + (ThreadCount + 1);
                extraEventThread.Start();
                _threads.Add(extraEventThread);
                _threadTasks[extraEventThread.Name] = "Startup";
            }
        }

        private int EventCount {
            get {
                return _toProcess.Count;
            }
        }

        private bool Finished {
            get {
                return EventCount == 0;
            }
        }

        /// <inheritdoc />
        public void Stop() {
            if (!_shared)
                ProperShutdown();
            else
                throw new Exception("Permission denied to shut down the shared queue.");
        }

        /// <inheritdoc />
        public void Step() {
            if (_paused) Util.Wake(StepTriggered);
        }

        public void BlockWhileWorking() {
            if (_paused)
                return;
            Util.Wake(WorkQueued);
            Util.Wake(StepTriggered);
            //Util.Wait(-1, !Finished, WorkFinished);
            Util.Wait(5000, !Finished, WorkFinished);
        }

        #endregion

        #region Thread

        private void eventThreadMethod() {
            //DB.ThreadStarted();
            ThreadCount++;
            _started = DateTime.Now;
            _cont = true;
            Util.Wake(_startLock);
            while (_cont) {
                eventThreadWork();
                Util.Wake(WorkFinished);
                Util.Wake(this);
                Util.Wait(target: WorkQueued, cont: _cont);
            }
            Util.Wake(WorkFinished);
            ThreadCount--;
            //DB.ThreadStopped();
        }

        private void extraEventThreadMethod() {
            _totalHelpers++;
            ThreadCount++;
            if (ThreadCount > _maxThreads + 1)
                _maxThreads = ThreadCount;
            DateTime start = DateTime.Now;
            lock (_activeHelperStarts)
                _activeHelperStarts.Add(start);

            eventThreadWork();

            _totalHelperTime += DateTime.Now.Subtract(start).TotalMilliseconds;
            lock (_activeHelperStarts)
                _activeHelperStarts.Remove(start);
            ThreadCount--;
        }

        private void eventThreadWork() {
            while (_cont && !Finished) {
                if (_paused && Thread.CurrentThread.Name.StartsWith("Extra " + _name + " "))
                    break;
                else if (_paused)
                    Util.Wait(target: StepTriggered);

                if (_cont/* && process != null*/) {
                    Process process = null;
                    try {
                        process = _toProcess.Next;
                        _threadTasks[Thread.CurrentThread.Name] = process.Name;
                        process.Run();
                        _tasksProcessed++;
                        _processTime += process.ProcessingTime;
                    } catch (QueueException e) {
                        //There was no item queued. Do nothing.
                    } catch (Exception e) {
                        _tasksFailed++;
                        //TODO log
                        //DB.Exception(e, "Queue unable to process item: " + e.Message, Level);
                        Console.WriteLine("Queue unable to process " + process.Name + ": " + e.Message);
                        Console.WriteLine(e.StackTrace);
                    }
                }
            }
        }

        #endregion

        #region IQueueStats Members

        private DateTime _started;
        private double _processTime = 0;
        private int _tasksProcessed = 0;
        private int _tasksFailed = 0;
        private int _tasksQueued = 0;
        private int _maxThreads = 0;
        private int _totalHelpers = 0;
        private double _totalHelperTime;
        private List<DateTime> _activeHelperStarts = new List<DateTime>();

        public double Time {
            get { return _started != null ? DateTime.Now.Subtract(_started).TotalMilliseconds + TotalHelperTime : 0; }
        }

        public double ActiveTime {
            get { return _processTime; }
        }

        public double SleepTime {
            get { return Time - _processTime; }
        }

        public double MeanProcessTime {
            get { return ProcessedTasks > 0 ? _processTime / ProcessedTasks : 0; }
        }

        public double PercentageActive {
            get { return _processTime / (Time / 100d); }
        }

        public int ProcessedTasks {
            get { return _tasksProcessed; }
        }

        public int FailedTasks {
            get { return _tasksFailed; }
        }

        public int QueuedTasks {
            get { return _tasksQueued; }
        }

        public int UnprocessedTasks {
            get { return _tasksQueued - _tasksProcessed; }
        }

        public string[] UnprocessedTaskNames {
            get {
                return new String[0];
                    //_eventQLowPriority.Select<Process, string>(process => process.Name).Concat(
                    //_eventQHighPriority.Select<Process, string>(process => process.Name)).Concat(
                    //_eventStackLowPriority.Select<Process, string>(process => process.Name)).Concat(
                    //_eventStackHighPriority.Select<Process, string>(process => process.Name)).ToArray();
                //return new String[] {
                //    "Q - Lo: " + _eventQLowPriority.Count, 
                //    "Q - Hi: " + _eventQHighPriority.Count, 
                //    "S - Lo: " + _eventStackLowPriority.Count, 
                //    "S - Hi: " + _eventStackHighPriority.Count };
            }
        }


        public int MaxHelperThreads {
            get { return _maxThreads; }
        }

        public int CurrentThreadCount {
            get { return ThreadCount - 1; }
        }

        public int TotalHelpersSpawned {
            get { return _totalHelpers; }
        }

        public double MeanHelperLife {
            get { return TotalHelperTime / _totalHelpers; }
        }

        public double TotalHelperTime {
            get {
                lock (_activeHelperStarts)
                    return _totalHelperTime + _activeHelperStarts.Select<DateTime, double>(start => DateTime.Now.Subtract(start).TotalMilliseconds).Sum(); 
            }
        }


        public string Breakdown {
            get {
                if (!IsRunning)
                    return "Not Running.";
                return string.Format("{0,-30} - # Tasks Processed: {1,-7} - Active: {2,-10} - Mean Process Time: {3}.\n",
                    Name,
                    ProcessedTasks,
                    String.Format("{0:.###}%", PercentageActive),
                    String.Format("{0:.###}ms", MeanProcessTime));
            }
        }

        public override string ToString() {
            return Breakdown;
        }

        #endregion

        internal void ProperShutdown() {
            //if (_eventQLowPriority != null)
            //    lock (_eventQLowPriority)
            //        _eventQLowPriority.Clear();
            //if (_eventQHighPriority != null)
            //    lock (_eventQLowPriority)
            //        _eventQHighPriority.Clear();
            //if (_eventStackLowPriority != null)
            //    lock (_eventStackLowPriority)
            //        _eventStackLowPriority.Clear();
            //if (_eventStackHighPriority != null)
            //    lock (_eventStackLowPriority)
            //        _eventStackHighPriority.Clear();
            _toProcess.Clear();
            _cont = false;
            Util.Wake(WorkFinished);
            Util.Wake(WorkQueued);
            Util.Wake(StepTriggered);
            if (ProcessThread != null)
                ProcessThread.Join(1000);
            foreach (var thread in _threads) {
                if (thread.IsAlive) {
                    Console.WriteLine("Killing " + thread.Name + ". Current task: " + _threadTasks[thread.Name]);
                    thread.Abort();
                }
            }
        }
    }
}