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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using common;
using common.Queue;
using System.Collections;
using OpenSim.Region.OptionalModules.Scripting.Minimodule.Interfaces;

namespace Diagrams.Framework.Util {
    public class MicroThreadQueueFactory : IQueueStats {

        private List<IAsynchQueue> _queues = new List<IAsynchQueue>();
        private MicroThreadQueue _sharedQ;

        #region IAsynchQueueFactory Members

        public IQueueStats Statistics { get { return this; } }

        public List<IAsynchQueue> Queues {
            get { return new List<IAsynchQueue>(_queues); }
        }

        private IMicrothreader _threader;

        public MicroThreadQueueFactory(IMicrothreader threader) {
            _threader = threader;
        }

        /// <summary>
        /// Get a singleton queue.
        /// </summary>
        public IAsynchQueue SharedQueue {
            get {
                if (_sharedQ == null) {
                    _sharedQ = new MicroThreadQueue();
                    //_sharedQ.Start("Shared Queue", 5, true);
                    _sharedQ.Start("Shared Queue", -1, true);
                    _queues.Add(_sharedQ);
                }
                return _sharedQ;
            }
        }

        public IAsynchQueue MakeQueue() {
            MicroThreadQueue q = new MicroThreadQueue();
            _queues.Add(q);
            _threader.Run(q);
            return q;
        }

        public void ShutdownAll() {
            foreach (MicroThreadQueue q in _queues)
                q.ProperShutdown();
            //_queues.Clear();
        }

        #endregion

        #region IQueueStats Members

        public double Time {
            get { return _queues.Sum(q => q.Statistics.Time); }
        }

        public double ActiveTime {
            get { return _queues.Sum(q => q.Statistics.ActiveTime); }
        }

        public double SleepTime {
            get { return _queues.Sum(q => q.Statistics.SleepTime); }
        }

        public double MeanProcessTime {
            get { return _queues.Sum(q => q.Statistics.MeanProcessTime) / _queues.Count; }
        }

        public double PercentageActive {
            get { return _queues.Sum(q => q.Statistics.PercentageActive) / _queues.Count; }
        }

        public int ProcessedTasks {
            get { return _queues.Sum(q => q.Statistics.ProcessedTasks); }
        }

        public int FailedTasks {
            get { return _queues.Sum(q => q.Statistics.FailedTasks); }
        }

        public int QueuedTasks {
            get { return _queues.Sum(q => q.Statistics.ProcessedTasks); }
        }

        public int UnprocessedTasks {
            get { return _queues.Sum(q => q.Statistics.ProcessedTasks); }
        }

        public string[] UnprocessedTaskNames {
            get { return _queues.SelectMany(queue => queue.Statistics.UnprocessedTaskNames.Select(task => queue.Name + " : " + task)).ToArray(); }
        }




        public int MaxHelperThreads {
            get { return _queues.Max(q => q.Statistics.MaxHelperThreads); }
        }

        public int CurrentThreadCount {
            get { return _queues.Sum(q => q.Statistics.CurrentThreadCount); }
        }

        public int TotalHelpersSpawned {
            get { return _queues.Sum(q => q.Statistics.TotalHelpersSpawned); }
        }

        public double MeanHelperLife {
            get { return _queues.Sum(q => q.Statistics.MeanHelperLife) / _queues.Count; }
        }

        public double TotalHelperTime {
            get { return _queues.Sum(q => q.Statistics.TotalHelperTime); }
        }

        public string Breakdown {
            get {
                string lines = "Queue Factory Statistics\n";
                _queues.ForEach(q => lines += q.Statistics.Breakdown);
                return lines;
            }
        }

        #endregion
    }

    public class MicroThreadQueue : IAsynchQueue, IEnumerator, IEnumerable, IQueueStats {
        private readonly Queue<KeyValuePair<string, Action>> _eventQ = new Queue<KeyValuePair<string, Action>>();
        private readonly Stack<KeyValuePair<string, Action>> _eventStack = new Stack<KeyValuePair<string,Action>>(); 
        private int _itemsPerThread = 1;
        private string _name;
        private bool _cont;
        private bool _paused;
        private bool _useStack;
        private bool _step;
        private bool _shared;

        private int Count {
            get { return _useStack ? _eventStack.Count : _eventQ.Count; }
        }

        #region IAsynchQueue Members

        public string Name {
            get { return _name; }
        }

        public bool Paused {
            get { return _paused; }
            set { _paused = value; }
        }

        public bool IsRunning {
            get { return _cont; }
        }

        public int QueuedItems {
            get { return Count; }
        }

        public bool IsWorking {
            get { return Count > 0; }
        }

        public bool UseStack {
            get { return _useStack; }
            set {
                _useStack = value;
                if (value)
                    while (_eventQ.Count > 0)
                        _eventStack.Push(_eventQ.Dequeue());
                else
                    while (_eventStack.Count > 0)
                        _eventQ.Enqueue(_eventStack.Pop());
            }
        }

        public void Start(string name, int itemsPerThread = -1) {
            _name = name;
            _itemsPerThread = itemsPerThread > 0 ? itemsPerThread : int.MaxValue;
            _started = DateTime.Now;
        }

        internal void Start(string name, int itemsPerThread, bool shared) {
            Start(name, itemsPerThread);
            this._shared = shared;
        }

        public void QWork(string name, Action processor) {
            QWork(name, processor, true); 
        }

        public void QWork(string name, Action processor, bool highPriority) {
            if (!_cont)
                return;
            _tasksQueued++;
            if (_useStack)
                _eventStack.Push(new KeyValuePair<string, Action>(name, processor));
            else
                _eventQ.Enqueue(new KeyValuePair<string,Action>(name, processor));
        }

        public void Stop() {
            if (!_shared)
                ProperShutdown();
        }

        internal void ProperShutdown() {
            _cont = false;
        }

        public void Step() {
            _step = true;
        }

        public void BlockWhileWorking() {
            while (IsWorking) ;
        }

        public IQueueStats Statistics {
            get { return this; }
        }

        #endregion

        #region IEnumerator Members

        public object Current {
            get { return CurrentItem; }
        }
        
        public KeyValuePair<string, Action> CurrentItem {
            get { return _useStack ? _eventStack.Pop() : _eventQ.Dequeue(); }
        }

        public bool MoveNext() {
            if (!_cont)
                return false;

            if (_paused && !_step)
                return _cont;

            if (_paused && IsWorking) {
                _step = false;
                Process();
            }

            int itemsPerTick = (QueuedItems / _itemsPerThread) + 1;
            for (int i = 0; i < itemsPerTick && IsWorking; i++)
                Process();
           
            return _cont;
        }

        private void Process() {
            KeyValuePair<string, Action> current = CurrentItem;
            try {
                DateTime start = DateTime.Now;
                current.Value.Invoke();
                _processTime += DateTime.Now.Subtract(start).TotalMilliseconds;
                _tasksProcessed++;
            } catch (Exception e) {
                _tasksFailed++;
                Console.WriteLine("Problem invoking " + current.Key + ". " + e.Message + "\n" + e.StackTrace);
            }
        }

        public void Reset() {
        }

        #endregion

        #region IEnumerable Members

        public IEnumerator GetEnumerator() {
            return this;
        }

        #endregion

        #region IQueueStats Members

        private DateTime _started;
        private double _processTime = 0;
        private int _tasksProcessed = 0;
        private int _tasksFailed = 0;
        private int _tasksQueued = 0;
        private int _maxThreads = 1;
        private int _totalHelpers = 0;
        private double _totalHelperTime = 0;
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
            get { return 1; }
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
    }
}
