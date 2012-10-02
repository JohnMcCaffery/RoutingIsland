using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using log4net;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Text;
using System.IO;
using common;
using jm726.lib.wrapper;
using jm726.lib.wrapper.logger;

namespace jm726.lib.wrapper.logger {

    /// <summary>
    /// Class which uses the ISpy interface to record all method calls to a given object. These are recorded to Xml.
    /// Can also play back recordings
    /// 
    /// ONLY PUBLIC PROPERTIES WITH GETTERS _AND_ SETTERS WILL BE LOGGED CORRECTLY
    /// </summary>
    /// <typeparam name="TToLog">The interface which is being logged.</typeparam>
    public class XmlLogWriter<TToLog> : Wrapper<TToLog>, IXmlLogWriter<TToLog> where TToLog : class {
        #region Private Fields
        /// <summary>
        /// The log4net logger object used to log information to the console or log events.
        /// </summary>
        private readonly ILog _logger;
        /// <summary>
        /// The name of this logger. This is the name of the type being logged with '_Logger' added onto the end.
        /// </summary>
        private readonly string _name;

        /// <summary>
        /// The XML document which stores the event logging document as it is being built.
        /// </summary>
        private XmlDocument _doc;
        /// <summary>
        /// The root XML node to which all events are to be appended as children.
        /// </summary>
        private XmlNode _root;
        /// <summary>
        /// The XML document being built up to log all the events happening to the wrapped instance of TToLog.
        /// </summary>
        private DateTime _lastEvent;

        /// <summary>
        /// Wether or not events are to be logged currently.
        /// </summary>
        private bool _logging;

        /// <summary>
        /// Whether or not to serialize the return value of the method.
        /// </summary>
        private bool _recordForPlayback;

        #endregion

        #region XmlLogWriter Properties

        /// <inheritdoc />
        public XmlDocument Log {
            get { return _doc; }
        }

        #endregion

        #region Constructor
        
        /// <summary>
        /// Constructor which creates a generic logger. Instantiates a series of helper fields and stores the TToLog instance which is to be wrapped by this logger.
        /// </summary>
        /// <param name="spy">The instance of TToLog which this logger will log calls to.</param>
        public XmlLogWriter(TToLog instance, bool recordForPlayback = false, bool recursive = true)
            : base(instance, "XmlLogWriter", recursive) {
            _lastEvent = default(DateTime);

            _recordForPlayback = recordForPlayback;
            _name = typeof(TToLog).FullName + "_Logger";
            _logger = LogManager.GetLogger(_name);

            _doc = new XmlDocument();
            XmlNode declaration = _doc.CreateXmlDeclaration("1.0", "utf-8", "yes");
            _doc.AppendChild(declaration);
        }

        #endregion

        #region Override Abstract Methods from Wrapper

        public override void ReportMethodCallVoid(string methodName, object[] parameters) {
            if (!_logging) {
                CallMethod(methodName, parameters);
                return;
            }

            MethodCall m = new MethodCall(GetMethod(methodName, parameters), WrappedInstance, parameters);

            if (_recordForPlayback && !m.Type.Equals("Method") && !m.Type.Equals("PropertySet"))
                return;

            XmlNode callNode = m.Serialize(_doc, SwitchArgument);
            callNode.Attributes.Append(GetTimeAttr());
            _root.AppendChild(callNode);
        }

        public override object ReportMethodCallReturn(string methodName, object[] parameters) {
            if (!_logging) 
                return CallMethod(methodName, parameters);

            MethodCall m = new MethodCall(GetMethod(methodName, parameters), WrappedInstance, parameters);

            if (_recordForPlayback && !m.Type.Equals("Method") && !m.Type.Equals("PropertySet"))
                return m.Return;

            XmlNode callNode = m.Serialize(_doc, SwitchArgument, _recordForPlayback);
            callNode.Attributes.Append(GetTimeAttr());
            _root.AppendChild(callNode);
            return m.Return;
        }

        public override void ReportEventTriggered(string eventName, object[] parameters) {
            //Do Nothing
        }

        /// <summary>
        /// Log the time at which a method call was made.
        /// </summary>
        private XmlAttribute GetTimeAttr() {
            XmlAttribute time = _doc.CreateAttribute("Time");
            if (_lastEvent.Equals(default(DateTime)))
                time.Value = "0";
            else
                time.Value = DateTime.Now.Subtract(_lastEvent).TotalMilliseconds + "";
            _lastEvent = DateTime.Now;

            return time;
        }

        #endregion

        #region XmlLogWriter Methods

        /// <inheritdoc />
        public void StartRecording() {
            if (_root != null)
                _doc.RemoveChild(_root);

            XmlNode root = _doc.CreateElement("Events");

            _doc.AppendChild(root);

            StartRecording(_doc);
        }
        public void StartRecording(XmlDocument doc) {
            XmlNodeList roots = doc.GetElementsByTagName("Events");
            if (roots.Count == 0)
                throw new ArgumentException("Unable to append events to supplied XML document. Document is not in the correct format");
            _doc = doc;
            _doc.NodeInserted += (source, args) => {
                if (args.NewParent.Name.Equals("Events"))
                    _lastEvent = DateTime.Now;
            };

            _root = roots[0];

            _lastEvent = default(DateTime);
            _logging = true;
        }

        /// <inheritdoc />
        public void StopRecording() {
            _logging = false;
        }

        /// <inheritdoc />
        public void PauseRecording() {
            _lastEvent = DateTime.Now;
            _logging = false;
        }

        /// <inheritdoc />
        public void RestartRecording() {
            _logging = false;
        }

        #endregion

        #region Util

        /// <summary>
        /// Used for extensibility.
        /// 
        /// Override this and check the type of the argument to define special behaviour for special types.
        /// Original defined so that instance specific IDs can be swapped to general but less unique names in the XML
        /// which can then be resolved back to new specific IDs when the sequence is played back.
        /// </summary>
        /// <param name="arg">The argument to swap for a different type.</param>
        /// <returns>The type to swap the argument to.</returns>
        protected virtual XmlNode SwitchArgument(ParameterInfo param, object arg) {
            return null;
        }

        #endregion
    }
}