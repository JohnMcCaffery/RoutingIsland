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
using common;
using common.Queue;
using Diagrams.Common.interfaces.keytable;
using common.config;
using log4net.Config;
using System.IO;
using System.Configuration;
using Nini.Config;
using log4net;

namespace diagrams.algorithms.dv {
    public class DV : IAlgorithm {
        internal const string DV_NAME = "DV";

        private static ILog Logger = LogManager.GetLogger(typeof(DV));

        private readonly int TTL;
        private IAsynchQueue _queue;
        private IKeyTableFactory _tableFactory;
        private IConfigSource _config;

        public DV(IKeyTableFactory tableFactory, IAsynchQueueFactory queueFactory, IConfigSource configSource) {
            _queue = queueFactory.MakeQueue();
            _tableFactory = tableFactory;
            _config = configSource;
            IConfig config = configSource.Configs["Algorithms"];
            if (config == null)
                config = configSource.Configs["DV"];
            if (config == null)
                config = configSource.Configs[0];

            _queue.Start("Distance Vector work Queue");
            _queue.UseStack = true;
            TTL = config.GetInt("TTL", 200);
            Model.OnWaitChanged += (oldWait, wait, paused) => _queue.Paused = paused;
        }

        public string Name {
            get { return DV_NAME; }
        }

        public bool Step() {
            if (Model.IsPaused && _queue.IsWorking) {
                _queue.Step();
                return true;
            }
            return false;
        }

        public void Stop() {
            Logger.Debug(DV_NAME + " stopping.");
            _queue.Stop();
            Logger.Debug(DV_NAME + " stopped.");
        }

        public IAlgorithmNode MakeNode(IMNodeInternal node, ForwardDelegate forwardMethod) {
            return new DVNode(node, forwardMethod, _queue, _tableFactory, TTL, _config);
        }
    }
}
