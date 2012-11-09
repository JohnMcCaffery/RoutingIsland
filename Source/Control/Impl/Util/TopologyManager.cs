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
using Nini.Config;
using OpenMetaverse;
using System.IO;
using System.Xml;
using common.framework.interfaces.entities;
using System.Drawing;
using common.framework.impl.util;
using log4net;
using Diagrams.Control.impl.Entities;
using Diagrams.Control.impl.Module;

namespace Diagrams.Control.impl.Util {
    public class TopologyManager : FileWorker, ITopologyManager {
        public const string FOLDER_KEY = "TopologyFolder";

        private const string COLOUR = "Colour";

        private const string EPS = "EPs";

        private const string FROM = "From";

        private const string LINKS = "Links";

        private const string NAME = "Name";

        private const string NODES = "Nodes";

        private const string ROUTERS = "Routers";

        private const string SCALE = "Scale";

        private const string ROT = "Rotation";

        private const string ANGLE = "Angle";

        private const string SHIFT = "Shift";

        private const string TO = "To";

        private const string TOPOLOGY = "Topology";

        private const string WEIGHT = "Weight";

        private const string X = "X";

        private const string Y = "Y";

        private const string Z = "Z";

        private readonly string _schemaFile;

        protected readonly string _topologyFolder = "Topologies";

        protected readonly string _prefix = "ni";

        protected readonly string Namespace = "RoutingProject";

        public static int NodeCount = 0;

        private readonly IControlUtil _control;

        private readonly bool _rotateWithHost;

        private readonly bool _packetsOnLoad;

        private static ILog Logger = LogManager.GetLogger(typeof(TopologyManager));
        
        protected IControlUtil Control {
            get {
                return _control;
            }
        }

        public TopologyManager(IConfig controlConfig, IControlUtil controlUtil)
            : base(controlConfig) {
            _control = controlUtil;
            _schemaFile = controlConfig.GetString("TopologySchema", null);
            _topologyFolder = controlConfig.Get(FOLDER_KEY, ".");
            _rotateWithHost = controlConfig.GetBoolean("RotateWithHost", true);
            _packetsOnLoad = controlConfig.GetBoolean("PacketsOnLoad", false);
            Namespace = controlConfig.Get("Namespace", Namespace);
        }

        private XmlDocument Validate(string shortFile, string file) {
            string shortSchema = _schemaFile == null ? null : _schemaFile.Replace(AppDomain.CurrentDomain.BaseDirectory, "");
            if (file == null) {
                Control.Say("Unable to load topology. The supplied file name was null.");
                return null;
            }
            if (!File.Exists(file)) {
               Control.Say("Unable to load topology from '" + shortFile + "'. It does not exist.");
                return null;
            }
            XmlDocument doc = new XmlDocument();
            doc.Load(file);
            if (_schemaFile == null || !File.Exists(_schemaFile)) {
                Control.Say("Unable to validate '" + shortFile + "'. " + (_schemaFile == null ? "No schema specified." : "Schema file '" + shortSchema + "' does not exist."));
                return doc;
            }

            doc.Schemas.Add(Namespace, _schemaFile);
            doc.Validate((sender, e) => {
                if (!e.Message.Contains(Namespace + ":Name") &&
                    !e.Message.Contains(Namespace + ":X") &&
                    !e.Message.Contains(Namespace + ":Y") &&
                    !e.Message.Contains(Namespace + ":Z") &&
                    !e.Message.Contains(Namespace + ":From") &&
                    !e.Message.Contains(Namespace + ":To"))
                    Control.Say("Error validating " + file + ". " + e.Message);
            });

            return doc;
        }

        public override string GetUserFolder(string name) {
            return Path.Combine(base.GetUserFolder(name), _topologyFolder);
        }

        public override string SharedFolder {
            get { return _sharedFolder == null ? null : Path.Combine(base.SharedFolder, _topologyFolder); }
        }

        /// <summary>
        /// Load a topology from a specified XML events.
        /// </summary>
        /// <param name="events">The events to load the topology from.</param>
        public virtual void LoadTopology(string user, UUID userID, string file) {
            string shortFile = file;
            file = GetFile(GetUserFolder(user), file);
            XmlDocument doc = Validate(shortFile, file);
            if (doc == null) return;

            Vector3 shift = GetShift(doc);
            Quaternion rot = GetRot(doc);
            float scale = GetScale(doc.GetElementsByTagName(TOPOLOGY, Namespace)[0]);
            Dictionary<string, INode> mappedNodes = new Dictionary<string, INode>();

            int nodes = LoadNodeSet(doc, NODES, rot, shift, scale, user, userID, mappedNodes);
            Control.Say(nodes < 0 ? "No Nodes found to load" : nodes + " Nodes loaded");

            int links = LoadLinkSet(doc, mappedNodes, user, userID);
            Control.Say(links < 0 ? "No Links found to load" : links + " Links loaded");
        }

        private int LoadNodeSet(XmlDocument doc, string parent, Quaternion rot, Vector3 shift, float scale, string user, UUID userID, Dictionary<string, INode> mappedNodes) {
            XmlNodeList parents = doc.GetElementsByTagName(parent, Namespace);
            if (parents == null || parents.Count == 0)
                return -1;
            int startCount = mappedNodes.Count;
            foreach (XmlNode node in parents[0].ChildNodes) {
                if (node is XmlComment)
                    continue;
                bool valid = true;
                if (_schemaFile != null) doc.Validate((source, ex) => {
                    valid = false;
                    if (ex.Message.Equals("The required attribute '" + Namespace + ":Name' is missing."))
                        Control.Say("Unable to load " + node.LocalName + ". " + ex.Message);
                    else
                        Control.Say("Unable to load " + node.Attributes[NAME, Namespace].Value + ". " + ex.Message);
                }, node);
                if (valid) {
                    INode n = LoadNode(node, rot, shift, scale, user, userID, mappedNodes);
                    if (n != null)
                        mappedNodes.Add(n.Name, n);
                }
            }
            return mappedNodes.Count - startCount;
        }

        private int LoadLinkSet(XmlDocument doc, Dictionary<string, INode> mappedNodes, string user, UUID userID) {
            int loadedLinks = 0;
            XmlNodeList links = doc.GetElementsByTagName(LINKS, Namespace);
            if (links != null && links.Count != 0) {
                foreach (XmlNode link in links[0].ChildNodes) {
                    if (link is XmlComment)
                        continue;
                    bool valid = true;
                    if (_schemaFile != null) doc.Validate((source, ex) => { valid = false; Control.Say("Unable to load Link. " + ex.Message); }, link);
                    if (valid && LoadLink(doc, link, mappedNodes, user, userID)) loadedLinks++;
                }
            } else
                loadedLinks = -1;
            return loadedLinks;
        }

        private INode LoadNode(XmlNode node, Quaternion rot, Vector3 shift, float scale, string user, UUID userID, Dictionary<string, INode> mappedNodes) {
            XmlAttribute nameAttr = node.Attributes[NAME, Namespace];
            if (nameAttr == null) {
                Control.Say("Unable to load " + node.LocalName + ". No name attribute defined.");
                return null;
            }
            var name = nameAttr.Value;
            if (mappedNodes.ContainsKey(name)) {
                Control.Say("Skipping " + name + ". A node with that name has already been created.");
                return null;
            }
            NodeCount++;
            Vector3 pos = Vector3.Zero;
            try {
                pos = GetPos(node, rot, shift, scale);
            } catch (Exception e) {
                Control.Say("Unable to load " + name + ". " + e.Message);
                return null;
            }

            //Control.HostPrim.Say(name + ": " + (pos - Control.HostPrim.Pos) + " / " + GetVector(node));

            var parameters = new Parameters();
            Color colour = Diagrams.Control.Impl.Module.Control.GetColour(NodeCount);

            XmlAttribute colourAttr = node.Attributes[COLOUR, Namespace];
            if (colourAttr != null) {
                colour = Color.FromName(colourAttr.Value);
                if (!colour.IsKnownColor || colour.ToKnownColor() == KnownColor.Transparent) {
                    try {
                        int colourInt;
                        if (Int32.TryParse(colourAttr.Value, out colourInt))
                            colour = Color.FromArgb(colourInt);
                        else {
                            Logger.Info("Unable to parse colour from " + colourAttr.Value + ". It does not parse to an integer.");
                            colour = Diagrams.Control.Impl.Module.Control.GetColour(NodeCount);
                        }
                    } catch (Exception e) {
                        Logger.Info("Unable to parse colour from " + colourAttr.Value + ". It cannot be parsed as a ARGB value.");
                        colour = Diagrams.Control.Impl.Module.Control.GetColour(NodeCount);
                    }
                }
            }
            return LoadNode(name, node, new Parameters(), pos, colour, scale, user, userID);
        }

        private Vector3 GetPos(XmlNode node, Quaternion rot, Vector3 shift, float scale) {
            Vector3 pos = Vector3.Zero;
            pos = Vector3.Multiply(GetVector(node), scale);

            if (!rot.Equals(Quaternion.Identity)) {
                Matrix4 x = Matrix4.CreateFromQuaternion(rot);
                Vector3 diff = pos;
                pos = Vector3.Transform(diff, x);
            }

            return pos + shift;
        }

        protected virtual INode LoadNode(string name, XmlNode xmlNode, Parameters parameters, Vector3 position, Color colour, float scale, string user, UUID userID) {
            return Control.AddNode(name, parameters, user, userID, position, colour);
        }

        private bool LoadLink(XmlDocument doc, XmlNode link, Dictionary<string, INode> mappedNodes, string user, UUID userID) {
            XmlAttribute fromAttr = link.Attributes[FROM, Namespace];
            XmlAttribute toAttr = link.Attributes[TO, Namespace];

            if (fromAttr == null && toAttr == null) {
                Control.Say("Unable to load Link. No From or To attributes defined.");
                return false;
            } else if (fromAttr == null) {
                Control.Say("Unable to load Link. No From attribute defined.");
                return false;
            } else if (toAttr == null) {
                Control.Say("Unable to load Link. No To attribute defined.");
                return false;
            }

            string fromName = fromAttr.Value;
            string toName = toAttr.Value;
            if (!mappedNodes.ContainsKey(fromName) && !mappedNodes.ContainsKey(toName))
                Control.Say("Unable to load link between " + fromName + " and " + toName + ". Neither node exists.");
            else if (!mappedNodes.ContainsKey(fromName))
                Control.Say("Unable to load link between " + fromName + " and " + toName + ". " + fromName + " does not exist.");
            else if (!mappedNodes.ContainsKey(toName))
                Control.Say("Unable to load link between " + fromName + " and " + toName + ". " + toName + " does not exist.");
            else if (fromName.Equals(toName))
                Control.Say("Unable to load link between " + fromName + " and " + toName + ". Both names are the same.");
            else {
                INode from = mappedNodes[fromName];
                INode to = mappedNodes[toName];
                XmlAttribute weightAttr = link.Attributes[WEIGHT, Namespace];
                float weight = weightAttr != null ? float.Parse(weightAttr.Value) : default(float);
                Control.AddLink(from.ID, to.ID, new Parameters("Visualise", _packetsOnLoad), user, userID, weight);
                return true;
            }
            return false;
        }

        private XmlNode SaveLinks(XmlDocument doc, string name, UUID id) {
            XmlNode linksNode = doc.CreateElement(_prefix, "Links", Namespace);

            Control.ForAllLinks(name, id, link => {
                XmlNode linkNode = doc.CreateElement(_prefix, "Link", Namespace);
                XmlAttribute fromAttr = doc.CreateAttribute(_prefix, "From", Namespace);
                XmlAttribute toAttr = doc.CreateAttribute(_prefix, "To", Namespace);
                XmlAttribute weightAttr = doc.CreateAttribute(_prefix, "Weight", Namespace);

                fromAttr.Value = Control.GetNode(link.FromID).Name;
                toAttr.Value = Control.GetNode(link.ToID).Name;
                weightAttr.Value = link.Weight.ToString();

                linkNode.Attributes.Append(fromAttr);
                linkNode.Attributes.Append(toAttr);
                if (!link.DistanceWeight)
                    linkNode.Attributes.Append(weightAttr);

                linksNode.AppendChild(linkNode);
            });
            return linksNode;
        }

        private XmlNode SaveNodes(XmlDocument doc, string name, UUID id) {
            XmlNode nodes = doc.CreateElement(_prefix, "Nodes", Namespace);
            Control.ForAllNodes(name, id, node => {
                nodes.AppendChild(SaveNode(doc, name, id, node));
            });
            return nodes;
        }

        /// <summary>
        /// Save the current topology to an XML events.
        /// </summary>
        /// <param name="events">The filename to save the events to.</param>
        public void SaveTopology(string name, UUID id, string file) {
            string shortFile = file;
            file = GetFile(GetUserFolder(name), file);
            Logger.Warn("Saving topology as " + file);
            if (file == null)
                return;
            XmlDocument doc = new XmlDocument();
            XmlNode declaration = doc.CreateXmlDeclaration("1.0", "utf-8", "yes");

            XmlAttribute nsPrefixAttr = doc.CreateAttribute("ni", "xmlns");
            nsPrefixAttr.Value = Namespace;

            doc.AppendChild(declaration);
            doc.AppendChild(CreateRoot(doc, name, id));

            doc.Save(file);
            Control.Say("Saved topology as '" + shortFile + "'.");
        }

        private XmlElement CreateRoot(XmlDocument doc, string name, UUID id) {
            XmlElement root = doc.CreateElement(_prefix, "Topology", Namespace);
            XmlNode nodesNode = SaveNodes(doc, name, id);

            if (nodesNode.ChildNodes.Count > 0) {
                root.AppendChild(nodesNode);
                XmlNode linksNode = SaveLinks(doc, name, id);
                if (linksNode.ChildNodes.Count > 0)
                    root.AppendChild(linksNode);
            }
            return root;
        }

        private Vector3 GetShift(XmlDocument doc) {
            XmlNodeList shiftNodes = doc.GetElementsByTagName(SHIFT, Namespace);
            if (shiftNodes == null || shiftNodes.Count == 0)
                return Control.HostPrim.Pos;
            return Vector3.Add(Control.HostPrim.Pos, GetVector(shiftNodes[0]));
        }

        private Quaternion GetRot(XmlDocument doc) {
            XmlNodeList rotNodes = doc.GetElementsByTagName(ROT, Namespace);
            Quaternion rot = _rotateWithHost ? Control.HostPrim.Rotation : Quaternion.Identity;
            if (rotNodes == null || rotNodes.Count == 0)
                return rot;
            float angle = float.Parse(rotNodes[0].Attributes[ANGLE, Namespace].Value);
            angle = (float)(angle * (Math.PI / 180f));
            return rot * Quaternion.CreateFromAxisAngle(GetVector(rotNodes[0]), angle);
        }

        private Vector3 GetVector(XmlNode node) {
            XmlAttribute xAttr = node.Attributes[X, Namespace];
            XmlAttribute yAttr = node.Attributes[Y, Namespace];
            XmlAttribute zAttr = node.Attributes[Z, Namespace];

            if (xAttr == null && yAttr == null && zAttr == null)
                throw new Exception("Unable to construct vector, expected X, Y and Z attributes were not found.");
            if (xAttr == null && yAttr == null)
                throw new Exception("Unable to construct vector, expected X and Y attributes were not found.");
            if (xAttr == null && zAttr == null)
                throw new Exception("Unable to construct vector, expected X and Z attributes were not found.");
            if (xAttr == null)
                throw new Exception("Unable to construct vector, expected X attribute was not found.");
            if (yAttr == null && zAttr == null)
                throw new Exception("Unable to construct vector, expected Y and Z attributes were not found.");
            if (yAttr == null)
                throw new Exception("Unable to construct vector, expected Y attribute was not found.");
            if (zAttr == null)
                throw new Exception("Unable to construct vector, expected Z attribute was not found.");

            float x = float.Parse(xAttr.Value);
            float y = float.Parse(yAttr.Value);
            float z = float.Parse(zAttr.Value);
            return new Vector3(x, y, z);
        }

        private float GetScale(XmlNode topologyNode) {
            XmlAttribute scaleAttr = topologyNode.Attributes[SCALE, Namespace];
            return scaleAttr != null ? float.Parse(scaleAttr.Value) : 1f;
        }

        public virtual XmlNode SaveNode(XmlDocument doc, string name, UUID id, IControlNode node) {
            XmlNode nodeElement = doc.CreateElement("Node", Namespace);
            XmlAttribute nameAttr = doc.CreateAttribute(_prefix, "Name", Namespace);
            XmlAttribute xAttr = doc.CreateAttribute(_prefix, "X", Namespace);
            XmlAttribute yAttr = doc.CreateAttribute(_prefix, "Y", Namespace);
            XmlAttribute zAttr = doc.CreateAttribute(_prefix, "Z", Namespace);
            XmlAttribute colourAttr = doc.CreateAttribute("Colour", Namespace);

            Vector3 pos = Vector3.Subtract(Control.GetNode(node.ID).Pos, Control.HostPrim.Pos);

            nameAttr.Value = node.Name;
            xAttr.Value = pos.X.ToString();
            yAttr.Value = pos.Y.ToString();
            zAttr.Value = pos.Z.ToString();
            colourAttr.Value = node.Colour.IsKnownColor ? node.Colour.Name : node.Colour.ToArgb().ToString();

            nodeElement.Attributes.Append(nameAttr);
            nodeElement.Attributes.Append(xAttr);
            nodeElement.Attributes.Append(yAttr);
            nodeElement.Attributes.Append(zAttr);
            nodeElement.Attributes.Append(colourAttr);

            return nodeElement;
        }
    }
}
