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
using System.Linq;
using OpenMetaverse;
using common;
using common.framework.abs.wrapper;
using common.framework.interfaces.entities;
using common.model.framework.interfaces;
using common.framework.impl.util;
using Diagrams.Common;
using common.Queue;
using Diagrams.Common.interfaces.keytable;
using System.Drawing;
using System.Collections.Generic;
using System.Threading;
using Diagrams.Framework.Util;
using JM726.Lib.Static;

namespace Diagrams {
    public partial class MNode : NodeWrapper, IMNode {
        /// <summary>
        ///   ToggleSelected used to send packets along a specified link
        /// </summary>
        protected ForwardDelegate _passDownMethod;
        /// <summary>
        ///   The links that leave this node. Indexed by the node they connect to
        /// </summary>
        private IKeyTable<IMLink> _links;
        /// <summary>
        ///   The links that leave this node. Indexed by the node they connect to
        /// </summary>
        private IKeyTable<IMNodeInternal> _neighbours;

        private Dictionary<string, IAlgorithmNode> _algorithms;

        /// <summary>
        /// The algorithm that is currently being run in the node.
        /// </summary>
        private IAlgorithmNode _currentAlgorithm;
        /// <summary>
        /// The name of the algorithm currently being run in the node.
        /// </summary>
        private string _currentAlgorithmName;
        /// <summary>
        /// Factory used to create new KeyTables as necessary
        /// </summary>
        private IKeyTableFactory _tableFactory;

        private IKeyTable<IMLink> ForwardingTable {
            get { return _currentAlgorithm.ForwardingTable; }
        }

        public Route[] ForwardingTableList {
            get {
                    //Remove the nodes that aren't in the forwarding table
                    return Model.KnownNodes.Where(node => ForwardingTable.ContainsKey(node.ID)).
                        //Project the nodes to strings
                        Select<IMNodeInternal, Route>(target => {
                            IMLink link = ForwardingTable[target.ID];
                            IMNodeInternal hop = link.OtherEnd(this);
                            return new Route(this, target, hop, link, _currentAlgorithm.GetDistance(target.ID));
                        }).ToArray();
            }
        }

        public MNode(INode node, ForwardDelegate forwardMethod, String defaultAlgorithm, IAlgorithm[]algorithms, IKeyTableFactory tableFactory)
            : base(node) {

            _passDownMethod = forwardMethod;
            _tableFactory = tableFactory;
            _links = tableFactory.MakeKeyTable<IMLink>();
            _neighbours = tableFactory.MakeKeyTable<IMNodeInternal>();
            _algorithms = new Dictionary<string, IAlgorithmNode>();

            foreach (IAlgorithm alg in algorithms) {
                IAlgorithmNode algNode = alg.MakeNode(this, SendPacket);
                _algorithms.Add(alg.Name, algNode);
                algNode.OnRouteChange += RouteChangeListener;
                algNode.IsCurrentAlgorithm = alg.Name.Equals(defaultAlgorithm);
                if (algNode.IsCurrentAlgorithm)
                    _currentAlgorithm = algNode;
            }
            if (_currentAlgorithm == null)
                throw new Exception("Test Node unable to set algorithm. '" + defaultAlgorithm + "' is not a valid algorithm.");
            _currentAlgorithmName = defaultAlgorithm;

            _passDownMethod = forwardMethod;

            OnPacketReceived += (at, p) => {
                IMPacket packet = p as IMPacket;
                if (packet != null && packet.Type == PTypes.data)
                    ReceiveData(packet);
                else if (packet != null && _algorithms.ContainsKey(packet.Algorithm))
                    _algorithms[packet.Algorithm].ProcessPacket(packet);
            };
            _weightDelegate = (id, newWeight) => {
                if (OnWeightChange != null) {
                    OnWeightChange(id, newWeight);
                    Logger.Debug(Name + " triggered OnWeightChange for '" + Links[id].Name + "'.");
                }
            };
            _highlightDelegate = () => ResetHighlightAll();
            OnHighlightReset += _highlightDelegate;
        }

        private readonly Action _highlightDelegate;

        #region IMNodeExternal Members

        /// <inhertidoc />
        public string CurrentAlgorithm {
            get {
                return _currentAlgorithmName;
            }
            set {
                if (!_algorithms.ContainsKey(value)) {
                    Say("Unable to set algorithm. '" + value + "' is not a valid algorithm.");
                    throw new Exception(Name + " unable to set algorithm. '" + value + "' is not a valid algorithm.");
                }
                _currentAlgorithm.IsCurrentAlgorithm = false;
                _currentAlgorithmName = value;

                _currentAlgorithm = _algorithms[value];
                _currentAlgorithm.IsCurrentAlgorithm = true;
            }
        }
        
        private WeightChangedDelegate _weightDelegate;

        /// <inhertidoc />
        public void AddLink(IMLink link, Parameters parameters) {
            IMNode neighbour = link.OtherEnd(this);

            //Add the link referenced by both its own UUID and the ID of the neighbour it connects to
            lock (_links) {
                if (!_links.ContainsKey(neighbour.ID))
                    _links.Add(neighbour.ID, link);
                if (!_links.ContainsKey(link.ID))
                    _links.Add(link.ID, link);
            }
            lock (_neighbours)
                if (!_neighbours.ContainsKey(link.ID))
                    _neighbours.Add(link.ID, neighbour);

            link.OnWeightChanged += _weightDelegate;

            if (OnLinkAdded != null) {
                OnLinkAdded(ID, link, parameters);
                Logger.Debug(Name + " triggered OnLinkAdded for '" + link.Name + "'.");
            }
        }

        /// <inhertidoc />
        public void RemoveLink(UUID l, Parameters parameters) {
            if (!Neighbours.ContainsKey(l))
                throw new Exception("The link to be remove is not a known link.");

            IMNodeInternal neighbour = _neighbours[l];
            IMLink link = _links[neighbour.ID];

            link.OnWeightChanged -= _weightDelegate;

            lock (_links) {
                _links.Remove(neighbour.ID);
                _links.Remove(l);
            } lock (_neighbours)
                _neighbours.Remove(link.ID);

            if (OnLinkRemoved != null && !parameters.Get<bool>("Clearing")) {
                OnLinkRemoved(ID, link, parameters);
                Logger.Debug(Name + " triggered OnLinkRemoved for '" + link.Name + "'.");
            }
        }

        /// <Inheritdoc />
        public void Send(IMNodeExternal destination, Parameters parameters) {
            if (!ForwardingTable.ContainsKey(destination.ID)) {
                Say("Unable to forward packet to " + destination.Name);
                return;
            }

            IMLink link = ForwardingTable[destination.ID];
            SendPacket(ID, link.ID, new MPacket(this, destination, this, parameters));
        }

        /// <inhertidoc />
        private void dropped(IMPacket packet) {
            Say("Unable to forward packet to " + packet.Destination.Name);
        }

        /// <summary>
        ///   What to do when a data packet is received.
        /// 
        ///   Will either forward the packet along the relevant link in the routing table or 
        ///   print out that the packet was received in world.
        /// </summary>
        /// <param name = "packet">The packet</param>
        protected virtual void ReceiveData(IMPacket packet) {
            if (ID.Equals(packet.D)) {
                Say("Received " + packet.Name);
                Util.Wake(this);
            } else if (packet.D != null && ForwardingTable.ContainsKey(packet.D)) {
                packet.Hop = this;
                SendPacket(this.ID, ForwardingTable[packet.D].ID, packet);
            } else
                dropped(packet);
        }

        private void SendPacket(UUID id, UUID link, IPacket packet) {
            try {
                _passDownMethod(id, link, packet);
            } catch (Exception e) {
                Logger.Debug("Unable to send packet. " + e.Message);
            }
        }

        public void VisualiseRoutingAlgorithm(Parameters parameters) {
            _currentAlgorithm.VisualiseAlgorithm(parameters);
        }

        public void VisualiseRoutingAlgorithm(UUID to, Parameters parameters) {
            _currentAlgorithm.VisualiseAlgorithm(to, parameters);
        }

        #endregion

        #region IMNodeInternal Members

        /// <inhertidoc />
        public event WeightChangedDelegate OnWeightChange;

        /// <inhertidoc />
        public event LinkAddedDelegate OnLinkAdded;

        /// <inhertidoc />
        public event LinkRemovedDelegate OnLinkRemoved;

        /// <inhertidoc />
        public event Action<UUID, Route[]> OnForwardingTableChange;

        /// <inhertidoc />
        public IKeyTable<IMLink> Links {
            get { return _links.Copy(); }
        }

        /// <inhertidoc />
        public IKeyTable<IMNodeInternal> Neighbours {
            get { return _neighbours.Copy(); }
        }
        
        #endregion
        
        public override bool Destroy() {
            Reset(_currentAlgorithmName);
            OnHighlightReset -= _highlightDelegate;
            return true;
        }
    }
}