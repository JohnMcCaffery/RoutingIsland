#region Namespace imports

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using Ninject;
using Ninject.Parameters;
using OpenMetaverse;
using common;
using common.Queue;
using common.config;
using common.framework.impl.util;
using common.framework.interfaces.entities;
using common.framework.interfaces.layers;
using common.model.framework.interfaces;
using dependencyinjection.factory;
using dependencyinjection.impl;
using dependencyinjection.interfaces;

#endregion

namespace Diagrams {
    public enum Events {
        AddNode,
        RemoveNode,
        AddLink,
        RemoveLink,
        CalculateRouting,
        RoutingTable,
        Changed,
        Speed,
        Stop,
        ProcessPacket,
        PassDown
    }

    /// <summary>
    ///   Implementation of the ISimulation interface within the SimulationLayer class
    /// </summary>
    [Serializable]
    public partial class Model {
        /*
        #region Private Fields

        private static readonly Type nodeType = typeof (INode);

        private static Type mLinkType = typeof (IMLink);

        private static readonly Type mNodeType = typeof (IMNode);

        private static readonly Type linkType = typeof (ILink);

        private static readonly Type packetType = typeof (IMPacket);

        private readonly List<IAlgorithm> algorithms;

        private readonly IAsynchQueue<Pair<Events, Parameters>> eventQ;
        private readonly KeyTable<IMLink> links;
        private readonly KeyTable<Dictionary<string, IMNode>> nodes;
        private readonly KeyTable<IMPacket> packets;
        private readonly IView view;
        private bool complete;
        private IMNode currentNode;
        private bool recalculate;
        private bool shuttingDown;
        private bool simultaneous = true;

        private bool visualise = true;

        #endregion

        #region Constructors

        /// <summary>
        ///   Initialises the model layer.
        /// 
        ///   Specifies the starting wait times which determines how fast the layer will run. 
        ///   Also specifies the algorithm folder where the implementations of any algorithms this model is to run can be found.
        /// </summary>
        public Model(IView view, IAsynchQueueFactory queueFactory) {
            this.view = view;

            string algFolder = ConfigurationManager.AppSettings[Config.Algorithms.AlgorithmsFolder];
            currentAlgorithm = ConfigurationManager.AppSettings[Config.Common.Algorithm];

            eventQ = queueFactory.GetQueue<Pair<Events, Parameters>>();
            eventQ.start(processEvent, "Model Layer event Q", Levels.MODEL);

            nodes = new KeyTable<Dictionary<string, IMNode>>();
            links = new KeyTable<IMLink>();
            packets = new KeyTable<IMPacket>();
            algorithms = new List<IAlgorithm>();

            if (algFolder == null)
                throw new ModelException("No algorithm folder set");

            int wait = Config.LoadInt(Config.Common.Wait, Defaults.Common.Wait);
            int waitMult = Config.LoadInt(Config.Model.WaitMultiplier, Defaults.Model.WaitMultiplier);

            IKernel k = NinjectFactory.getKernel<DynamicLoaderModule>();

            algFolder = Path.Combine(Environment.CurrentDirectory, algFolder);
            k.Get<IDynamicLoaderModule>().BindAllInFolder(typeof (IAlgorithm), algFolder, true,
                                                          new ConstructorArgument("queueFactory", queueFactory));
            foreach (IAlgorithm alg in k.GetAll<IAlgorithm>()) {
                //DB.Print("Creating " + alg.Name, Levels.BOOTSTRAP);
                algorithms.Add(alg);
                alg.Wait = wait;
                alg.WaitMult = waitMult;
            }

            bool valid = false;
            foreach (IAlgorithm alg in algorithms)
                if (alg.Name.Equals(currentAlgorithm)) valid = true;

            if (!valid) {
                view.shutdown();
                eventQ.stop();
                throw new ModelException("Unable to start model layer, default algorithm " + currentAlgorithm +
                                         " is not a valid algorithm");
            }
        }

        #endregion

        #region Private Events

        private event TopologyChangedDelegate onTopologyChanged;

        private event ResetDelegate onReset;

        #endregion

        #region Properties

        /// <inheritdoc />
        public bool Visualise {
            set { visualise = value; }
        }

        /// <inheritdoc />
        public bool Simultaneous {
            set { simultaneous = value; }
        }

        /// <inheritdoc />
        public bool Complete {
            set { complete = value; }
        }

        /// <inheritdoc />
        public List<string> Algorithms {
            get {
                var l = new List<string>();
                foreach (IAlgorithm alg in algorithms)
                    l.Add(alg.Name);
                return l;
            }
        }

        #endregion

        #region Methods

        /// <inheritdoc />
        public void shutdown() {
            //DB.Print("Being shut down", Levels.SHUTDOWN, Levels.MODEL);
            shuttingDown = true;

            foreach (IAlgorithm alg in algorithms)
                alg.stop();

            foreach (Dictionary<string, IMNode> nodeInstances in nodes)
                foreach (IAlgorithm algorithm in algorithms)
                    if (nodeInstances.ContainsKey(algorithm.Name))
                        nodeInstances[algorithm.Name].removed();

            eventQ.stop();

            if (view != null) view.shutdown();
            //DB.Print("Shut down", Levels.SHUTDOWN, Levels.MODEL);
        }

        /// <inheritdoc />
        public void setAlgorithm(string algorithm) {
            currentAlgorithm = algorithm;
            //DB.Print("Setting default algorithm to " + algorithm);

            foreach (Dictionary<string, IMNode> nodeInstances in nodes)
                foreach (IAlgorithm alg in algorithms)
                    if (nodeInstances.ContainsKey(alg.Name))
                        nodeInstances[alg.Name].CurrentAlgorithm = currentAlgorithm;
        }

        /// <inheritdoc />
        public void step() {
            if (currentNode != null)
                currentNode.step();
        }

        #endregion

        #region Private Methods

        #region Event Management

        private void processEvent(Pair<Events, Parameters> evt) {
            switch (evt.A) {
                case Events.AddNode:
                    newNode(evt.B);
                    break;
                case Events.RemoveNode:
                    removeSpecificNode(evt.B);
                    break;
                case Events.AddLink:
                    newLink(evt.B);
                    break;
                case Events.RemoveLink:
                    removeLink(getLink(evt.B));
                    break;
                case Events.Changed:
                    recalculateRouting();
                    break;

                case Events.RoutingTable:
                    showRoutingTable(evt.B);
                    break;
                case Events.CalculateRouting:
                    showRouting(evt.B);
                    break;
                case Events.Speed:
                    SpecifiedSetSpeed(evt.B);
                    break;
                case Events.Stop:
                    shutdown();
                    break;
                case Events.ProcessPacket:
                    processPacketEvent(evt.B);
                    break;
                case Events.PassDown:
                    passDownEvent(evt.B);
                    break;
            }
        }

        #endregion

        #region Events

        private void newNode(Parameters parameters) {
            if (!parameters.checkType("Node", nodeType))
                return;
            var node = parameters["Node"] as INode;

            node.PacketReceivedEvent += processPacket;

            if (algorithms.Count == 0)
                throw (new Exception("No algorithms defined"));

            if (!nodes.ContainsKey(node.ID))
                nodes.Add(node.ID, new Dictionary<string, IMNode>());
            else
                nodes[node.ID] = new Dictionary<string, IMNode>();

            foreach (IAlgorithm alg in algorithms) {
                IMNode n = alg.makeNode(node, passDown);
                lock (nodes)
                    nodes[node.ID].Add(alg.Name, n);

                onTopologyChanged += n.TopologyChanged;
                onReset += n.ResetEvent;

                n.CurrentAlgorithm = currentAlgorithm;
            }
            //DB.Print("Added " + node.Name, Levels.MODEL);
        }

        private void removeSpecificNode(Parameters parameters) {
            if (!parameters.checkType("ID", Parameters.keyType))
                return;
            var n = (UUID) parameters["ID"];

            string name = "ToID remove node";
            if (nodes.ContainsKey(n)) {
                foreach (IAlgorithm alg in algorithms) {
                    if (nodes[n].ContainsKey(alg.Name)) {
                        IMNode node = nodes[n][alg.Name];
                        name = node.Name;
                        node.removed();
                        Util.Wake(node);

                        onTopologyChanged -= node.TopologyChanged;
                        onReset -= node.ResetEvent;
                    }
                }
                lock (nodes)
                    nodes.Remove(n);
            }
            //DB.Print("Removed node " + name, Levels.MODEL);
        }

        private void newLink(Parameters parameters) {
            if (!parameters.checkType("Link", linkType))
                return;
            var link = parameters["Link"] as ILink;

            link.LinkChangedEvent += notifyChange;

            IMLink l = null;
            foreach (IAlgorithm alg in algorithms) {
                IMNode node1 = nodes[link.FromID][alg.Name];
                IMNode node2 = nodes[link.ToID][alg.Name];

                l = new MLink(link, node1, node2);

                node1.AddLink(l);
                node2.AddLink(l);
            }
            onReset += l.ResetEvent;
            links.Add(l.ID, l);
            //DB.Print("Added " + link.Name, Levels.MODEL);
        }

        private void removeLink(IMLink link) {
            if (link == null)
                return;
            if (nodes.ContainsKey(link.FromID) && nodes.ContainsKey(link.ToID)) {
                foreach (IAlgorithm alg in algorithms) {
                    if (nodes[link.FromID].ContainsKey(alg.Name)) {
                        IMNode node1 = nodes[link.FromID][alg.Name];

                        node1.RemoveLink(link);
                    }
                    if (nodes[link.ToID].ContainsKey(alg.Name)) {
                        IMNode node2 = nodes[link.ToID][alg.Name];
                        node2.RemoveLink(link);
                    }
                }
            }
            links.Remove(link.ID);
            onReset -= link.ResetEvent;
            //DB.Print("Removed " + link.Name, Levels.MODEL);
        }

        private void showRoutingTable(Parameters parameters) {
            if (!parameters.checkType("ID", Parameters.keyType))
                return;

            var n = (UUID) parameters["ID"];

            if (nodes.ContainsKey(n) && nodes[n].ContainsKey(currentAlgorithm)) {
                recalculateRouting();

                if (currentNode != null)
                    currentNode.stop();

                currentNode = nodes[n][currentAlgorithm];
                currentNode.showRoutingTable();
            }
        }

        private void showRouting(Parameters parameters) {
            IMNode root = null;
            IMNode to = null;

            if (parameters.checkType("Root", Parameters.keyType)) {
                var rootKey = (UUID) parameters["Root"];

                if (nodes.ContainsKey(rootKey) && nodes[rootKey].ContainsKey(currentAlgorithm))
                    root = nodes[rootKey][currentAlgorithm];
            }
            if (parameters.checkType("ToID", Parameters.keyType)) {
                var toKey = (UUID) parameters["ToID"];

                if (nodes.ContainsKey(toKey) && nodes[toKey].ContainsKey(currentAlgorithm))
                    to = nodes[toKey][currentAlgorithm];
            }
            if (root != null) {
                recalculateRouting();

                if (currentNode != null)
                    currentNode.stop();
                currentNode = root;

                parameters["Visualise"] = true;
                if (to == null)
                    root.calculateRouting(parameters);
                else
                    root.calculateRouting(to, parameters);
            }
        }

        private void SpecifiedSetSpeed(Parameters parameters) {
            if (!parameters.checkType("Speed", Parameters.intType))
                return;
            var speed = (int) parameters["Speed"];

            foreach (IAlgorithm alg in algorithms)
                alg.Wait = speed;

            lock (nodes)
                foreach (Dictionary<string, IMNode> nodeInstances in nodes)
                    foreach (IAlgorithm algorithm in algorithms)
                        if (nodeInstances.ContainsKey(algorithm.Name))
                            nodeInstances[algorithm.Name].Wait = speed;
        }

        #endregion

        #region Util

        private IMLink getLink(Parameters parameters) {
            if (!parameters.checkType("ID", Parameters.keyType))
                return null;
            var key = (UUID) parameters["ID"];
            return links.ContainsKey(key) ? links[key] : null;
        }

        private bool recalculateRouting() {
            try {
                if (recalculate && !eventQ.IsWorking) {
                    recalculate = false;
                    if (onTopologyChanged != null) {
                        onTopologyChanged();
                        return true;
                    }
                }
            }
            catch (Exception e) {
                //DB.Exception(e, "Unabe to recalculate routing", Levels.MODEL);
            }
            return false;
        }

        #endregion

        #endregion
         * */
    }
}