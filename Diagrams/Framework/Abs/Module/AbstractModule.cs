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
using common;
using common.framework.impl.util;
using common.framework.interfaces.entities;
using Diagrams.Common;
using Diagrams.Common.interfaces.keytable;
using common.Queue;
using log4net;
using System.Linq;

namespace Diagrams {
    public abstract class AbstractModule<TNode, TLink> : IModule
        where TNode : INode
        where TLink : ILink {
                

        private IKeyTable<TNode> _nodes;
        private IKeyTable<TLink> _links;

        private IKeyTable<IKeyTable<TNode>> _neighbours;
        private IKeyTable<IKeyTable<TLink>> _connections;

        private readonly IAsynchQueue _queue;
        protected readonly IKeyTableFactory TableFactory;
        protected readonly ILog Logger;

        protected AbstractModule(IKeyTableFactory keyTableFactory, IAsynchQueueFactory queueFactory) {
            TableFactory = keyTableFactory;
            Logger = LogManager.GetLogger(GetType());

            _nodes = keyTableFactory.MakeKeyTable<TNode>();
            _links = keyTableFactory.MakeKeyTable<TLink>();

            _neighbours = keyTableFactory.MakeKeyTable<IKeyTable<TNode>>();
            _connections = keyTableFactory.MakeKeyTable<IKeyTable<TLink>>();

            //_queue = queueFactory.MakeQueue();
            //Queue.Start(GetType().Name + " module queue");
            _queue = queueFactory.SharedQueue;
        }

        private void CheckLinksPair(UUID from, UUID to, string request) {
            if (!_nodes.ContainsKey(from) && !_nodes.ContainsKey(to))
                throw new Exception("Unable to " + request + " link. To and From are not known nodes.");
            else if (!_nodes.ContainsKey(from))
                throw new Exception("Unable to " + request + " link. From is not a known node.");
            else if (!_nodes.ContainsKey(to))
                throw new Exception("Unable to " + request + " link. To is not a known node.");
            else if (from.Equals(to))
                throw new Exception("Unable to " + request + " link. To and from must be different nodes.");
        }

        #region IModule<TNode> Members

        public abstract int Wait { get; set; }
        public abstract bool Paused { get; set; }

        public virtual INode AddNode(string name, Parameters parameters, Vector3 position = default(Vector3), Color colour = default(Color)) {
            if (name == null)
                throw new Exception("Unable to add entity. Name cannot be null");
            lock (this) {
                TNode node = MakeNode(name, parameters, position, colour);
                _nodes.Add(node.ID, node);
                _neighbours.Add(node.ID, TableFactory.MakeKeyTable<TNode>());
                _connections.Add(node.ID, TableFactory.MakeKeyTable<TLink>());
                Logger.Debug("Stored node '" + name + "'.");
                return node;
            }
        }

        public virtual ILink AddLink(UUID from, UUID to, Parameters parameters, float weight = default(float), bool bidirectional = true) {
            lock (this) {
                CheckLinksPair(from, to, "add");
                if (AreLinked(from, to))
                    throw new Exception("Unable to add link. '" + _nodes[from].Name + "' and '" + _nodes[to] + "' are alread linked.");

                TLink link = MakeLink(from, to, parameters, weight, bidirectional);
                _links.Add(link.ID, link);
                _neighbours[from].Add(link.ID, _nodes[to]);
                _connections[from].Add(to, link);
                if (bidirectional) {
                    _neighbours[to].Add(link.ID, _nodes[from]);
                    _connections[to].Add(from, link);
                }

                Logger.Debug("Stored '" + link.Name + "'.");
                return link;
            }
        }

        public void RemoveLink(UUID l, Parameters parameters) {
            if (!IsLink(l))
                return;
            //throw new Exception("Unable to remove link. Given ID is not a known link.");
            Logger.Debug("Removing '" + GetLink(l).Name + "'.");
            RemoveLink(GetLink(l), parameters);
        }

        public void RemoveLink(UUID from, UUID to, Parameters parameters) {
            CheckLinksPair(from, to, "remove");
            if (!AreLinked(from, to))
                throw new Exception("Unable to remove link. '" + _nodes[from].Name + "' and '" + _nodes[to] + "' are not linked.");
            Logger.Debug("Removing '" + _connections[from][to].Name + "'.");
            lock (this) 
                RemoveLink(_connections[from][to], parameters);
        }

        protected virtual void RemoveLink(TLink link, Parameters parameters) {
            lock (this) {
                link.Parameters.Append(parameters);
                link.Destroy();
                _links.Remove(link.ID);
                _neighbours[link.FromID].Remove(link.ID);
                _connections[link.FromID].Remove(link.ToID);
                if (link.IsBidirectional) {
                    _neighbours[link.ToID].Remove(link.ID);
                    _connections[link.ToID].Remove(link.FromID);
                }
                Logger.Info("Removed '" + link.Name + "'.");
            }
        }

        public virtual void RemoveNode(UUID n, Parameters parameters) {
            if (!IsNode(n))
                return;
                //throw new Exception("Unable to remove entity. Given ID is not a known node.");
            lock (this) {
                INode node = GetNode(n);
                node.Parameters.Append(parameters);
                node.Destroy();
                _neighbours.Remove(n);
                _connections.Remove(n);
                _nodes.Remove(n);
                Logger.Debug("Removed node '" + node.Name + "'.");
            }
        }

        public virtual void Stop() {
            Clear();
        }

        public virtual void Clear() {
            _nodes.Clear();
            _links.Clear();
            _neighbours.Clear();
            _connections.Clear();
        }

        public virtual void Clear(params UUID[] nodes) {
            foreach (var n in nodes) {
                if (_connections.ContainsKey(n)) {
                    lock (_connections) {
                        foreach (var l in _connections[n])
                            _links.Remove(l.ID);
                        _connections.Remove(n);
                    }
                }
                _neighbours.Remove(n);
                _nodes.Remove(n);
            }
        }

        #endregion

        /// <summary>
        /// Perform the specified action on every node mapped by the module.
        /// </summary>
        /// <param name="doThis">The delegate function to be performed on each node.</param>
        public void ForAllNodes(Action<TNode> doThis) {
            foreach (TNode n in _nodes)
                doThis(n);
        }

        /// <summary>
        /// Perform the specified action on every link mapped by the module.
        /// </summary>
        /// <param name="doThis">The delegate function to be performed on each link.</param>
        public void ForAllLinks(Action<TLink> doThis) {
            foreach (TLink l in _links)
                doThis(l);
        }

        /// <summary>
        /// Check whether a node and a link are connected.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="link">The link.</param>
        public bool AreConnected(UUID node, UUID link) {
            return IsNode(node) && IsLink(link) && _neighbours[node].ContainsKey(link);
        }

        /// <summary>
        /// Find out whether two nodes are linked
        /// </summary>
        /// <param name="from">The first entity to check for a _link</param>
        /// <param name="to">The second entity to check for a _link</param>
        public virtual bool AreLinked(UUID from, UUID to) {
            return _connections.ContainsKey(from) && _connections[from].ContainsKey(to);
        }

        /// <summary>
        /// Find out whether an ID references a entity
        /// </summary>
        /// <param name="senderID">The senderID to check whether it is a entity.</param>
        public virtual bool IsNode(UUID id) {
            return _nodes.ContainsKey(id);
        }

        /// <summary>
        /// Find out whether an ID references a _link
        /// </summary>
        /// <param name="senderID">The senderID to check whether it is a _link.</param>
        public virtual bool IsLink(UUID id) {
            return _links.ContainsKey(id);
        }

        /// <summary>
        /// Get a list of all links that are linked to a given entity. The list is indexed by the ID of the neighbour at the other end of the _link.
        /// </summary>
        /// <param name="entity">The ID of the entity to get links from.</param>
        public virtual IKeyTable<TLink> GetLinks(UUID node) {
            if (!IsNode(node))
                throw new Exception("Unable to get links. There is no such node.");
            return _connections[node].Copy();
        }

        /// <summary>
        /// Get the two nodes at either end of a _link
        /// </summary>
        /// <param name="link">The _link to get the nodes at either end of.</param>
        public virtual Pair<TNode, TNode> GetEnds(UUID link) {
            if (!IsLink(link))
                throw new Exception("Unable to get ends. ID is not a known link.");
            return new Pair<TNode, TNode>(_nodes[_links[link].FromID], _nodes[_links[link].ToID]);
        }

        /// <summary>
        /// Get a _link given the two nodes at either end
        /// </summary>
        /// <param name="from">The entity at one end of the _link.</param>
        /// <param name="to">The entity at the other end of the _link</param>
        public virtual TLink GetLink(UUID from, UUID to) {
            if (!_connections.ContainsKey(from) || !_connections[from].ContainsKey(to))
                return default(TLink);
            return _connections[from][to];
        }

        /// <summary>
        /// Get a link given its ID.
        /// </summary>
        /// <param name="link">The ID of the link.</param>
        public TLink GetLink(UUID link) {
            return _links[link];
        }

        /// <summary>
        /// Get a node given its ID.
        /// </summary>
        /// <param name="node">The ID of the node.</param>
        public TNode GetNode(UUID node) {
            return _nodes[node];
        }

        /// <summary>
        /// Create a new _link and perform any special tasks associated with making a new _link specific to the implementation of the manager.
        /// </summary>
        protected abstract TLink MakeLink(UUID from, UUID to, Parameters parameters, float weight, bool bidirectional);

        /// <summary>
        /// Create a new entity and perform any special tasks associated with making a new entity specific to the implementation of the manager.
        /// </summary>
        protected abstract TNode MakeNode(string name, Parameters parameters, Vector3 position,
                                          Color colour);


        /// <summary>
        /// Get all the neighbours of a given entity. The list is indexed by the ID of the _link going to the neighbour.
        /// </summary>
        /// <param name="entity">The entity to get the neighbours of.</param>
        public IKeyTable<TNode> GetNeighbours(UUID node) {
            if (!IsNode(node))
                throw new Exception("Unable to get neighbours. There is no such node.");
            return _neighbours[node].Copy(); ;
        }

        /// <summary>
        /// Find out whether a given _link is attached to a specified _node
        /// </summary>
        /// <param name="node">The _node to check.</param>
        /// <param name="link">The _link which may or may not be attached to _node.</param>
        public bool IsNodeLink(UUID node, UUID link) {
            return _neighbours[node].ContainsKey(link);
        }

        /// <summary>
        /// Check what the _node at the other end of a _link is
        /// </summary>
        /// <param name="node">The _node at 'this' end of the _link.</param>
        /// <param name="link">The _link to check.</param>
        protected TNode OtherEnd(UUID node, UUID link) {
            return _neighbours[node][link];
        }

        public IAsynchQueue Queue {
            get {
                return _queue;
            }
        }
    }
}