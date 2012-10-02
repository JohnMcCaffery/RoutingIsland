#region Namespace imports

using OpenMetaverse;

using common.framework.impl.util;
using common.framework.interfaces.entities;
using common.framework.interfaces.layers;
using common.model.framework.interfaces;
using System;

#endregion

namespace Diagrams {
    /// <summary>
    ///   Implementation of the INetworkLayer interface within the SimulationLayer class
    /// </summary>
    public partial class Model {

        /*
        private string currentAlgorithm;

        #region IModelNetwork Members

        /// <inheritdoc />
        public void send(UUID from, UUID to, Parameters parameters) {
            if (currentAlgorithm == null)
                throw new Exception("Unable to send packet, no Algorithm set");
            if (!nodes.ContainsKey(from) || !nodes[from].ContainsKey(currentAlgorithm))
                throw new Exception("Unable to send packet, sender not found");
            if (!nodes.ContainsKey(to) || !nodes[to].ContainsKey(currentAlgorithm))
                throw new Exception("Unable to send packet, target not found");

            IMNode fromNode = nodes[from][currentAlgorithm];
            IMNode toNode = nodes[to][currentAlgorithm];
            //DB.Print("Sending packet from " + fromNode.Name + " to " + toNode.Name, Levels.MODEL);
            processPacket(fromNode,
                          new MPacket(fromNode, fromNode, nodes[to][currentAlgorithm], PTypes.data, parameters));
        }

        public void processPacket(UUID at, UUID p) {
            if (!packets.ContainsKey(p)) {
                //DB.Dropped("Unkown packet arrived at the network layer");
                return;
            }
            IMPacket packet = packets[p];
            if (!nodes.ContainsKey(at)) {
                //DB.Dropped("Packet arrived at network layer for unkown node");
                return;
            }
            if (!nodes[at].ContainsKey(currentAlgorithm) && !nodes[at].ContainsKey(packet.Algorithm)) {
                //DB.Dropped("Packet arrived at network layer but no algorithm set");
                return;
            }

            var alg = nodes[at].ContainsKey(packet.Algorithm) ? packet.Algorithm : currentAlgorithm;
            packets.Remove(packet.ID);
            processPacket(nodes[at][alg], packet);
        }

        /// <inheritdoc />
        public void notifyChange(UUID l, float weight) {
            if (!links.ContainsKey(l))
                //throw new ModelException("Unable to change weight of link, link not found");
                return;

            IMLink link = links[l];
            foreach (IAlgorithm alg in algorithms) {
                nodes[link.FromID][alg.Name].changeLink(link);
                nodes[link.ToID][alg.Name].changeLink(link);
                //link.From.changeLink(link);
                //link.To.changeLink(link);
            }
        }

        public void resetAll() {
            onReset();
        }

        #endregion

        #region Private Methods

        private void processPacketEvent(Parameters parameters) {
            if (!parameters.checkType("At", mNodeType) || !parameters.checkType("Packet", packetType))
                throw new ModelException("Invalid parameters for receive event");

            var at = parameters["At"] as IMNode;
            var packet = parameters["packet"] as IMPacket;

            recalculateRouting();

            at.receive(packet);
        }

        private void passDownEvent(Parameters parameters) {
            if (!parameters.checkType("FromID", mNodeType) || !parameters.checkType("FromID", mNodeType) ||
                !parameters.checkType("FromID", mNodeType))
                throw new ModelException("Invalid parameters for receive event");

            var from = parameters["FromID"] as IMNode;
            var link = parameters["Link"] as IMLink;
            var packet = parameters["Packet"] as IMPacket;

            if (from == null || link == null || packet == null || !from.InWorld || !link.InWorld ||
                !link.OtherEnd(from).InWorld) {
                //DB.Dropped("Network layer dropped " + packet.Name + " whilst transferring down to the physical layer");
                //packet.say("Dropped whilst transferring down to the physical layer");
                return;
            }

            recalculateRouting();

            packets.Add(packet.ID, packet);

            view.send(from.ID, link.ID, packet);
        }
        /// <summary>
        /// Receive a packet that is to be sent from above or from a process on this level
        /// </summary>
        /// <param name="packet">The packet that was received</param>
        private void route(IMPacket packet) {
            ID from = packet.Hop.ID;
            if (!nodes.ContainsKey(from) || !nodes[from].ContainsKey(currentAlgorithm) || !nodes[from][currentAlgorithm].InWorld) {
                if (!nodes.ContainsKey(from))
                    //DB.Dropped("Network layer dropped " + packet.Name + " whilst routing, cannot lookup specified recipient");
                else if (!nodes[from].ContainsKey(currentAlgorithm))
                    //DB.Dropped("Network layer dropped " + packet.Name + " whilst routing, specified recipient does not have an instance for " + currentAlgorithm + " algorithm");
                else
                    //DB.Dropped("Network layer dropped " + packet.Name + " whilst routing, specified recipient is not in world");
                packet.say("Dropped whilst routing");
                return;
            }
            recalculateRouting();

            if (!packets.ContainsKey(packet.ID))
                packets.Add(packet.ID, packet);

            //DB.Print("Starting routing for "+ packet.Name, Levels.MODEL);
            processPacket(from, packet.ID);
        }

        #endregion
         * */
    }
}