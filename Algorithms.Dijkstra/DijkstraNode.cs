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
using Diagrams;
using common.framework.interfaces.entities;
using OpenMetaverse;
using common.framework.impl.util;
using common.model.framework.interfaces;
using Diagrams.Common;
using Diagrams.Common.interfaces.keytable;
using common;
using log4net;
using System.Drawing;
using log4net.Config;
using System.IO;
using System.Configuration;
using System.Threading;
using Nini.Config;
using System.Diagnostics;
using JM726.Lib.Static;

namespace diagrams.algorithms.dijkstra {
    public class DijkstraNode : AbstractAlgorithmNode, IAlgorithmNode {
        #region Static Fields
        public static DijkstraNode VisualisedNode = null;

        private static readonly double currentGlow = .8d;
        private static readonly double tentativeGlow = .5d;


        
        #endregion

        #region Private fields

        /// <summary>
        ///   The distances of this node to other nodes
        /// </summary>
        private readonly IKeyTable<float> _distances;
        private readonly IKeyTable<DijkstraNode> _prev;
        private readonly IKeyTable<IMLink> _wipRoute;
        private readonly IAsynchQueue _queue;

        #region Parameters

        private bool _text = false;
        private bool _visualise;
        private volatile bool _cont;

        #endregion

        #region Data

        private IMNodeInternal _target;
        private LinkedList<DijkstraNode> _confirmed;
        private LinkedList<IMLink> _links;
        private LinkedList<DijkstraNode> _tentative;

        #endregion

        private Thread _visualiseThread;

        #endregion

        private int Wait {
            get { return Model.IsPaused ? 0 : Model.GlobalWait * Model.WaitMult; }
        }

        protected IAsynchQueue Queue {
            get { return _queue; }
        }

        #region Constructor

        public DijkstraNode(IMNodeInternal node, IKeyTableFactory tableFactory, IAsynchQueue queue, IConfigSource config)
            : base(node, tableFactory, config) {

            _queue = queue;
            _distances = tableFactory.MakeKeyTable<float>();
            _wipRoute = tableFactory.MakeKeyTable<IMLink>();
            _prev = tableFactory.MakeKeyTable<DijkstraNode>();

            _confirmed = new LinkedList<DijkstraNode>();
            _tentative = new LinkedList<DijkstraNode>();
            _links = new LinkedList<IMLink>();

            SetDistanceFromRoot(ID, 0f, null, null);
        }

        protected override void LinkAdded(IMLink link, Parameters parameters, bool visualise) {
            OnChange("Link to '" + link.OtherEnd(Node).Name + "' added", link.ID, parameters, visualise);
        }

        protected override void LinkRemoved(IMLink link, Parameters parameters, bool visualise) {
            OnChange("Link to '" + link.OtherEnd(Node).Name + "' removed", link.ID, parameters, visualise);
        }

        protected override void WeightChanged(IMLink link, bool visualise) {
            OnChange("Link to '" + link.Name + "' changed weight to " + link.Weight, link.ID, Node.Parameters, visualise);
        }

        private Type _dijkstraType = typeof(DijkstraNode);

        /// <summary>
        /// Override this to change what happens when something changes in the topology.
        /// Changes are:
        /// A link is added.
        /// A link is removed.
        /// The weight of a link changes.
        /// 
        /// The default behaviour is to run the algorithm from every node.
        /// </summary>
        protected virtual void OnChange(string msg, UUID link, Parameters parameters, bool visualise) {
            foreach (DijkstraNode n in this.GetAllNodes<DijkstraNode>()) {
                if (n.GetType().Equals(_dijkstraType) && !Links.ContainsKey(n.ID) || !link.Equals(Links[n.ID].ID) || !Links[n.ID].IsBidirectional)
                    n.RunAlgorithm("Running algorithm from " + n.Name + " because " + msg + ".", null, n.Equals(VisualisedNode));
            }
        }

        #endregion

        #region DijkstraNode

        internal double Selected {
            get { return Node.Selected; }
            set { Node.Selected = value; }
        }

        internal Color Colour {
            get { return Node.Colour; }
            set { Node.Colour = value; }
        }

        #endregion

        #region IAlgorithmNode

        //public override event ForwardingTableChangeDelegate OnRouteChange;

        public override void Stop() {
            if (_visualise && Equals(VisualisedNode)) {
                ResetAlgorithm();
                _visualise = false;
                _text = false;
                VisualisedNode = null;
                RunAlgorithm(Name + " visualisation stopped. Re-running algorithm silently.", null, false);
            }
        }

        public override void ProcessPacket(IMPacket packet) {
            //Do nothing
        }

        public override void VisualiseAlgorithm(Parameters parameters) {
            VisualiseAlgorithm(UUID.Zero, parameters);
        }

        public override void VisualiseAlgorithm(UUID to, Parameters parameters) {
            if (Equals(VisualisedNode)) {
                ResetAlgorithm();
                _visualise = false;
                VisualisedNode = null;
                return;
            }
            else if (VisualisedNode != null)
                VisualisedNode.Stop();

            IMNodeInternal target = KnownNodes.ContainsKey(to) ? KnownNodes[to] : null;

            _text = Dijkstra.AlwaysPrint || (Dijkstra.EverPrint && parameters != null && parameters.Get<bool>("Text"));
            VisualisedNode = this;

            RunAlgorithm("Visualise Dijkstra's Algorithm", target, true);
        }

        #endregion

        public void RunAlgorithm(string description, IMNodeInternal to, bool visualise) {
            _visualise = visualise;
            ResetAlgorithm();
            if (_visualise && IsCurrentAlgorithm) {
                _visualiseThread = new Thread(() => RunAlgorithm(to));
                _visualiseThread.Name = description;
                _visualiseThread.Start();
            } else {
                _queue.QWork(GetType().Name + " " + description, () => RunAlgorithm(to));
            }
        }

        #region External state

        internal float GetDistanceFromRoot(UUID root) {
            if (root == ID)
                return 0f;
            else if (_distances.ContainsKey(root))
                return _distances[root];
            else
                return float.MaxValue;
        }
        internal DijkstraNode GetPrev(UUID key) {
            if (_prev.ContainsKey(key))
                return _prev[key];
            return null;
        }

        internal void SetDistanceFromRoot(UUID root, float value, DijkstraNode prevNode, IMLink link) {
            //if (prevNode != null/* && !Links.ContainsKey(prevNode.ID)*/)
            //    return;

            lock (_distances) {
                if (_distances.ContainsKey(root)) {
                    _distances[root] = value;
                    _prev[root] = prevNode;
                } else {
                    _distances.Add(root, value);
                    _prev.Add(root, prevNode);
                }
            }
            if (prevNode != null)
                SetWIPRoute(root, link);
        }

        internal IMLink GetWIPRoute(UUID target) {
            if (_wipRoute.ContainsKey(target))
                return _wipRoute[target];
            return null;
        }

        /// <summary>
        ///   Used to specify that the route to a target node is along the specified AddLink
        /// </summary>
        /// <param name = "target">The node to map a route to</param>
        /// <param name = "link">The AddLink along which to pass packets intended for target node</param>
        public void SetWIPRoute(UUID target, IMLink link) {
            lock (_wipRoute) {
                if (_wipRoute.ContainsKey(target))
                    _wipRoute[target] = link;
                else
                    _wipRoute.Add(target, link);
            }
        }
        #endregion

        #region The Algorithm

        /// <summary>
        /// Initialise the various variables, flags and collections that are used to run the algorithm.
        /// </summary>
        /// <param name="target">The target being routed to. Null means route to every target.</param>
        /// 
        /// 
        private void Init(IMNodeInternal target) {
            _target = _target != null && target == null ? _target : target;
            _confirmed.Clear();
            _tentative.Clear();
            _links.Clear();
            _tentative.AddFirst(this);
            foreach (DijkstraNode n in this.GetAllNodes<DijkstraNode>())
                n.SetDistanceFromRoot(ID, float.MaxValue, null, null);
            _cont = true;
        }

        private void Pause() {
            Pause(1f);
        }
        private void Pause(float divider) {
            if (Model.IsPaused)
                Util.Wait(cont: _cont, target: this);
            else
                Util.Wait((int) (Wait / divider), _cont, this);
        }

        private object _runLock = new object();

        private void RunAlgorithm(IMNodeInternal to) {
            lock (_runLock) {
                Init(to);

                if (_visualise)
                    Logger.Debug("Dijkstra visualising routing from " + Name);

                if (_cont && _text && _visualise) {
                    Say("Starting Algorithm");
                    Say("1. Initialising Confirmed and Tentative lists");
                }

                DijkstraNode current = getNext(this);
                DijkstraNode prev = current;

                if (_cont && _text && _visualise) {
                    Pause();
                    Say("3. Cost for root (" + Node.Name + ") = 0");
                    Pause();
                }

                MainLoop(current, prev);

                //If the algorithm didn't finish prematurely
                if (_cont)
                    UpdateRoutingTable();
                algorithmFinished();
            }

        }

        /// <summary>
        /// The main loop which is the body of the algorithm.
        /// Loops through every neighbour of the current node checking to see if they:
        /// A. Have been visited before
        /// B. Have been visited before but are now a shorter route
        /// </summary>
        /// <param name="current">The node currently being checked.</param>
        /// <param name="prev">The previous node that was checked.</param>
        /// <returns>The last node to be checked.</returns>
        private void MainLoop(DijkstraNode current, DijkstraNode prev) {
            while (_cont && current != null && !hitTarget(current)) {
                //If necessary display text and wait
                if (_cont && _text && _visualise) {
                    current.Say("4. Iterating through neighbours excluding neighbours in Confirmed");
                    Pause();
                }
                //Get the current distance to the target
                float dist = current.GetDistanceFromRoot(ID);
                foreach (var n in current.Neighbours) {
                    IMLink link = current.Links[n.ID];
                    if (_visualise)
                        _links.AddFirst(link);
                    var neighbour = this.GetAlgNode<DijkstraNode>(n.ID);
                    if (_cont && !_confirmed.Contains(neighbour)) {
                        if (_visualise && _text)
                            VisualiseCheckNeighbourText(neighbour, current, dist, link);
                        else if (_visualise)
                            VisualiseCheckNeighbour(neighbour, current, dist, link);
                        checkNeighbour(neighbour, current, dist, link);
                    }
                    if (!_cont)
                        break;
                }
                //Store the old previous node
                prev = current;
                if (_cont && _text && _visualise) {
                    Say("Finished iterating through current's (" + current.Name +
                              "'s) neighbours, getting next current from tentative list");
                    Pause();
                }
                //Get the next node to be checked (the one which has the shortest path to it)
                current = getNext(current);
            }
            FinishedReset(current, prev);
        }

        private void FinishedReset(DijkstraNode current, DijkstraNode prev) {
            if (hitTarget(current)) {
                foreach (var tentative in _tentative)
                    tentative.Reset();
                _tentative.Clear();

                if (_cont && _visualise && prev != null) {
                    if (_text)
                        Say("A route to the target was found. The algorithm is finished");
                    current.Reset();
                }
            } else {
                if (_cont && _visualise && prev != null) {
                    if (_text)
                        Say("There are no more nodes in the tentative list. The algorithm is finished");
                    prev.Reset();
                }
            }
        }

        /// <summary>
        /// Check whether the target to be routed to has been found.
        /// </summary>
        /// <param name="current">The current node.</param>
        /// <returns>True if the current node is the target node.</returns>
        private bool hitTarget(DijkstraNode current) {
            if (!_cont || _target == null)
                return false;
            if (_cont && _text && _visualise && current.ID == _target.ID)
                Say("Found the shortest route to target " + _target.Name);
            return current.ID == _target.ID;
        }

        /// <summary>
        /// Check a neighbour of the current node to see if it needs to be added to the tentative list
        /// or the shortest route to it needs to be updated.
        /// </summary>
        /// <param name="neighbour">The neighbour to check.</param>
        /// <param name="current">The current node.</param>
        /// <param name="dist">The distance from the root to the current node.</param>
        /// <param name="link">The link between current and neighbour.</param>
        private void checkNeighbour(DijkstraNode neighbour, DijkstraNode current, float dist, IMLink link) {
            float newDistance = dist + link.Weight;

            try {
                //If the new route from root to neighbour is shorter than the old route or there was no old route
                //set the current node as the first step towards the root from neighbour
                if (_cont && !_tentative.Contains(neighbour)) {
                    _tentative.AddFirst(neighbour);
                    neighbour.SetDistanceFromRoot(ID, newDistance, current, link);
                } else if (_cont && newDistance <= neighbour.GetDistanceFromRoot(ID))
                    neighbour.SetDistanceFromRoot(ID, newDistance, current, link);
            } catch (KeyNotFoundException e) {
                //If the AddLink being checked is no Keyer in the list of links the algorithm needs to stop
                _cont = false;
                if (_visualise)
                    VisualisedNode = null;
                Logger.Debug("Trying to work with a neighbour (" + neighbour.Name + ") which is not actually connected to " +
                         current.Name + " (root = " + Name + ")");
            }

            //Incase another thread has modified the distance
            if (_cont && newDistance < neighbour.GetDistanceFromRoot(ID))
                neighbour.SetDistanceFromRoot(ID, newDistance, neighbour, link);
        }

        /// <summary>
        /// Visualises the process of checking a neighbour node.
        /// </summary>
        /// <param name="neighbour">The neighbour to check.</param>
        /// <param name="current">The current node.</param>
        /// <param name="dist">The distance from the root to the current node.</param>
        /// <param name="link">The link between current and neighbour.</param>
        private void VisualiseCheckNeighbour(DijkstraNode neighbour, DijkstraNode current, float dist, IMLink link) {
            if (!_visualise)
                return;

            if (_cont) {
                Pause(4f);
                link.Colour = Color.Blue;
                Pause(6);
            }

            try {
                if (_cont && !_tentative.Contains(neighbour)) {
                    neighbour.Selected = tentativeGlow;
                    Pause(6);
                }
            } catch (KeyNotFoundException e) {
                //If the AddLink being checked is no Keyer in the list of links the algorithm needs to stop
                _cont = false;
                if (_visualise)
                    VisualisedNode = null;
                Logger.Debug("Trying to work with a neighbour (" + neighbour.Name + ") which is not actually connected to " +
                         current.Name + " (root = " + Name + ")");
            }

            if (_cont)
                link.Colour = Color.White;
        }

        /// <summary>
        /// Visualises the process of checking a neighbour node and prints out text explaining what is happening.
        /// </summary>
        /// <param name="neighbour">The neighbour to check.</param>
        /// <param name="current">The current node.</param>
        /// <param name="dist">The distance from the root to the current node.</param>
        /// <param name="link">The link between current and neighbour.</param>
        private void VisualiseCheckNeighbourText(DijkstraNode neighbour, DijkstraNode current, float dist, IMLink link) {
            if (!_visualise && !_text)
                return;

            float newDistance = dist + link.Weight;

            if (_cont) {
                //if (_cont && _text)
                //    current.Say("Examining neighbour " + neighbour.Name);
                Pause(4f);
                link.Colour = Color.Blue;
                if (_cont && _text) {
                    Pause();
                    current.Say("5. Test cost to " + neighbour.Name + " = " + dist + " + " + link.Weight + " = " +
                                  newDistance);
                    Pause();
                }
                Pause(4f);
            }

            try {
                if (_cont && !_tentative.Contains(neighbour)) {
                    if (_cont && _text)
                        current.Say(neighbour.Name + " is not in tentative list");

                    _tentative.AddFirst(neighbour);
                    neighbour.SetDistanceFromRoot(ID, newDistance, current, link);

                    if (_cont) {
                        Pause(4f);
                        neighbour.Selected = tentativeGlow;
                        if (_cont && _text) {
                            current.Say("6a. Adding " + neighbour.Name + " to the tentative list");
                            Pause();
                            current.Say("6b. " + neighbour.Name + ".cost = test cost (" + newDistance + ")");
                        }
                    }
                }
                    //If the new route from root to neighbour is shorter than the old route or there was no old route
                    //set the current node as the first step towards the root from neighbour
                else if (_cont && newDistance <= neighbour.GetDistanceFromRoot(ID)) {
                    neighbour.SetDistanceFromRoot(ID, newDistance, current, link);

                    if (_cont && _text) {
                        current.Say(neighbour.Name + ".cost (" + neighbour.GetDistanceFromRoot(ID) + ") > test cost (" +
                                      newDistance + ")");
                        Pause();
                        current.Say("7. " + neighbour.Name + ".cost = test cost (" + newDistance + ")");
                        Pause();
                    }
                }
            } catch (KeyNotFoundException e) {
                //If the AddLink being checked is no Keyer in the list of links the algorithm needs to stop
                _cont = false;
                if (_visualise)
                    VisualisedNode = null;
                Logger.Debug("Trying to work with a neighbour (" + neighbour.Name + ") which is not actually connected to " +
                         current.Name + " (root = " + Name + ")");
            }

            if (_cont && newDistance < neighbour.GetDistanceFromRoot(ID))
                neighbour.SetDistanceFromRoot(ID, newDistance, neighbour, link);

            if (_cont) {
                Pause(4f);
                link.Colour = Color.White;
                Pause(6f);
            }
        }

        /// <summary>
        /// Get the next node to be checked. Next node is the node in the tentative list which is closest to the root.
        /// </summary>
        /// <param name="prev">The node that was previously checked.</param>
        /// <returns>The new current node.</returns>
        private DijkstraNode getNext(DijkstraNode prev) {
            if (_cont && _tentative.Count > 0) {
                DijkstraNode next = _tentative.OrderBy(node => node.GetDistanceFromRoot(ID)).First();
                if (!_cont)
                    return null;
                _tentative.Remove(next);
                _confirmed.AddLast(next);

                if (_cont && _visualise)
                    VisualiseChooseNext(next, prev);
                return next;
            }
            return null;
        }

        /// <summary>
        /// Visualise the process of choosing the next node. Will output text about what is going on if necessary.
        /// </summary>
        /// <param name="next">The next node to be current.</param>
        /// <param name="prev">The node that was previously current.</param>
        private void VisualiseChooseNext(DijkstraNode next, DijkstraNode prev) {
            if (_cont && !next.Equals(this)) {
                if (_cont && _text) {
                    Say("8. There are nodes in the tentative list. Finding the node in tentative with the smallest cost");
                    Pause();
                }
                Pause(3f);
                if (_cont && _text) {
                    Say("9. new current = " + next.Name + " with cost " + next.GetDistanceFromRoot(ID));
                    Pause();
                    Say("10. Adding link between ct (" + prev.Name + ") and new current (" + next.Name + ") to the shortest path");
                    Pause();
                }
            }

            prev.Reset();

            IMLink confirmedLink = next.GetWIPRoute(ID);
            if (_cont && confirmedLink != null) {
                Pause(6f);
                confirmedLink.Colour = Color.Red;
            }

            if (_cont && _text) {
                Say((next.Equals(this) ? "2a" : "11a") + ". Adding " +
                          (next.Equals(this) ? "root" : "new current") + " (" + next.Name + ") to Confirmed");
                Pause();
                Say((next.Equals(this) ? "2b" : "11b") + ". Setting current to " +
                          (next.Equals(this) ? "root" : "new current") + " (" + next.Name + ")");
            }

            next.Reset();
            next.Selected = currentGlow;
            next.Colour = Color.White;

            if (_cont && _text && !next.Equals(this)) {
                Pause();
                Say("11c. Going to step 4");
                Pause();
            }
        }

        protected virtual string Algorithm {
            get { return Dijkstra.DIJKSTRA_NAME; }
        }

        /// <summary>
        /// Once the algorithm has finished use the information it has stored to update the routing table.
        /// </summary>
        private void UpdateRoutingTable() {
            IKeyTable<IMLink> oldTable = ForwardingTable;
            foreach (DijkstraNode target in _confirmed) {
                if (target.ID != ID) {
                    //Work backwards along the route, starting at the target, until the first hop is found
                    DijkstraNode prev = target;
                    while (prev != null && !Equals(prev.GetPrev(ID)))
                        prev = prev.GetPrev(ID);

                    if (prev != null && Links.ContainsKey(prev.ID)) {
                        IMLink l = Links[prev.ID];
                        SetRoute(Algorithm, target.ID, l, target.GetDistanceFromRoot(ID));
                        oldTable.Remove(target.ID);
                    }
                }
            }
            //Trigger events for any nodes that can no longer be routed to
            foreach (IMNodeInternal n in KnownNodes) {
                if (oldTable.ContainsKey(n.ID)) 
                    RemoveRoute(Algorithm, n.ID);
            }
        }

        #endregion

        public bool IsRunning {
            get { return _cont; }
        }

        #region Node Public

        /// <summary>
        ///   Called when the algorithm finishes
        /// </summary>
        public void algorithmFinished() {
            _cont = false;
            Util.Wake(Node);
        }

        #endregion

        #region Thread - Public

        #region Public Util

        /// <summary>
        ///   Reset the algorithm. Change any visuals the algorithm had changed and stop the thread.
        /// </summary>
        public void Reset() {      
            Node.Reset(Dijkstra.DIJKSTRA_NAME);
        }

        /// <summary>
        ///   Reset the algorithm. Change any visuals the algorithm had changed and stop the thread.
        ///   If the algorithm has finished running 
        /// </summary>
        public void ResetAlgorithm() {
            _cont = false;
            Util.Join(_visualiseThread);
            if (_visualise) {
                lock (_runLock) {
                    if (_tentative != null)
                        foreach (DijkstraNode n in _tentative)
                            if (!n.Equals(this))
                                n.Reset();
                    if (_confirmed != null)
                        foreach (DijkstraNode n in _confirmed)
                            if (!n.Equals(this))
                                n.Reset();
                    if (_links != null)
                        foreach (IMLink link in _links)
                            link.Reset();
                    Reset();
                }
            }
        }

        #endregion

        #endregion

        public override string ToString() {
            return Name + " - Dijkstra";
        }
    }
}
