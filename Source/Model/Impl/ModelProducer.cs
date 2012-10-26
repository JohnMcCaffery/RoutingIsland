#region Namespace imports

using System;
using OpenMetaverse;
using common;
using common.framework.impl.util;
using common.framework.interfaces.entities;
using common.framework.interfaces.layers;
using common.model.framework.interfaces;

#endregion

namespace Diagrams {
    public partial class Model {
        /*
        #region IModenConfig 

        /// <inheritdoc />
        public void addNode(INode node) {
            recalculate = true;
            qEvent(Events.AddNode, new Parameters(new[] {"Node"}, new Object[] {node}));
        }

        /// <inheritdoc />
        public void removeNode(UUID n) {
            recalculate = true;
            qEvent(Events.RemoveNode, new Parameters(new[] {"ID"}, new Object[] {n}));
        }

        /// <inheritdoc />
        public void addLink(ILink link) {
            recalculate = true;
            qEvent(Events.AddLink, new Parameters(new[] {"Link"}, new Object[] {link}));
        }

        /// <inheritdoc />
        public void removeLink(UUID l) {
            recalculate = true;
            qEvent(Events.RemoveLink, new Parameters(new[] {"ID"}, new Object[] {l}));
        }

        /// <inheritdoc />
        public void showRoutingTable(UUID n) {
            qEvent(Events.RoutingTable, new Parameters(new[] {"ID"}, new Object[] {n}));
        }

        /// <inheritdoc />
        public void visualiseAlgorithm(UUID root, Parameters parameters) {
            parameters["Root"] = root;
            qEvent(Events.CalculateRouting, parameters);
        }

        /// <inheritdoc />
        public void visualiseAlgorithm(UUID root, UUID to, Parameters parameters) {
            parameters["Root"] = root;
            parameters["ToID"] = to;
            qEvent(Events.CalculateRouting, parameters);
        }

        /// <inheritdoc />
        public void setWait(int speed) {
            qEvent(Events.Speed, new Parameters(new[] {"Speed"}, new Object[] {speed}));
        }

        #endregion

        #region IModelNetwork

        private void qEvent(Events evt, Parameters parameters) {
            if (!shuttingDown)
                eventQ.qItem(new Pair<Events, Parameters>(evt, parameters));
        }

        /// <summary>
        ///   Pass a packet down to the physical layer
        /// </summary>
        /// <param name = "from">The node the packet should start at</param>
        /// <param name = "link">The link along which the node should travel</param>
        /// <param name = "packet">The packet to send</param>
        private void passDown(IMNode from, IMLink link, IMPacket packet) {
            qEvent(Events.PassDown,
                   new Parameters(new[] {"FromID", "Link", "Packet"}, new Object[] {from, link, packet}));
        }

        /// <inheritdoc />
        private void processPacket(IMNode at, IPacket p) {
            qEvent(Events.ProcessPacket, new Parameters(new[] {"At", "packet"}, new Object[] {at, p}));
        }

        /// <summary>
        ///   Recalculate the routing for all nodes
        /// </summary>
        private void notifyChange() {
            recalculate = true;
            qEvent(Events.Changed, new Parameters());
        }

        #endregion
         * */
    }
}