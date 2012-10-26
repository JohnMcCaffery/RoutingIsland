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
using System.Linq;
using System.Collections.Generic;
using Diagrams;
namespace common.Queue {
    // TODO IMPLEMENTED
    [Serializable]
    public class AsynchQueueFactory : IAsynchQueueFactory, IQueueStats {

        private List<IAsynchQueue> _queues = new List<IAsynchQueue>();
        private AsynchQueue _sharedQ;

        #region IAsynchQueueFactory Members

        public IQueueStats Statistics { get { return this; } }

        public List<IAsynchQueue> Queues {
            get { return new List<IAsynchQueue>(_queues); }
        }

        /// <summary>
        /// Get a singleton queue.
        /// </summary>
        public IAsynchQueue SharedQueue {
            get {
                if (_sharedQ == null) {
                    _sharedQ = new AsynchQueue();
                    //_sharedQ.Start("Shared Queue", 5, true);
                    _sharedQ.Start("Shared Queue", -1, true);
                    _queues.Add(_sharedQ);
                }
                return _sharedQ;
            }
        }

        public IAsynchQueue MakeQueue() {
            IAsynchQueue q = new AsynchQueue();
            _queues.Add(q);
            return q;
        }

        public void ShutdownAll() {
            foreach (AsynchQueue q in _queues)
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
}