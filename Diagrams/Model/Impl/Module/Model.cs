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
using System.Drawing;
using OpenMetaverse;
using common.framework.impl.util;
using common.model.framework.interfaces;
using Diagrams.Common.interfaces.keytable;
using common.Queue;
using common.framework.interfaces.entities;
using common.config;
using Ninject;
using dependencyinjection.factory;
using dependencyinjection.impl;
using System.IO;
using dependencyinjection.interfaces;
using Ninject.Parameters;
using System.Collections.Generic;
using log4net;
using System.Reflection;
using System.Linq;
using System.Configuration;
using Nini.Config;
using Diagrams.Common;

namespace Diagrams {
    public partial class Model : AbstractModule<IMNodeExternal, IMLink>, IModel {
        private static int _wait;
        private static int _waitMult = 1;
        private static bool _paused = false;
        private static ILog Log = LogManager.GetLogger(typeof(Model));

        private IView _view;
        private string _currentAlgorithm;
        private IAlgorithm[] _algorithms;

        private static IKeyTable<IMNodeInternal> _knownNodes;
        public static IKeyTable<IMNodeInternal> KnownNodes { get { return _knownNodes.Copy(); } }

        public static event Action<int, int, bool> OnWaitChanged;

        /// <summary>
        /// The speed an visualisation the algorithm does should run at.
        /// </summary>
        public static int GlobalWait {
            get { return _wait; }
        }

        public static int WaitMult {
            get { return _waitMult; }
        }

        public static bool IsPaused {
            get { return _paused; }
        }

        #region Constructor

        public Model(IView view, IKeyTableFactory tableFactory, IAsynchQueueFactory queueFactory, IConfigSource config, params IAlgorithm[] algorithms)
            : base(tableFactory, queueFactory) {

            if (algorithms.Length == 0) {
                view.Stop();
                throw new Exception("Unable to start model layer, default algorithm " + _currentAlgorithm +
                                            " is not a valid algorithm");
            }

            _knownNodes = tableFactory.MakeKeyTable<IMNodeInternal>();
            _view = view;
            _algorithms = algorithms;

            IConfig commonConfig = config.Configs["Common"];
            IConfig modelConfig = config.Configs["Model"];
            if (modelConfig == null)
                modelConfig = config.Configs[0];
            if (commonConfig == null)
                commonConfig = config.Configs[0];

            _wait = commonConfig.GetInt("Wait", 50);
            _waitMult = modelConfig.GetInt("WaitMult", 5);
            _currentAlgorithm = modelConfig.GetString("Algorithm", algorithms.First().Name);

            if (_algorithms.Count(alg => alg.Name.Equals(_currentAlgorithm)) == 0) {
                _currentAlgorithm = algorithms.First().Name;
                Logger.Debug("Specified Current Algorithm ('" + modelConfig.GetString("Algorithm") + "') invalid. Using '" + _currentAlgorithm + "'.");
            }
            if (!modelConfig.Contains("Algorithm"))
                Logger.Debug("Current Algorithm not specified. Using '" + _currentAlgorithm + "'.");

            Logger.Info("Model started with " + algorithms.Length + " algorithms.");
            foreach (IAlgorithm alg in algorithms)
                Logger.Info(alg.Name + (alg.Name.Equals(_currentAlgorithm) ? " (default)" :""));
        }

        #endregion

        #region IModel Members

        public override int Wait {
            get { return _wait; }
            set {
                if (value > 0) {
                    _view.Wait = value;
                    int oldWait = _wait;
                    _wait = value;
                    if (OnWaitChanged != null)
                        OnWaitChanged(oldWait, value, _paused);
                }
            }
        }

        public override bool Paused {
            get { return _paused; }
            set {
                _view.Paused = value;
                _paused = value;
                if (OnWaitChanged != null)
                    OnWaitChanged(_wait, _wait, _paused);
            }
        }

        public string[] Algorithms {
            get { return _algorithms.SelectMany<IAlgorithm, string>(alg => new String[] { alg.Name }).ToArray(); }
        }

        public string Algorithm {
            get { return _currentAlgorithm; }
        }

        public void Send(UUID from, UUID to, Parameters parameters) {
            if (!IsNode(from) && !IsNode(to))
                throw new Exception("Unable to send packet. Neither From nor To are known nodes");
            if (!IsNode(from))
                throw new Exception("Unable to send packet. From is not a known node");
            else if (!IsNode(to))
                throw new Exception("Unable to send packet. To is not a known node");

            Queue.QWork("Send Packet from " + GetNode(from) + " to " + GetNode(to) + ".", 
                () => GetNode(from).Send(GetNode(to), parameters));
        }

        public void VisualiseRouting(UUID at, Parameters parameters) {
            if (!IsNode(at))
                throw new Exception("Unable to visualise routing. Root is not a valid node");
            GetNode(at).VisualiseRoutingAlgorithm(parameters);
        }

        public void VisualiseRouting(UUID from, UUID to, Parameters parameters) {
            if (!IsNode(from) && !IsNode(to))
                throw new Exception("Unable to visalise routing. Neither from nor to are valid nodes");
            if (!IsNode(from))
                throw new Exception("Unable to visualise routing. From is not a valid node");
            if (!IsNode(to))
                throw new Exception("Unable to visualise routing. To is not a valid node");
            GetNode(from).VisualiseRoutingAlgorithm(to, parameters);
        }

        public void DisplayForwardingTable(UUID id, Parameters parameters, params UUID[] boards) {
            if (!IsNode(id))
                throw new Exception("Unable to display forwarding table. ID is not a known node");
            _view.DisplayForwardingTable(id, GetNode(id).ForwardingTableList, boards);
        }

        public void SetAlgorithm(string algorithm) {
            try {
                ForAllNodes(node => node.CurrentAlgorithm = algorithm);
                _currentAlgorithm = algorithm;
            } catch (Exception e) {
                ForAllNodes(node => node.CurrentAlgorithm = _currentAlgorithm);
                throw new Exception("Model unable to set algorithm to '" + algorithm + "'.");
            }
        }

        public bool Step() {
            foreach (IAlgorithm alg in _algorithms)
                if (alg.Name.Equals(_currentAlgorithm))
                    return alg.Step();
            return false;
        }

        public override void Stop() {
            Logger.Debug("Model stopping.");
            base.Stop();
            foreach (IAlgorithm alg in _algorithms)
                alg.Stop();
            _view.Stop();
            Logger.Info("Model stopped.");
        }

        public override void Clear() {
            base.Clear();
            _view.Clear();
        }

        public override void Clear(params UUID[] nodes) {
            base.Clear(nodes);
            _view.Clear(nodes);
        }

        #endregion

        protected override void RemoveLink(IMLink link, Parameters parameters) {
            UUID l = link.ID;
            link.From.RemoveLink(l, parameters);
            if (link.IsBidirectional)
                link.To.RemoveLink(l, parameters);
            base.RemoveLink(link, parameters);
            _view.RemoveLink(l, parameters);
        }

        public override void RemoveNode(UUID node, Parameters parameters) {
            base.RemoveNode(node, parameters);
            _view.RemoveNode(node, parameters);
            if (_knownNodes.ContainsKey(node))
                _knownNodes.Remove(node);
        }

        protected override IMLink MakeLink(UUID from, UUID to, Parameters parameters, float weight, bool bidirectional) {
            Logger.Debug("Creating link between '" + GetNode(from).Name + "' and '" + GetNode(to).Name + "'.");
            IMNodeExternal f = GetNode(from);
            IMNodeExternal t = GetNode(to);
            ILink l = _view.AddLink(from, to, parameters, weight, bidirectional);

            IMLink link = new MLink(l, f as IMNode, t as IMNode, weight);
            f.AddLink(link, parameters);
            if (bidirectional)
                t.AddLink(link, parameters);
            Logger.Info("Created  '" + link.Name + "'.");
            return link;

        }

        protected override IMNodeExternal MakeNode(string name, Parameters parameters, Vector3 position, Color colour) {
            Logger.Debug("Creating node '" + name + "'.");
            MNode node = new MNode(_view.AddNode(name, parameters, position, colour), _view.Send, _currentAlgorithm, _algorithms, TableFactory);
            _knownNodes.Add(node.ID, node);
            node.OnForwardingTableChange += (id, routes) => _view.UpdateForwardingTable(id, routes);
            Logger.Info("Created node '" + name + "'.");
            return node;
        }
    }
}