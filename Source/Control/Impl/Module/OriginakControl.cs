using System;
using System.Collections.Generic;
using System.Drawing;
using OpenMetaverse;
using common.framework.impl.util;
using common.framework.interfaces.basic;
using common.framework.interfaces.entities;
using common.framework.interfaces.layers;
using common.interfaces.entities;
using Diagrams.Common.interfaces.keytable;
using common.Queue;
using System.Xml;
using System.Globalization;
using log4net;
using common.config;
using System.IO;
using System.Xml.Schema;
using StAndrews.NeworkIsland.Framework.Util.Logger.Opensim;
using Diagrams.Common;
using jm726.lib.wrapper.logger;
using StAndrews.NetworkIsland.Control;
using common;
using Nini.Config;
using System.Threading;
using jm726.lib.Serialization;
using System.Linq;
using System.Diagnostics;
using JM726.Lib.Static;

namespace Diagrams.Control.Impl.Module {
    public class OriginalControl : AbstractModule<IControlNode, ILink>, IControl {
        public static int NodeCount = 0;

        #region Private Fields

        private IPrim _hostPrim;
        private IModel _model;
        private IPrimFactory _factory;
        private int _wait;

        private bool _recordingEnabled;
        private bool _timing;
        private bool _currentlyRecording;

        private OpenSimLogReader _reader;
        private IXmlLogWriter<IModel> _modelWriter;
        private IKeyTable<IXmlLogWriter> _writers;
        /// <summary>
        /// When recording this replaces calls to base methods
        /// </summary>
        private IModule _recordingBase;


        private Dictionary<string, UUID> _readerMap;
        private IKeyTable<string> _writerMap;

        #endregion

        #region Protected fields

        //TODO get + set god + Name
        protected readonly UUID _godID = UUID.Random();
        protected readonly string _godName = "Routing Project";
        protected readonly string _userFolder;
        private readonly IKeyTable<bool> _paused;

        private bool _beingLogged;

        #endregion

        #region Protected Properties

        protected IPrim HostPrim {
            get { return _hostPrim; }
        }

        protected IModel Model {
            get { return _model; }
        }
        
        protected bool GetPaused(string name, UUID id) {
            if (!_paused.ContainsKey(id))
                _paused.Add(id, false);
            return _paused[id];
        }

        protected void SetPaused(string name, UUID id, bool value) {
            if (!_paused.ContainsKey(id))
                _paused.Add(id, false);
            _paused[id] = value;
            Model.Paused = value;
            if (value)
                Util.Wake(_reader);
            else {
                _reader.PlayNextEvent(false);
                QueueNextEvent(name, id);
            }
        }

        /// <summary>
        /// The prim factory which can be used to create primitives or listen for chat events.
        /// </summary>
        protected IPrimFactory Factory {
            get { return _factory; }
        }

        #endregion

        #region Constructor

        public OriginalControl(IKeyTableFactory tableFactory, IAsynchQueueFactory queueFactory, IPrimFactory primFactory, IModel model, UUID hostID, IConfigSource config)
            : base(tableFactory, queueFactory) {

            _hostPrim = primFactory[hostID];

            _readerMap = new Dictionary<string, UUID>();
            _writerMap = tableFactory.MakeKeyTable<string>();
            _paused = tableFactory.MakeKeyTable<bool>();

            IConfig controlConfig = config.Configs["Control"];
            IConfig commonConfig = config.Configs["Common"];
            if (controlConfig == null)
                controlConfig = config.Configs[0];
            if (commonConfig == null)
                commonConfig = config.Configs[0];

            _wait = commonConfig.GetInt("Wait", 50);
            _userFolder = controlConfig.Get("UserFolder", ".");
            _recordingEnabled = controlConfig.GetBoolean("RecordingEnabled", false);
            _timing = controlConfig.GetBoolean("TimedPlayback", true);
            _schemaFile = controlConfig.GetString("TopologySchema", null);
            
            _reader = new OpenSimLogReader(_readerMap, model, HostPrim.Pos);
            _reader.MapInstance<IModule>(this);
            _writers = tableFactory.MakeKeyTable<IXmlLogWriter>();

            _factory = primFactory;
            if (_recordingEnabled) {
                _modelWriter = new OpenSimLogWriter<IModel>(_writerMap, model, HostPrim.Pos, true, false);
                _model = _modelWriter.Instance;
                IXmlLogWriter<IModule> baseWriter = new OpenSimLogWriter<IModule>(_writerMap, this, HostPrim.Pos, true);
                _recordingBase = baseWriter.Instance;
                _writers.Add(hostID, baseWriter);
            } else 
                _model = model;

            Namespace = controlConfig.Get("Namespace", Namespace);
            Logger.Info("Control started.");
        }

        protected string GetPath(string username) {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.Combine(_userFolder, username));
            if (!File.Exists(folder))
                Directory.CreateDirectory(folder);
            return folder;
        }

        protected string GetFile(string folder, string file) {
            if (Path.IsPathRooted(file)) {
                HostPrim.Say("Unable to get topology at '" + file + "'. Filename must be a relative path.");
                return null;
            }
            if (!File.Exists(folder))
                Directory.CreateDirectory(folder);
            file = Path.Combine(folder, Path.GetFileName(file));
            if (!Path.GetExtension(file).ToUpper().Equals(".XML"))
                file += ".xml";
            return file;
        }

        #endregion

        #region IModule Members

        public override bool Paused {
            get {
                return _paused.Count > 0 && _paused.First();
            }
            set { }
        }

        /// <summary>
        /// Perform the specified action on every node mapped by the module.
        /// </summary>
        /// <param name="doThis">The delegate function to be performed on each node.</param>
        protected void ForAllNodes(string name, UUID id, Action<IControlNode> doThis) {
            ForAllNodes(node => {
                if (Authorize(node.ID, name, id))
                    doThis(node);
            });
        }

        /// <summary>
        /// Perform the specified action on every link mapped by the module.
        /// </summary>
        /// <param name="doThis">The delegate function to be performed on each link.</param>
        protected void ForAllLinks(string name, UUID id, Action<ILink> doThis) {
            ForAllLinks(link => {
                if (Authorize(link.ID, name, id))
                    doThis(link);
            });
        }

        public override int Wait {
            get { return _wait; }
            set {
                if (_currentlyRecording) {
                    lock (_modelWriter) {
                        if (!_beingLogged) {
                            _beingLogged = true;
                            _recordingBase.Wait = value;
                            _beingLogged = false;
                            return;
                        }
                    }
                } 
                _wait = value;
                Model.Wait = value;
            }
        }

        public override ILink AddLink(UUID from, UUID to, Parameters parameters, float weight = default(float), bool bidirectional = true) {
            if (_currentlyRecording) {
                lock (_modelWriter) {
                    if (!_beingLogged) {
                        _beingLogged = true;
                        ILink ret = _recordingBase.AddLink(from, to, parameters, weight, bidirectional);
                        _beingLogged = false;
                        return ret;
                    }
                }
            }
            ILink l = base.AddLink(from, to, parameters, weight, bidirectional);
            return l;
        }

        public override INode AddNode(string name, Parameters parameters, Vector3 position = default(Vector3), Color colour = default(Color)) {
            if (_currentlyRecording) {
                lock (_modelWriter) {
                    if (!_beingLogged) {
                        _beingLogged = true;
                        INode ret = _recordingBase.AddNode(name, parameters, position, colour);
                        _beingLogged = false;
                        return ret;
                    }
                }
            }
            return base.AddNode(name, parameters, position, colour);
        }

        protected override void RemoveLink(ILink link, Parameters parameters) {
            UUID l = link.ID;
            if (_currentlyRecording) {
                if (_writers.ContainsKey(l)) 
                    _writers[l].StopRecording();
                lock (_modelWriter) {
                    if (!_beingLogged) {
                        _beingLogged = true;
                        _recordingBase.RemoveLink(l, parameters);
                        UnMapID(link.ID, link.Name);
                        _beingLogged = false;
                        return;
                    }
                }
            }
            base.RemoveLink(link, parameters);
            Model.RemoveLink(l, parameters);
            if (!_currentlyRecording)
                UnMapID(link.ID, link.Name);
        }

        public override void RemoveNode(UUID n, Parameters parameters) {
            if (!IsNode(n))
                return;

            foreach (ILink l in GetLinks(n))
                RemoveLink(l.ID, parameters);

            RemoveNode(GetNode(n), parameters);
        }

        private void RemoveNode(INode node, Parameters parameters) {
            UUID n = node.ID;
            if (_currentlyRecording) {
                if (_writers.ContainsKey(n))
                    _writers[n].StopRecording();
                lock (_modelWriter) {
                    if (!_beingLogged) {
                        _beingLogged = true;
                        _recordingBase.RemoveNode(n, parameters);
                        UnMapID(n, node.Name);
                        _beingLogged = false;
                        return;
                    }
                }
            }
            base.RemoveNode(n, parameters);
            Model.RemoveNode(n, parameters);
            if (!_currentlyRecording)
                UnMapID(n, node.Name);
        }

        public override void Stop() {
            Logger.Debug("Control stopping.");
            StopPlayback();
            base.Stop();
            Model.Stop();
            Logger.Info("Control stopped.");
        }

        #endregion

        #region IControl Members


        public void Clear(UUID id, string sender) {
            _queue.Paused = true;
            Logger.Info("Clearing.");
            ForAllLinks(link => {
                Logger.Info("Queueing clear for " + link.Name + ".");
                _queue.QWork("Clear Link " + link.Name, () => {
                    Logger.Info("Clearing " + link.Name + ".");
                    RemoveLink(link.ID, new Parameters());
                });
            });
            ForAllNodes(node => {
                Logger.Info("Queueing clear for " + node.Name + ".");
                _queue.QWork("Clear Node " + node.Name, () => {
                    Logger.Info("Clearing " + node.Name + ".");
                    RemoveNode(node.ID, new Parameters());
                });
            });
            _queue.Paused = false;
            //Hold the system 1 minute or until the queue finishes working.
            //Util.Wait(60000, _queue.IsWorking, _queue);
            _queue.BlockWhileWorking();
        }

        #endregion

        #region AbstractModule implementations

        protected override ILink MakeLink(UUID from, UUID to, Parameters parameters, float weight = default(float), bool bidirectional = true) {

            Logger.Debug("Creating link between '" + GetNode(from).Name + "' and '" + GetNode(to).Name + "'.");
            if (!_recordingEnabled) {
                ILink l = _model.AddLink(from, to, parameters, weight, bidirectional);
                Logger.Debug("Created  '" + l.Name + "'.");
                return l;
            }
            ILink link = new ControlLink(_model.AddLink(from, to, parameters, weight, bidirectional), GetNode(from), GetNode(to));

            link = MapID<ILink>(link);

            Logger.Info("Created  '" + link.Name + "'.");
            return link;
        }

        protected override IControlNode MakeNode(string name, Parameters parameters, Vector3 position = default(Vector3), Color colour = default(Color)) {

            Logger.Debug("Creating node '" + name + "'.");
            IControlNode node = new ControlNode(_model.AddNode(name, parameters, position, colour), position);
            if (!_recordingEnabled) {
                Logger.Info("Created node '" + name + "'.");
                return node;
            }

            node = MapID<IControlNode>(node);
            Logger.Info("Created node '" + name + "'.");
            return node;
        }

        /// <summary>
        /// Map a string to an ID so it can be looked up when serializing and deserializing.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        private T MapID<T>(T instance) where T : class, ILogicEntity {
            if (_readerMap.ContainsKey(instance.Name))
                _readerMap[instance.Name] = instance.ID;
            else
                _readerMap.Add(instance.Name, instance.ID);
            _reader.MapInstance<T>(instance);

            if (_recordingEnabled) {
                IXmlLogWriter<T> writer = new OpenSimLogWriter<T>(_writerMap, instance, HostPrim.Pos, true);
                _writers.Add(instance.ID, writer);
                _writerMap.Add(instance.ID, instance.Name);
                if (_currentlyRecording)
                    writer.StartRecording(_modelWriter.Log);
                return writer.Instance;
            }
            return instance;
        }

        private void UnMapID(UUID id, string name) {
            if (_readerMap.ContainsKey(name))
                _readerMap.Remove(name);

            if (_recordingEnabled && _writers.ContainsKey(id)) {
                _writers[id].StopRecording();
                _writers.Remove(id);
                _writerMap.Remove(id);
            }
        }

        #endregion

        #region Load Topology

        protected readonly string Namespace = "RoutingProject";
        private const string X = "X";
        private const string Y = "Y";
        private const string Z = "Z";

        private const string TOPOLOGY = "Topology";
        private const string SCALE = "Scale";
        
        private const string SHIFT = "Shift";
        private const string NODES = "Nodes";
        private const string ROUTERS = "Routers";
        private const string EPS = "EPs";
        private const string LINKS = "Links";

        private const string NAME = "Name";
        private const string COLOUR = "Colour";

        private const string FROM = "From";
        private const string TO = "To";
        private const string WEIGHT = "Weight";

        private readonly string _schemaFile;

        protected readonly string _topologyFolder = "Topologies";
        protected readonly string _sequenceFolder = "Sequences";

        /// <summary>
        /// Load a topology from a specified XML events.
        /// </summary>
        /// <param name="events">The events to load the topology from.</param>
        protected virtual void LoadTopology(string file, string user, UUID userID) {
            string shortFile = file;
            file = GetFile(Path.Combine(GetPath(user), _topologyFolder), file);
            if (file == null)
                return;
            XmlDocument doc = Validate(shortFile, file);
            if (doc == null) return;

            Vector3 shift = GetShift(doc);
            float scale = GetScale(doc.GetElementsByTagName(TOPOLOGY, Namespace)[0]);
            Dictionary<string, INode> mappedNodes = new Dictionary<string, INode>();
            int nodes = LoadNodeSet(doc, NODES, shift, scale, user, userID, mappedNodes);
            int routers = LoadNodeSet(doc, ROUTERS, shift, scale, user, userID, mappedNodes);
            int eps = LoadNodeSet(doc, EPS, shift, scale, user, userID, mappedNodes);
            
            XmlNodeList links = doc.GetElementsByTagName(LINKS, Namespace);
            int loadedLinks = 0;
            if (links != null && links.Count != 0)
                foreach (XmlNode link in links[0].ChildNodes) {
                    if (link is XmlComment)
                        continue;
                    bool valid = true;
                    if (_schemaFile != null) doc.Validate((source, ex) => { valid = false; Say("Unable to load Link. " + ex.Message); }, link);
                    if (valid && LoadLink(doc, link, mappedNodes, user, userID)) loadedLinks++;
                } 
            else
                loadedLinks = -1;

            Say(nodes < 0 ? "No Nodes found to load" : nodes + " Nodes loaded");
            Say(routers < 0 ? "No Routers found to load" : routers + " Routers loaded");
            Say(eps < 0 ? "No EPs found to load" : eps + " EPs loaded");
            Say(loadedLinks < 0 ? "No Links found to load" : loadedLinks + " Links loaded");
        }

        private int LoadNodeSet(XmlDocument doc, string parent, Vector3 shift, float scale, string user, UUID userID, Dictionary<string, INode> mappedNodes) {
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
                    if (ex.Message.Equals("The required attribute '" + Namespace  + ":Name' is missing."))
                        Say("Unable to load " + node.LocalName + ". " + ex.Message); 
                    else
                        Say("Unable to load " + node.Attributes[NAME, Namespace].Value + ". " + ex.Message); 
                }, node);
                if (valid) {
                    INode n = LoadNode(node, shift, scale, user, userID, mappedNodes);
                    if (n != null)
                        mappedNodes.Add(n.Name, n);
                }
            }
            return mappedNodes.Count - startCount;
        }

        private Vector3 GetShift(XmlDocument doc) {
            XmlNodeList shiftNodes = doc.GetElementsByTagName(SHIFT, Namespace);
            if (shiftNodes == null || shiftNodes.Count == 0)
                return HostPrim.Pos;
            return Vector3.Add(HostPrim.Pos, GetVector(shiftNodes[0]));
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

        private INode LoadNode(XmlNode node, Vector3 shift, float scale, string user, UUID userID, Dictionary<string, INode> mappedNodes) {
            XmlAttribute nameAttr = node.Attributes[NAME, Namespace];
            if (nameAttr == null) {
                Say("Unable to load " + node.LocalName + ". No name attribute defined.");
                return null;
            }
            var name = nameAttr.Value;
            if (mappedNodes.ContainsKey(name)) {
                Say("Skipping " + name + ". A node with that name has already been created.");
                return null;
            }
            NodeCount++;
            Vector3 pos = Vector3.Zero;
            try {
                pos = Vector3.Add(shift, Vector3.Multiply(GetVector(node), scale));
            } catch (Exception e) {
                Say("Unable to load " + name + ". " + e.Message);
                return null;
            }
            var parameters = new Parameters();
            Color colour = GetColour(NodeCount);

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
                            colour = GetColour(NodeCount);
                        }
                    } catch (Exception e) {
                        Logger.Info("Unable to parse colour from " + colourAttr.Value + ". It cannot be parsed as a ARGB value.");
                        colour = GetColour(NodeCount);
                    }
                }
            }
            return LoadNode(name, node, MakeParameters(node, user, userID), pos, colour, scale, user, userID);
        }

        protected virtual INode LoadNode(string name, XmlNode xmlNode, Parameters parameters, Vector3 position, Color colour, float scale, string user, UUID userID) {
            return AddNode(name, parameters, position, colour);
        }

        private bool LoadLink(XmlDocument doc, XmlNode link, Dictionary<string, INode> mappedNodes, string user, UUID userID) {
            XmlAttribute fromAttr = link.Attributes[FROM, Namespace];
            XmlAttribute toAttr = link.Attributes[TO, Namespace];

            if (fromAttr == null && toAttr == null) {
                Say("Unable to load Link. No From or To attributes defined.");
                return false;
            } else if (fromAttr == null) {
                Say("Unable to load Link. No From attribute defined.");
                return false;
            } else if (toAttr == null) {
                Say("Unable to load Link. No To attribute defined.");
                return false;
            }

            string fromName = fromAttr.Value;
            string toName = toAttr.Value;
            if (!mappedNodes.ContainsKey(fromName) && !mappedNodes.ContainsKey(toName)) 
                Say("Unable to load link between " + fromName + " and " + toName + ". Neither node exists.");
            else if (!mappedNodes.ContainsKey(fromName))
                Say("Unable to load link between " + fromName + " and " + toName + ". " + fromName + " does not exist.");
            else if (!mappedNodes.ContainsKey(toName))
                Say("Unable to load link between " + fromName + " and " + toName + ". " + toName + " does not exist.");
            else if (fromName.Equals(toName))
                Say("Unable to load link between " + fromName + " and " + toName + ". Both names are the same.");
            else {
                INode from = mappedNodes[fromName];
                INode to = mappedNodes[toName];
                XmlAttribute weightAttr = link.Attributes[WEIGHT, Namespace];
                float weight = weightAttr != null ? float.Parse(weightAttr.Value) : default(float);
                AddLink(from.ID, to.ID, MakeParameters(link, user, userID), weight);
                return true;
            }
            return false;
        }

        private void Say(string msg) {
            Logger.Info(msg);
            _hostPrim.Say(msg);
        }

        private XmlDocument Validate(string shortFile, string file) {
            string shortSchema = _schemaFile == null ? null : _schemaFile.Replace(AppDomain.CurrentDomain.BaseDirectory, "");
            if (!File.Exists(file)) {
                Say("Unable to load topology from '" + shortFile + "'. It does not exist.");
                return null;
            }
            XmlDocument doc = new XmlDocument();
            doc.Load(file);
            if (_schemaFile == null || !File.Exists(_schemaFile)) {
                Say("Unable to validate '" + shortFile + "'. " + (_schemaFile == null ? "No schema specified." : "Schema file '" + shortSchema + "' does not exist."));
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
                    Say("Error validating " + file + ". " + e.Message);
            });

            return doc;
        }

        /// <summary>
        /// Override this to use the body of an xml element to load in parameters to a newly created node or link.
        /// </summary>
        /// <param name="node">The xml node to parse parameters from.</param>
        /// <returns>A parameters object representing any information stored in the body of node.</returns>
        protected virtual Parameters MakeParameters(XmlNode node, string creator, UUID creatorID) {
            return new Parameters();
        }

        #endregion

        #region Events

        /// <summary>
        /// Start recording all user triggered events.
        /// </summary>
        protected void StartRecording() {
            if (!_recordingEnabled) {
                HostPrim.Say("Recording has beeen disabled in the configuration.");
                return;
            }

            _currentlyRecording = true;
            if (_recordingEnabled) {
                _modelWriter.StartRecording();
                foreach (IXmlLogWriter writer in _writers)
                    writer.StartRecording(_modelWriter.Log);
            }
        }

        /// <summary>
        /// Finish recording all user triggered events.
        /// </summary>
        protected void StopRecording() {
            if (!_recordingEnabled)
                return;

            _currentlyRecording = false;
            _modelWriter.StopRecording();
            foreach (IXmlLogWriter writer in _writers)
                writer.StopRecording();
        }

        /// <summary>
        /// Write the sequence of user triggered events to an XML events.
        /// </summary>
        /// <param name="events">The method of the events to write the sequence of events to.</param>
        /// <param name="topology">The method of the topology events to load in as the start point for the sequence.</param>
        protected void SaveRecording(string name, string file, string topology = null) {
            if (!_recordingEnabled) {
                HostPrim.Say("Recording has beeen disabled in the configuration.");
                return;
            }
            string shortFile = file;
            file = GetFile(Path.Combine(GetPath(name), _sequenceFolder), file);
            if (file == null)
                return;

            if (topology != null) {
                XmlAttribute topologyAttr = _modelWriter.Log.CreateAttribute("Topology");
                topologyAttr.Value = topology;
                _modelWriter.Log.FirstChild.NextSibling.Attributes.Append(topologyAttr);
            }

            _modelWriter.Log.Save(file);
            HostPrim.Say("Saved recording as '" + shortFile + "'");
        }

        /// <summary>
        /// Play back a previously recorded sequence of events.
        /// </summary>
        /// <param name="events">The events where the sequence of events is stored.</param>
        protected void PlayRecording(string name, UUID id, string file) {
            string shortFile = file;
            file = GetFile(Path.Combine(GetPath(name), _sequenceFolder), file);
            if (file == null)
                throw new ArgumentException("Control event playback cannot load null file.");
            if (!File.Exists(file))
                throw new ArgumentException("Control event playback cannot load '" + shortFile + "'. It does not exist in the filesystem.");
            XmlDocument doc = new XmlDocument();
            try {
                doc.Load(file);
            } catch (XmlException e) {
                throw new XmlException("Control event playback cannot load '" + shortFile + "'. " + e.Message);
            }

            XmlAttribute topologyAttr = doc.FirstChild.NextSibling.Attributes != null ? doc.FirstChild.NextSibling.Attributes["Topology"] : null;
            if (topologyAttr != null)
                LoadTopology(topologyAttr.Value, name, id);

            HostPrim.Say("Starting playback of " + shortFile);
            _reader.PlayRecording(doc, true, _timing);
            PlayEvents(name, id);
        }
        /// <summary>
        /// Work through any events in a loaded sequence that have not been played.
        /// If timing put them on the queue with their wait. Otherwise just play them immediately.
        /// </summary>
        private void PlayEvents(string name, UUID id) {
            _stopped = false;
            if (!_timing)
                while (!_stopped && !GetPaused(name, id) && _reader.HasNextEvent)
                    _reader.PlayNextEvent(false);
            else 
                QueueNextEvent(name, id);
        }

        private void QueueNextEvent(string name, UUID id) {
            if (_stopped || !_reader.HasNextEvent || GetPaused(name, id)) 
                return;
            _queue.QWork("Process sequenceEvent", () => {
                Util.Wait(_reader.NextEventWait, !_stopped, _reader);
                if (!_stopped && !GetPaused(name, id)) {
                    _reader.PlayNextEvent(false);
                    QueueNextEvent(name, id);
                }
            });
        }

        private bool _stopped;

        protected void StopPlayback() {
            _stopped = true;
            _reader.StopPlayback();
            Util.Wake(_reader);
        }

        protected bool PlayingSequence {
            get {
                return _reader.HasNextEvent;
            }
        }

        protected void PlayNextEvent(string name, UUID id) {
            if (GetPaused(name, id))
                _reader.PlayNextEvent();
            else
                Util.Wake(_reader);
        }

        #endregion

        #region Util

        /// <summary>
        /// Hook to allow more complex forms of sharing between users. Override this to define who is authorized to do what.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="name"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        protected virtual bool Authorize(UUID entity, string name, UUID id) {
            return true;
        }

        /// <summary>
        /// Grant a user the authorization to manipulate a given entity.
        /// </summary>
        /// <param name="entity">The entity the user has permission to use.</param>
        /// <param name="name">The name of the user.</param>
        /// <param name="id">The ID of the user.</param>
        protected virtual void GrantAuthorization(UUID entity, string name, UUID id) {
        }

        /// <summary>
        /// Hook to allow groups to be implemented. Override this to group users together.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        protected virtual UUID GetOwnershipID(UUID id) {
            return id;
        }

        public static Color GetColour(int i) {
            switch (i % 11) {
                case 1:
                    return Color.Red;
                case 2:
                    return Color.Orange;
                case 3:
                    return Color.Yellow;
                case 4:
                    return Color.Green;
                case 5:
                    return Color.Blue;
                case 6:
                    return Color.Indigo;
                case 7:
                    return Color.Violet;
                case 8:
                    return Color.Black;
                case 9:
                    return Color.Violet;
                case 10:
                    return Color.Indigo;
                case 11:
                    return Color.Blue;
                case 12:
                    return Color.Green;
                case 13:
                    return Color.Yellow;
                case 14:
                    return Color.Orange;
                case 15:
                    return Color.Red;
                case 16:
                    return Color.Black;
            }

            return Color.Orange;
        }

        #endregion
    }
}