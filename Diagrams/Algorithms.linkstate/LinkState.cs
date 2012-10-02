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
using common.model.framework.interfaces;
using Diagrams;
using Diagrams.Common.keytable;
using Diagrams.Common.interfaces.keytable;
using common.Queue;
using common;
using log4net.Config;
using System.Configuration;
using System.IO;
using log4net;
using Nini.Config;
using diagrams.algorithms.dijkstra;
using JM726.Lib.Static;

namespace diagrams.algorithms.linkstate {
    public class LinkState : IAlgorithm {
        internal const string LINK_STATE_NAME = "LinkState";
        internal static readonly object PausedTarget = new object();
        private static ILog Logger = LogManager.GetLogger(typeof(LinkState));

        private IKeyTableFactory _tableFactory;
        private IAsynchQueue _dijkstraQ;
        private IAsynchQueue _eventQ;
        private IConfigSource _config;

        public LinkState(IKeyTableFactory tableFactory, IAsynchQueueFactory queueFactory, IConfigSource config) {
            _tableFactory = tableFactory;
            _config = config;
            //_dijkstraQ = queueFactory.MakeQueue();
            _dijkstraQ = queueFactory.SharedQueue;
            _eventQ = queueFactory.MakeQueue();
            //_dijkstraQ.Start("Link State dijkstra queue");
            _eventQ.Start("Link State packet queue");
            _eventQ.UseStack = true;
            Model.OnWaitChanged += (oldWait, wait, paused) => _eventQ.Paused = paused;
        }
        public string Name {
            get { return LINK_STATE_NAME; }
        }

        public bool Step() {
            if (DijkstraNode.VisualisedNode != null && DijkstraNode.VisualisedNode.IsRunning) {
                Util.Wake(DijkstraNode.VisualisedNode);
                return true;
            } else if (Model.IsPaused && _eventQ.IsWorking) {
                _eventQ.Step();
                return true;
            }
            return false;
        }

        public void Stop() {
            Logger.Debug(LINK_STATE_NAME + " stopping.");
            if (DijkstraNode.VisualisedNode != null)
                DijkstraNode.VisualisedNode.Stop();
            Logger.Debug(LINK_STATE_NAME + " visualised node stopped.");
            _eventQ.Stop();
            Logger.Debug(LINK_STATE_NAME + " stopped.");
        }

        public IAlgorithmNode MakeNode(IMNodeInternal node, ForwardDelegate forwardMethod) {
            return new LinkStateNode(node, _tableFactory, _dijkstraQ, _eventQ, forwardMethod, _config);
        }
    }
}
