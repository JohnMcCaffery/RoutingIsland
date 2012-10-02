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
using System.IO;
using System.Configuration;
using log4net;
using Nini.Config;
using JM726.Lib.Static;

namespace diagrams.algorithms.dijkstra {
    public class Dijkstra : IAlgorithm {
        internal const string DIJKSTRA_NAME = "Dijkstra";
        private static ILog Logger = LogManager.GetLogger(typeof(Dijkstra));

        private IKeyTableFactory _tableFactory;
        private IAsynchQueue _queue;
        private IConfigSource _config;

        internal static bool AlwaysPrint;
        internal static bool EverPrint;

        public Dijkstra(IKeyTableFactory tableFactory, IAsynchQueueFactory queueFactory, IConfigSource config) {
            _tableFactory = tableFactory;
            //_queue = queueFactory.MakeQueue();
            //_queue.Start("Dijkstra Queue");
            _queue = queueFactory.SharedQueue;
            _config = config;
            Model.OnWaitChanged += (oldWait, newWait, paused) => {
                if (DijkstraNode.VisualisedNode != null && oldWait == newWait && !paused)
                    Util.Wake(DijkstraNode.VisualisedNode);
            };

            IConfig dijkstraConfig = config.Configs["Dijkstra"];
            AlwaysPrint = dijkstraConfig != null && dijkstraConfig.GetBoolean("AlwaysPrint", false);
            EverPrint = dijkstraConfig != null && dijkstraConfig.GetBoolean("EverPrint", false);
        }

        public string Name {
            get { return DIJKSTRA_NAME; }
        }

        public void Stop() {
            Logger.Debug(DIJKSTRA_NAME + " stopping.");
            if (DijkstraNode.VisualisedNode != null)
                DijkstraNode.VisualisedNode.Stop();
            Logger.Debug(DIJKSTRA_NAME + " stopped.");
        }

        public bool Step() {
            if (DijkstraNode.VisualisedNode != null && DijkstraNode.VisualisedNode.IsRunning/* && Model.Paused*/) {
                Util.Wake(DijkstraNode.VisualisedNode);
                return true;
            }
            return false;
        }

        public IAlgorithmNode MakeNode(IMNodeInternal node, ForwardDelegate forwardMethod) {
            return new DijkstraNode(node, _tableFactory, _queue, _config);
        }
    }
}
