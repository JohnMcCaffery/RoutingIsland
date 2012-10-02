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
using OpenMetaverse;
using common.framework.impl.util;
using common.model.framework.interfaces;
using common.framework.interfaces.entities;
using Diagrams.Common.interfaces.keytable;
using diagrams.algorithms.dijkstra;
using common;
using algorithms.dijkstra.impl;
using log4net.Config;
using Nini.Config;
using System.Diagnostics;
using JM726.Lib.Static;

namespace diagrams.algorithms.linkstate {
    public class LinkStateNode : DijkstraNode, IAlgorithmNode {
        private ForwardDelegate _forwardMethod;
        private static int _event = 0;
        private IAsynchQueue _eventQ;

        private HashSet<int> _processedEvents;

        private bool _changed = false;

        protected override string Algorithm {
            get {
                return LinkState.LINK_STATE_NAME;
            }
        }

        public LinkStateNode(IMNodeInternal node, IKeyTableFactory tableFactory, IAsynchQueue dijkstraQ, IAsynchQueue packetQ, ForwardDelegate forwardMethod, IConfigSource config)
            : base(node, tableFactory, dijkstraQ, config) {

            
                _eventQ = packetQ;
            _forwardMethod = forwardMethod;
            _processedEvents = new HashSet<int>();

            OnRouteChange += (alg, target, oldRoute, newRoute, oldDistance, distance) => {
                _changed = true;
                if (!Model.IsPaused)
                    return;
                if (oldRoute != null && newRoute != null)
                    Say("Link State changed route to " + target.Name + " from " + oldRoute.OtherEnd(Node).Name + " to " + newRoute.OtherEnd(Node).Name + ".");
                else if (oldRoute == null)
                    Say("Link State added route to " + target.Name + " via " + newRoute.OtherEnd(Node).Name + " with distance " + distance + ".");
                else if (newRoute == null)
                    Say("Link State removed route to " + target.Name + ". " + oldRoute.OtherEnd(Node).Name + " is no longer a valid first hop and no other route was found.");
                else
                    Say("Link State changed weight of route to " + target.Name + " from " + oldDistance + " to " + distance + ".");
            };
        }

        public override void ProcessPacket(IMPacket p) {
            LinkStatePacket packet = p as LinkStatePacket;
            if (packet == null) {
                Logger.Info(ToString() + " received unknown packet: " + p.Name);
                return;
            }
            if (!_processedEvents.Contains(packet.Event)) {
                _changed = true;
                OnChange("Receiving packet from '" + p.Source.Name + "'", Links.ContainsKey(p.S) ? Links[p.S].ID : UUID.Zero, packet.Event, p.Visualise);
            }
        }

        protected override void OnChange(string reason, UUID l, Parameters parameters, bool visualise) {
            _changed = false;
            OnChange(reason, l, _event++, visualise);
        }

        private void OnChange(string reason, UUID l, int evt, bool visualise) {
            _processedEvents.Add(evt);
            if (Model.IsPaused && IsCurrentAlgorithm) {
                _eventQ.QWork(Name + " Link State " + reason, () => {
                    Say(reason + ". Link State running Dijkstra's algorithm.");
                    RunAlgorithm(Name + " Link State " + reason + ".", null, Equals(DijkstraNode.VisualisedNode));
                    QueueUpdateBroadcast(l, reason, evt, visualise);
                });
            } else {
                RunAlgorithm(Name + "Link State " + reason + ".", null, Equals(DijkstraNode.VisualisedNode));
                QueueUpdateBroadcast(l, reason, evt, visualise);
            }
        }

        /// <summary>
        /// Broadcast update packets. If paused and visualising this algorithm queues the packet. Otherwise just broadcasts the packets.
        /// </summary>
        /// <param name="receivedAlong"></param>
        /// <param name="msg"></param>
        /// <param name="evt"></param>
        /// <param name="parameters"></param>
        private void QueueUpdateBroadcast(UUID receivedAlong, string msg, int evt, bool visualise) {
            Util.Wait(cont: IsRunning, target: Node);
            //if (!_changed) {
            //    if (Model.IsPaused && IsCurrentAlgorithm && visualise)
            //        Say("No change made so no update to be broadcast.");
            //    return;
            //}
            _changed = false;
            if (Model.IsPaused && IsCurrentAlgorithm && visualise)
                _eventQ.QWork(Name + " Link IsOn broadcasting update.", () => BroadcastUpdate(receivedAlong, msg, evt, visualise));
            else
                BroadcastUpdate(receivedAlong, msg, evt, visualise);
        }

        /// <summary>
        /// Broadcasts update packets. Iterates through neighbour and as long as the source of the update was not the neighbour sends a flood packet to that neighbour.
        /// </summary>
        /// <param name="receivedAlong"></param>
        /// <param name="msg"></param>
        /// <param name="evt"></param>
        /// <param name="visualisePackets"></param>
        private void BroadcastUpdate(UUID receivedAlong, string msg, int evt, bool visualisePackets) {
            if (Model.IsPaused && IsCurrentAlgorithm && visualisePackets)
                Say("Broadcasting update after " + msg + ".");
            foreach (IMNode neighbour in Neighbours) {
                IMLink link = Links[neighbour.ID];
                if (link != null && (!neighbour.ID.Equals(receivedAlong) || !link.IsBidirectional))
                    _forwardMethod(ID, link.ID, new LinkStatePacket(Node, Node, neighbour, evt, visualisePackets));
            }
        }

        public override string ToString() {
            return Name + " LinkState";
        }

    }
}
