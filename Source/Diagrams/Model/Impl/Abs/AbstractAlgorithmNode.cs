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
using OpenMetaverse;
using common;
using common.framework.impl.util;
using common.model.framework.interfaces;
using common.framework.interfaces.entities;
using Diagrams.Common;
using Diagrams.Common.interfaces.keytable;
using log4net;
using common.config;
using log4net.Config;
using System.Configuration;
using System.IO;
using Nini.Config;

namespace Diagrams {
    public static class AlgCollectionExtension {
        private static Dictionary<Type, IKeyTable<IAlgorithmNode>> _algNodes = new Dictionary<Type,IKeyTable<IAlgorithmNode>>();

        internal static IKeyTableFactory _tableFactory;

        public static IEnumerable<TAlg> GetAllNodes<TAlg>(this AbstractAlgorithmNode n) where TAlg : class, IAlgorithmNode {
            if (!_algNodes.ContainsKey(n.GetType()))
                _algNodes.Add(n.GetType(), _tableFactory.MakeKeyTable<IAlgorithmNode>());
            return _algNodes[n.GetType()].Select<IAlgorithmNode, TAlg>(alg => alg as TAlg);
        }

        public static void RegisterAlgNode(this AbstractAlgorithmNode n) {
            if (!_algNodes.ContainsKey(n.GetType()))
                _algNodes.Add(n.GetType(), _tableFactory.MakeKeyTable<IAlgorithmNode>());
            _algNodes[n.GetType()].Add(n.ID, n);
        }

        public static TAlg GetAlgNode<TAlg>(this AbstractAlgorithmNode n, UUID id) where TAlg : class, IAlgorithmNode {
            return _algNodes[n.GetType()][id] as TAlg;
        }
    }


    /// <summary>
    /// </summary>
    public abstract class AbstractAlgorithmNode : IAlgorithmNode {

        protected static IKeyTable<IMNodeInternal> KnownNodes { get { return Model.KnownNodes; } }

        protected readonly ILog Logger;

        private readonly IKeyTable<IMLink> _table;

        private readonly IKeyTable<float> _distances;
        /// <summary>
        /// The node that this algorithm is running in.
        /// </summary>
        protected IMNodeInternal _node;

        private bool _isCurrentAlgorithm = false;

        public abstract void Stop();

        protected void Say(string msg, int channel) {
            if (IsCurrentAlgorithm)
                Node.Say(channel, msg);
        }
        protected void Say(string msg) {
            if (IsCurrentAlgorithm)
                Node.Say(msg);
        }

        /// <inhertidoc />
        public event ForwardingTableChangeDelegate OnRouteChange;

        /// <inhertidoc />
        public IKeyTable<IMLink> ForwardingTable {
            get { return _table.Copy(); }
        }

        /// <inhertidoc />
        public virtual bool IsCurrentAlgorithm {
            get {
                return _isCurrentAlgorithm;
            }
            set {
                if (!value)
                    Stop();
                _isCurrentAlgorithm = value;
            }
        }

        public UUID ID {
            get { return _node.ID; }
        }

        public string Name {
            get { return _node.Name + " DV"; }
        }

        /// <summary>
        /// Table of all links out of the entity.
        /// </summary>
        protected IKeyTable<IMLink> Links {
            get { return _node.Links; }
        }

        /// <summary>
        /// Table of all neighbours of the entity.
        /// </summary>
        protected IKeyTable<IMNodeInternal> Neighbours {
            get { return _node.Neighbours; }
        }

        protected IMNodeInternal Node {
            get { return _node; }
        }

        protected AbstractAlgorithmNode(IMNodeInternal node, IKeyTableFactory tableFactory, IConfigSource config) {
            AlgCollectionExtension._tableFactory = tableFactory;
            Logger = LogManager.GetLogger(GetType());
            _node = node;
            _table = tableFactory.MakeKeyTable<IMLink>();
            _distances = tableFactory.MakeKeyTable<float>();
            this.RegisterAlgNode();

            Node.OnWeightChange += (link, weight) => WeightChanged(Links[link], IsCurrentAlgorithm && Links[link].Parameters.Get<bool>("Visualise"));
            Node.OnLinkAdded += (n, link, parameters) => LinkAdded(link, parameters, IsCurrentAlgorithm && parameters.Get<bool>("Visualise"));
            Node.OnLinkRemoved += (n, link, parameters) => LinkRemoved(link, parameters, IsCurrentAlgorithm && parameters.Get<bool>("Visualise"));
        }

        protected abstract void WeightChanged(IMLink link, bool visualise);

        protected abstract void LinkAdded(IMLink link, Parameters parameters, bool visualise);

        protected abstract void LinkRemoved(IMLink link, Parameters parameters, bool visualise);

        protected void SetRoute(string alg, UUID target, IMLink link, float dist) {
            if (_table.ContainsKey(target)) {
                IMLink oldLink = _table[target];
                float oldDist = _distances[target];
                _table[target] = link;
                _distances[target] = dist;
                if (OnRouteChange != null && IsCurrentAlgorithm && (!link.Equals(oldLink) || dist != oldDist))
                    OnRouteChange(alg, KnownNodes[target], oldLink, link, oldDist, dist);
            } else {
                _table.Add(target, link);
                _distances.Add(target, dist);
                if (OnRouteChange != null && IsCurrentAlgorithm)
                    OnRouteChange(alg, KnownNodes[target], null, link, -1f, dist);
            }
        }

        protected void RemoveRoute(string alg, UUID target) {
            if (_table.ContainsKey(target)) {
                IMLink oldLink = _table[target];
                float oldDist = _distances[target];
                _table.Remove(target);
                _distances.Remove(target);
                if (OnRouteChange != null && IsCurrentAlgorithm)
                    OnRouteChange(alg, KnownNodes[target], oldLink, null, oldDist, -1f);
            }
        }

        protected void ClearTable() {
            _table.Clear();
        }

        public void DisplayForwardingTable(Parameters parameters) {
        }

        /// <summary>
        /// Find the distance to the specified target node from the current node.
        /// </summary>
        /// <param name="target">The target to get the distance to.</param>
        /// <returns>The distance to target.</returns>
        public float GetDistance(UUID target) {
            return _distances.ContainsKey(target) ? _distances[target] : -1f;
        }

        /// <inhertidoc />
        public abstract void VisualiseAlgorithm(Parameters parameters);

        /// <inhertidoc />
        public abstract void VisualiseAlgorithm(UUID to, Parameters parameters);

        /// <inhertidoc />
        public abstract void ProcessPacket(IMPacket packet);

        public override bool Equals(object obj) {
            if (obj != null && obj.GetType().Equals(GetType()))
                return ID.Equals(((AbstractAlgorithmNode)obj).ID);
            return false;
        }

        public override int GetHashCode() {
            return ID.GetHashCode();
        }
    }
}
