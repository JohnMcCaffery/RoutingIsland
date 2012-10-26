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
using common.interfaces.entities;
using Diagrams.Common.interfaces.keytable;
using StAndrews.NeworkIsland.Framework.Util.Logger.Opensim;
using common.framework.interfaces.basic;
using jm726.lib.wrapper.logger;
using common.framework.interfaces.entities;
using System.IO;
using System.Xml;
using Diagrams.Common;
using Diagrams.Control.impl.Module;
using common.Queue;
using common;
using System.Drawing;
using common.framework.impl.util;
using Diagrams.Control.Impl.Controls.Buttons;

namespace Diagrams.Control.impl.Util {
    public class SequenceManager : FileWorker, ISequenceManager {
        public const string FOLDER_KEY = "SequenceFolder";

        #region Readonly Fields

        private readonly OpenSimLogReader _reader;

        private readonly Dictionary<string, UUID> _readerMap;

        private readonly IKeyTable<string> _writerMap;

        private readonly IKeyTable<IXmlLogWriter> _writers;

        private readonly IModule _control;

        private readonly IPrim _hostPrim;

        private readonly IControlUtil _controlUtil;

        private readonly bool _recordingEnabled;

        protected readonly string _sequenceFolder = "Sequences";

        private readonly IAsynchQueue _queue;

        #endregion

        #region Instance variables

        private IXmlLogWriter _baseWriter;

        private bool _currentlyRecording;

        private bool _timing;

        private bool _stopped;

        #endregion

        #region Properties 

        public IModule Control {
            get { return _control; }
        }

        public bool PlayingSequence {
            get { return _reader.HasNextEvent; }
        }

        public override string GetFolder(string name) {
            return Path.Combine(base.GetFolder(name), _sequenceFolder);
        }

        #endregion

        #region Constructor

        public SequenceManager(IModule control, IControlUtil controlUtil, IConfig controlConfig, IPrimFactory factory, IKeyTableFactory tableFactory, IAsynchQueue queue)
            : base(controlConfig) {

            _queue = queue;
            _controlUtil = controlUtil;
            _hostPrim = controlUtil.HostPrim;

            _readerMap = new Dictionary<string, UUID>();
            _writerMap = tableFactory.MakeKeyTable<string>();
            _recordingEnabled = controlConfig.GetBoolean("RecordingEnabled", false);
            _sequenceFolder = controlConfig.Get(FOLDER_KEY, ".");
            _timing = controlConfig.GetBoolean("TimedPlayback", true);

            _reader = new OpenSimLogReader(_readerMap, control, _hostPrim.Pos);
            _reader.MapInstance<IModule>(control);
            _writers = tableFactory.MakeKeyTable<IXmlLogWriter>();

            _control = Make<IModule>(new RecordControl(control), true);

        }

        public SequenceManager(IModule control, IControlUtil controlUtil, IConfig controlConfig, IPrimFactory primFactory, IKeyTableFactory tableFactory, IAsynchQueueFactory queueFactory)
            : this(control, controlUtil, controlConfig, primFactory, tableFactory, queueFactory.MakeQueue()) {
        }

        #endregion

        #region Setup

        public T Make<T>(T instance, bool recursive) where T : class {
            if (!_recordingEnabled) {
                _reader.MapInstance<T>(instance, !recursive);
                return instance;
            }
            
            IXmlLogWriter<T> writer = new OpenSimLogWriter<T>(_writerMap, instance, _hostPrim.Pos, true, recursive); ;
            if (_baseWriter == null)
                _baseWriter = writer;
            else 
                _writers.Add(UUID.Random(), writer);

            if (_currentlyRecording)
                writer.StartRecording();

            _reader.MapInstance<T>(writer.Instance, !recursive);
            return writer.Instance;
        }

        /// <summary>
        /// Map a string to an ID so it can be looked up when serializing and deserializing.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        public T MakeMapped<T>(T instance) where T : class, IEntity {
            _readerMap[instance.Name] = instance.ID;
            _reader.MapInstance<T>(instance);

            if (!_recordingEnabled) 
                return instance;

            IXmlLogWriter<T> writer = new OpenSimLogWriter<T>(_writerMap, instance, _hostPrim.Pos, true, true);
            _writers[instance.ID] = writer;
            _writerMap[instance.ID] = instance.Name;
            if (_currentlyRecording)
                writer.StartRecording(_baseWriter.Log);
            return writer.Instance;
        }

        public void UnMap(UUID id, string name) {
            if (_readerMap.ContainsKey(name))
                _readerMap.Remove(name);

            if (_recordingEnabled && _writers.ContainsKey(id)) {
                _writers[id].StopRecording();
                _writers.Remove(id);
                _writerMap.Remove(id);
            }
        }

        public void Stop() {
            StopPlayback();
            StopRecording();
        }

        #endregion

        #region Recording

        /// <summary>
        /// Start recording all user triggered events.
        /// </summary>
        public void StartRecording() {
            if (!_recordingEnabled) {
                _hostPrim.Say("Recording has beeen disabled in the configuration.");
                return;
            }

            if (_recordingEnabled) {
                _currentlyRecording = true;
                _baseWriter.StartRecording();
                foreach (IXmlLogWriter writer in _writers)
                    writer.StartRecording(_baseWriter.Log);
            }
        }

        /// <summary>
        /// Write the sequence of user triggered events to an XML events.
        /// </summary>
        /// <param name="events">The method of the events to write the sequence of events to.</param>
        /// <param name="topology">The method of the topology events to load in as the start point for the sequence.</param>
        public void SaveRecording(string name, string file, string topology = null) {
            if (!_recordingEnabled) {
                _hostPrim.Say("Recording has beeen disabled in the configuration.");
                return;
            }
            string shortFile = file;
            file = GetFile(GetFolder(name), file);
            if (file == null)
                return;

            if (topology != null) {
                XmlAttribute topologyAttr = _baseWriter.Log.CreateAttribute("Topology");
                topologyAttr.Value = topology;
                _baseWriter.Log.FirstChild.NextSibling.Attributes.Append(topologyAttr);
            }

            _baseWriter.Log.Save(file);
            _hostPrim.Say("Saved recording as '" + shortFile + "'");
        }

        /// <summary>
        /// Finish recording all user triggered events.
        /// </summary>
        public void StopRecording() {
            if (!_recordingEnabled || !_currentlyRecording)
                return;

            _currentlyRecording = false;
            _baseWriter.StopRecording();
            foreach (IXmlLogWriter writer in _writers)
                writer.StopRecording();
        }

        #endregion

        #region Playback

        /// <summary>
        /// Play back a previously recorded sequence of events.
        /// </summary>
        /// <param name="events">The events where the sequence of events is stored.</param>
        public void PlayRecording(string name, UUID id, string file) {
            string shortFile = file;
            file = GetFile(GetFolder(name), file);
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
                _controlUtil.Topology.LoadTopology(name, id, topologyAttr.Value);

            _hostPrim.Say("Starting playback of " + shortFile);
            _reader.PlayRecording(doc, true, _timing);
            PlayEvents();
        }

        public void PlayNextEvent() {
            if (_controlUtil.Paused)
                _reader.PlayNextEvent();
            else
                JM726.Lib.Static.Util.Wake(_reader);
        }

        public void StopPlayback() {
            _stopped = true;
            _reader.StopPlayback();
            JM726.Lib.Static.Util.Wake(_reader);
        }

        /// <summary>
        /// Work through any events in a loaded sequence that have not been played.
        /// If timing put them on the queue with their wait. Otherwise just play them immediately.
        /// </summary>
        private void PlayEvents() {
            _stopped = false;
            if (!_timing)
                while (!_stopped && !_controlUtil.Paused && _reader.HasNextEvent)
                    _reader.PlayNextEvent(false);
            else
                QueueNextEvent();
        }

        private void QueueNextEvent() {
            if (_stopped || !_reader.HasNextEvent || _controlUtil.Paused)
                return;
            _queue.QWork("Process sequenceEvent", () => {
                JM726.Lib.Static.Util.Wait(_reader.NextEventWait, !_stopped && _reader.NextEventWait > 0, _reader);
                if (!_stopped && !_controlUtil.Paused) {
                    _reader.PlayNextEvent(false);
                    QueueNextEvent();
                }
            });
        }

        #endregion

        private class RecordControl : IModule {
            private IModule _control;

            internal RecordControl(IModule control) {
                _control = control;
            }
            
            #region IModule Members

            public int Wait {
                get {
                    return _control.Wait;
                }
                set {
                    _control.Wait = value;
                }
            }

            public bool Paused {
                get {
                    return _control.Paused;
                }
                set {
                    _control.Paused = value;
                }
            }

            public INode AddNode(string name, Parameters parameters, Vector3 position = default(Vector3), Color colour = default(Color)) {
                return _control.AddNode(name, parameters, position, colour);
            }

            public ILink AddLink(UUID from, UUID to, Parameters parameters, float weight = default(float), bool bidirectional = true) {
                return _control.AddLink(from, to, parameters, weight, bidirectional);
            }

            public void RemoveLink(UUID link, Parameters parameters) {
                _control.RemoveLink(link, parameters);
            }

            public void RemoveLink(UUID from, UUID to, Parameters parameters) {
                _control.RemoveLink(from, to, parameters);
            }

            public void RemoveNode(UUID node, Parameters parameters) {
                _control.RemoveNode(node, parameters);
            }

            public void Stop() {
                _control.Stop();
            }

            public void Clear() {
                _control.Clear();
            }

            public void Clear(params UUID[] nodes) {
                _control.Clear(nodes);
            }

            #endregion
        }

        #region IModule Members

        public int Wait {
            get {
                return _control.Wait;
            }
            set {
                _control.Wait = value;
            }
        }

        public bool Paused {
            get {
                return _control.Paused;
            }
            set {
                _control.Paused = value;
                if (!value)
                    QueueNextEvent();
            }
        }

        public INode AddNode(string name, Parameters parameters, Vector3 position = default(Vector3), Color colour = default(Color)) {
            return _control.AddNode(name, parameters, position, colour);
        }

        public ILink AddLink(UUID from, UUID to, Parameters parameters, float weight = default(float), bool bidirectional = true) {
            return _control.AddLink(from, to, parameters, weight, bidirectional);
        }

        public void RemoveLink(UUID link, Parameters parameters) {
            _control.RemoveLink(link, parameters);
        }

        public void RemoveLink(UUID from, UUID to, Parameters parameters) {
            _control.RemoveLink(from, to, parameters);
        }

        public void RemoveNode(UUID node, Parameters parameters) {
            _control.RemoveNode(node, parameters);
        }

        public void Clear() {
            _control.Clear();
        }

        public void Clear(params UUID[] nodes) {
            _control.Clear(nodes);
        }

        #endregion
    }
}
