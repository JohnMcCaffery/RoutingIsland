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
using Diagrams.Common;
using common.framework.interfaces.entities;
using OpenMetaverse;
using common.framework.impl.util;
using common.model.framework.interfaces;
using common;
using algorithms.distanceVector.impl.util;
using algorithms.distanceVector.impl.entities;
using Diagrams.Common.interfaces.keytable;
using common.config;
using log4net.Config;
using Nini.Config;
using System.Threading;

namespace diagrams.algorithms.dv {
    public class DVNode : AbstractAlgorithmNode, IAlgorithmNode {
        private IAsynchQueue _queue;
        private ForwardDelegate _forwardMethod;
        private IMPacket _unknownPacket;

        private Parameters highlightParameters;
        private DistanceVector distanceVector;
        private IKeyTable<DistanceVector> neighbourVectors;
        private bool _poison;

        public static bool _alwaysPrint = false;
        public static bool _everPrint = false;
        public static bool highlightPrintText = false;

        //public override event ForwardingTableChangeDelegate OnRouteChange;

        private readonly int TTL;

        #region IAlgorithmNode

        internal DVNode(IMNodeInternal node, ForwardDelegate forwardMethod, IAsynchQueue queue, IKeyTableFactory tableFactory, int TTL, IConfigSource config)
            : base(node, tableFactory, config) {
            _queue = queue;
            _forwardMethod = forwardMethod;
            this.TTL = TTL;

            distanceVector = new DistanceVector();

            neighbourVectors = tableFactory.MakeKeyTable<DistanceVector>();
            highlightParameters = new Parameters(new object[] { "HColour", node.Colour });

            IConfig dvConfig = config.Configs["DV"];
            if (dvConfig == null)
                dvConfig = config.Configs["DistanceVector"];
            if (dvConfig == null)
                dvConfig = config.Configs["Algorithm"];
            if (dvConfig == null)
                dvConfig = config.Configs[0];

            _poison = dvConfig.GetBoolean("PoisonReverse", true);
            _everPrint = dvConfig.GetBoolean("EverPrint", false);
            _alwaysPrint = dvConfig.GetBoolean("AlwaysPrint", false);
            highlightPrintText = dvConfig.GetBoolean("HighlightPrint", false);
        }

        protected override void LinkAdded(IMLink link, Parameters parameters, bool visualise) {
            findShortestPaths(TTL, "'" + link.Name + "' added", visualise);
            //If there was a packet which was received from the other end of the link that was added before
            //the link had been added process it.
            if (_unknownPacket != null && _unknownPacket.S.Equals(link.OtherEnd(ID))) {
                ProcessPacket(_unknownPacket);
                _unknownPacket = null;
            }
        }

        protected override void LinkRemoved(IMLink link, Parameters parameters, bool visualise) {
            lock (neighbourVectors)
                neighbourVectors.Remove(link.OtherEnd(ID));
            findShortestPaths(TTL, "'" + link.Name + "' removed", visualise);
        }

        protected override void WeightChanged(IMLink link, bool visualise) {
            findShortestPaths(TTL, "'" + link.Name + "' weight changed to " + link.Weight, visualise);
        }

        public override void ProcessPacket(IMPacket p) {
            if (!Links.ContainsKey(p.S)) {
                _unknownPacket = p;
                return;
            }

            DVPacket packet = p as DVPacket;

            lock (neighbourVectors)
                //Add the new distance vector to the collection of known distance vectors
                if (!neighbourVectors.ContainsKey(p.S))
                    neighbourVectors.Add(p.S, packet.DistanceVector);
                else
                    neighbourVectors[p.S] = packet.DistanceVector;
            findShortestPaths(packet.TTL, "Distance Vector receiving '" + p.Name + "'", p.Visualise);
        }

        public override void VisualiseAlgorithm(Parameters parameters) {
            VisualiseAlgorithm(null, parameters);
        }

        public override void VisualiseAlgorithm(UUID to, Parameters parameters) {
            VisualiseAlgorithm(KnownNodes[to], parameters);
        }

        private void VisualiseAlgorithm(IMNodeInternal target, Parameters parameters) {
            Logger.Debug("Distance vector visualising routing for " + Node.Name + ".");

             if (target == null)
                Node.TriggerHighlightAll(DV.DV_NAME);
            else 
                Node.TriggerHighlight(DV.DV_NAME, target);
        }

        public override void Stop() {
            Node.TriggerHighlightReset();
        }

        #endregion

        #region Algorithm

        /// <summary>
        /// Find the shortest path to every other node. TTL is specified so that if the request was started by receiving a packet it won't be propogated forever.
        /// </summary>
        /// <param name="TTL"></param>
        private void findShortestPaths(int TTL, string msg, bool visualise) {
            _queue.QWork(Name + " recalculating shortest paths.", () => {
                Logger.Debug(Name + " " + msg + ". Processing.");
                List<string> changes = new List<string>();
                foreach (IMNodeInternal n in KnownNodes)
                    if (!n.ID.Equals(ID))
                        findShortestPath(n, changes);

                bool step = false;
                if (Model.IsPaused && IsCurrentAlgorithm) {
                    if (changes.Count > 0) {
                        Say("DV processing routes after " + msg + ". " + changes.Count + " changes necessary.");
                        foreach (var change in changes)
                            Say(change);
                    } else
                        step = true;
                }

                if (ShouldPrint(visualise))
                    PrintDistanceVector(msg);
                Logger.Info(Name + " processed " + msg + " " + changes.Count + " changes made to the forwarding table.");
                if (changes.Count > 0 && TTL > 0)
                    QueueBroadcastUpdate(TTL, msg, visualise);

                if (step)
                    new Thread(() => _queue.Step()).Start();
            });
        }

        /// <summary>
        /// Finds the shortest route to a target.
        /// 
        /// To start with can be in one of two states:
        /// A: No route known
        /// B: There is a route known
        /// 
        /// Can finish in one of four states
        /// 1. There was no route originally and one has been found
        /// 2. There is a new, shorter route along a different link
        /// 3. The target can no longer be reached but used to be reachable
        /// 4. The original route is still shortest but is now a different length
        /// 5. The original route is no longer available but a new route was found
        /// 6. The original route is still shortest and has not changed
        /// 7. The target was not reachable before and is still not reachable
        /// 
        /// 1 = Update distance vector, broadcast update, trigger routing table changed event
        /// 2 = Update distance vector, broadcast update, trigger routing table changed event
        /// 3 = Update distance vector, broadcast update, trigger routing table changed event
        /// 4 = Update distance vector, broadcast update, trigger routing table changed event
        /// 5 = Update distance vector, broadcast update, trigger routing table changed event
        /// 6 = No events triggered
        /// 7 = No events triggered
        /// </summary>
        /// <param name="targetNode"></param>
        /// <returns></returns>
        private bool findShortestPath(IMNodeInternal targetNode, List<string> changes) {
            UUID target = targetNode.ID;

            //oldDist == null indicates initial state A, otherwise initial state B.
            Distance oldDist;
            lock (distanceVector)
                oldDist = distanceVector.ContainsKey(target) ? distanceVector[target].copy() : null;
            
            IKeyTable<DistanceVector> tmpNeighbours = neighbourVectors.Copy();

            IMLink link = null;
            float dist = float.MaxValue;
            if (oldDist != null && oldDist.Link.OtherEnd(ID).Equals(target) && Links.ContainsKey(target)) {
                //If there was an old route and target is a neighbour and the link was the link between them use the weight of that link as the starting weight
                link = Links[target];
                dist = link.Weight;
            } else if (oldDist != null && tmpNeighbours.ContainsKey(oldDist.Hop.ID) && tmpNeighbours[oldDist.Hop.ID].ContainsKey(target) && tmpNeighbours[oldDist.Hop.ID][target].Dist > 0f) {
                //If there was an old route and it involved routing via a neighbour and that neighbour can still route then take the starting weight as the distance to the neighbour + the distance from the neighbour to the target
                link = Links[oldDist.Hop.ID];
                dist = tmpNeighbours[oldDist.Hop.ID][target].Dist + link.Weight;
            }

            //Check every neighbour to see if the weight of the link to that neighbour + the distance the neighbour can achieve to the target is shorter     .
            foreach (var neighbour in Neighbours) {
                var neighbourLink = Links[neighbour.ID];
                var neighbourVector = tmpNeighbours.ContainsKey(neighbour.ID) && tmpNeighbours[neighbour.ID].ContainsKey(target) ? tmpNeighbours[neighbour.ID][target] : null;
                var neighbourDist = neighbourVector != null ? neighbourLink.Weight + neighbourVector.Dist : float.MaxValue;
                //Neighbour vector distance of 0 or less indicates poison reverse or a link to itself
                //Also checks to see if the link at the other end is the neighbour and whether the weight of the link is less than the current distance
                if ((neighbourVector != null && neighbourVector.Dist > 0 && neighbourDist < dist) ||
                    neighbour.Equals(targetNode) && neighbourLink.Weight < dist) {
                    link = neighbourLink;
                    dist = neighbour.Equals(targetNode) ? link.Weight : neighbourDist;
                }
            }

            if (link == null && oldDist == null)
                //Case 5 + 6
                return false;
            if (link != null && oldDist == null) {
                //1. There was no route originally and one has been found
                UpdateRoutes(targetNode, link.OtherEnd(Node), link, dist);
                changes.Add("Distance Vector added route to " + targetNode.Name + " via " + link.OtherEnd(Node).Name + " with distance " + Math.Round(dist, 2) + ".");
            } else if (link == null && oldDist != null) {
                //3. The target can no longer be reached but used to be reachable
                lock (distanceVector) {
                    distanceVector.Remove(target);
                    RemoveRoute(DV.DV_NAME, target);
                    changes.Add("Distance Vector removed route to " + targetNode.Name + ". " + oldDist.Hop.Name + " is no longer a valid first hop and no other route was found.");
                }
            } else if (oldDist != null && !link.Equals(oldDist.Link)) {
                //2. There is a new, shorter route along a different link
                UpdateRoutes(targetNode, link.OtherEnd(Node), link, dist);
                changes.Add("Distance Vector changed route to " + targetNode.Name + " from " + oldDist.Hop.Name + " to " + link.OtherEnd(Node).Name + " with distance " + Math.Round(dist, 2) + ".");
            } else if (oldDist != null && link.Equals(oldDist.Link) && dist != oldDist.Dist) {
                //4. The original route is still shortest but is now a different length
                UpdateRoutes(targetNode, link.OtherEnd(Node), link, dist);
                changes.Add("Distance Vector changed weight of route to " + targetNode.Name + " from " + Math.Round(oldDist.Dist, 2) + " to " + Math.Round(dist, 2) + ".");
            } else
                return false;

            return true;
        }

        private void UpdateRoutes(IMNodeInternal target, IMNodeInternal hop, IMLink link, float dist) {
            if (target.ID.Equals(ID))
                return;
            SetRoute(DV.DV_NAME, target.ID, link, dist);
            lock (distanceVector)
                if (!distanceVector.ContainsKey(target.ID))
                    distanceVector.Add(target.ID, new Distance(target, hop, link, dist));
                else
                    distanceVector[target.ID].update(hop, link, dist);
        }

        /// <summary>
        ///   Will need to add in poisoned reverse
        /// </summary>
        private void QueueBroadcastUpdate(int TTL, string msg, bool visualise) {
            //if (visualisePackets && IsCurrentAlgorithm && Model.IsPaused)
            //    _queue.QWork("DV " + Name + " broadcasting update.", () => BroadcastUpdate(TTL, msg, visualisePackets, parameters));
            //else
                BroadcastUpdate(TTL, msg, visualise);
        }

        private void BroadcastUpdate(int TTL, string msg, bool visualise) {
            if (ShouldPrint(visualise))
                Logger.Info(Name + " DV broadcasting update after '" + msg + "'.");
            if (Model.IsPaused && IsCurrentAlgorithm && visualise)
                Say("Broadcasting update after " + msg + ".");
            foreach (IMNode n in Neighbours) {
                IMLink l = Links[n.ID];
                //Poisoned reverse, don't pass back routes which route via the destination node
                var v = new DistanceVector();
                lock (distanceVector) {
                    foreach (Distance d in distanceVector)
                        if (!_poison || !d.Link.Equals(l)) //No Poison reverse
                            v.Add(d.Target.ID, new Distance(d.Target, d.Hop, d.Link, d.Dist));
                        else //Poison reverse
                            v.Add(d.Target.ID, new Distance(d.Target, d.Hop, d.Link, -1f));
                }

                v.Add(ID, new Distance(Node, Node, l, 0f));
                _forwardMethod(ID, l.ID, new DVPacket(Node, n, v, visualise, TTL));
            }
        }

        private bool ShouldPrint(bool visualise) {
            return _alwaysPrint || (_everPrint && visualise);
        }

        #endregion

        #region Routing Table Printout

        private static int NAME_WIDTH = 10;

        private void PrintDistanceVector(string msg) {
            string spacer = "|";
            String infinity = "-1";
            int colWidth = NAME_WIDTH + spacer.Length + 1;
            
            //Column header
            msg = "\n" + Name + " (DV): " + msg;
            msg  += "\n";
            for (int i = 0; i < NAME_WIDTH; i++)
                msg += "#";
            msg += spacer + " ";
            msg += getCol(Name, colWidth);
            lock (distanceVector)
                foreach (Distance d in distanceVector)
                    msg += getCol(d.Target.Name, colWidth);

            //Divider
            msg += "\n";
            for (int i = 0; i < colWidth * (distanceVector.Count + 2); i++)
                msg += "_";

            //Row for this node
            msg += "\n" + getCol(Name, NAME_WIDTH) + spacer + " ";
            msg += getCol("0", colWidth);
            lock (distanceVector)
                foreach (Distance d in distanceVector)
                    msg += getCol(d.Dist + "", colWidth);
            
            //Row for neighbour vectors
            foreach (IMNodeInternal neighbour in Neighbours) {
                IMLink l = Links[neighbour.ID];
                UUID y = neighbour.ID;
                if (!neighbourVectors.ContainsKey(y))
                    continue;
                msg += "\n" + getCol(neighbour.Name, NAME_WIDTH) + spacer + " ";
                msg += getNeighbourCol(ID, y, colWidth, infinity);

                lock (distanceVector)
                    foreach (Distance d in distanceVector) {
                        var x = d.Target;
                        if (neighbourVectors[y].ContainsKey(x.ID))
                            msg += getNeighbourCol(x.ID, y, colWidth, infinity);
                        else
                            msg += getCol("", NAME_WIDTH) + spacer + " ";
                    }
            }

            //Logger.Debug(msg + "\n");
            Console.WriteLine(msg);
        }

        private string getNeighbourCol(UUID x, UUID y, int width, string infinity) {
            if (x.Equals(y))
                return getCol("0", width);
            if (neighbourVectors[y].ContainsKey(x)) {
                float v = neighbourVectors[y][x].Dist;
                string format = "{0:##.";
                for (int i = 0; i < width - (v < 10f ? 3 : 4); i++)
                    format += "#";
                format += "}";
                return getCol(String.Format(format, v), width);
            }
            return getCol(infinity, width);
        }

        private string getName(INode n, int width) {
            return getCol(n.Name, width);
        }

        private string getCol(string text, int nameWidth) {
            if (text.Length > nameWidth - 1)
                return text.Substring(0, nameWidth - 1) + " ";
            return text.PadRight(nameWidth);
        }

        #endregion

        public override string ToString() {
            return Node.Name + " - DV";
        }
    }
}
