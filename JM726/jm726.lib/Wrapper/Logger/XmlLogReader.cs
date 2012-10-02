/*************************************************************************
Copyright (c) 2012 John McCaffery 

This file is part of JohnLib.

JohnLib is free software: you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

JohnLib is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License
along with JohnLib.  If not, see <http://www.gnu.org/licenses/>.

**************************************************************************/

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
using jm726.lib.Serialization;
using JM726.Lib.Static;

namespace jm726.lib.wrapper.logger {
    /// <summary>
    /// Class which uses the ISpy interface to record all method calls to a given object. These are recorded to Xml.
    /// Can also play back recordings
    /// ONLY PUBLIC PROPERTIES WITH GETTERS _AND_ SETTERS WILL BE LOGGED CORRECTLY
    /// </summary>
    /// <typeparam name="TToLog">The interface which is being logged.</typeparam>
    public class XmlLogReader {

        private static readonly HashSet<Assembly> Assemblies = new HashSet<Assembly>();

        #region Private Fields
        /// <summary>
        /// The type of a void object
        /// </summary>
        private readonly static Type _voidType = typeof(void);
        /// <summary>
        /// The log4net logger object used to log information to the console or log events.
        /// </summary>
        private readonly ILog _logger;
        /// <summary>
        /// The name of this logger. This is the name of the type being logged with '_Logger' added onto the end.
        /// </summary>
        private readonly string _name;

        /// <summary>
        /// Map of all the xml serializers that have been created
        /// </summary>
        private readonly Dictionary<Type, XmlSerializer> _serializers;
        /// <summary>
        /// Map of all extra types that are known to be needed to serialize a given type
        /// </summary>
        private readonly Dictionary<Type, IEnumerable<Type>> _inheritedTypes;
        
        /// <summary>
        /// Interface types mapped to their full name
        /// </summary>
        private readonly Dictionary<string, Type> _mappedInterfaces;
        /// <summary>
        /// Interface instances mapped to the interface they implement
        /// </summary>
        private readonly Dictionary<Type, object> _mappedInstances;
        /// <summary>
        /// Interface instances mapped to their hash value
        /// </summary>
        private readonly Dictionary<string, object> _hashedInstances;
        /// <summary>
        /// Which interfaces are mapped to which types.
        /// Used to check whether an instance mapped to a hash is valid to playback a given interface.
        /// </summary>
        private readonly Dictionary<string, HashSet<Type>> _hashedInterfaces;


        /// <summary>
        /// The current event being processed if stepping through an event sequence.
        /// </summary>
        private XmlNode _currentEvent;

        #endregion

        #region Constructor

        public XmlLogReader() : this(null) { }

        /// <summary>
        /// Constructor which creates a generic logger. Instantiates a series of helper fields and stores the TToLog instance which is to be wrapped by this logger.
        /// </summary>
        /// <param name="spy">The instance of TToLog which this logger will log calls to.</param>
        public XmlLogReader(object instance) {
            _serializers = new Dictionary<Type, XmlSerializer>();
            _inheritedTypes = new Dictionary<Type, IEnumerable<Type>>();

            _mappedInstances = new Dictionary<Type, object>();
            _mappedInterfaces = new Dictionary<string, Type>();
            _hashedInstances = new Dictionary<string, object>();
            _hashedInterfaces = new Dictionary<string, HashSet<Type>>();

            if (instance != null) {
                foreach (Type intface in instance.GetType().GetInterfaces())
                    MapInstance(intface, instance);
            }

            _logger = LogManager.GetLogger(GetType());
        }

        #endregion

        #region XmlLogReader Methods

        /// <summary>
        /// Map an instance to the interface it implements.
        /// </summary>
        /// <typeparam name="TToMapTo">The interface that instance specifies. If TToMap is not an interface then instance is mapped to all interfaces it implements.</typeparam>
        /// <param name="instance">The instance to map.</param>
        /// <param name="ignoreInheritance">Whether to map to parent interfaces of the interfaces directly implemented or just to the child interface.</param>
        public void MapInstance<TToMapTo>(TToMapTo instance, bool ignoreInheritance = false) {
            Type type = typeof(TToMapTo);

            if (type.IsInterface)
                MapInstance(type, instance, ignoreInheritance);
            else
                foreach (Type intface in instance.GetType().GetInterfaces())
                    MapInstance(intface, instance, ignoreInheritance);
        }

        /// <summary>
        /// Map an instance to the interface it implements.
        /// </summary>
        /// <param name="TToMapTo">The interface that instance specifies. If TToMap is not an interface then instance is mapped to all interfaces it implements.</param>
        /// <param name="instance">The instance to map.</param>
        /// <param name="ignoreInheritance">Whether to map to parent interfaces of the interfaces directly implemented or just to the child interface.</param>
        public void MapInstance(Type TToMapTo, object instance, bool ignoreInheritance = true) {
            //If the type specified is not an interface just map to all interfaces implemented by the type
            if (!TToMapTo.IsInterface) {
                foreach (Type intface in TToMapTo.GetInterfaces())
                    MapInstance(intface, instance, ignoreInheritance);
                return;
            }

            string hash = instance.GetHashCode().ToString();
            if (!_hashedInstances.ContainsKey(hash)) {
                _hashedInstances.Add(hash, instance);
                _hashedInterfaces.Add(hash, new HashSet<Type>());
            } else
                _hashedInstances[hash] = instance;
            _hashedInterfaces[hash].Add(TToMapTo);

            //Store the assemblies for the interface and the instance so they can be used for resolving types
            Assemblies.Add(TToMapTo.Assembly);
            Assemblies.Add(instance.GetType().Assembly);

            //Map the instance to the interface
            if (_mappedInstances.ContainsKey(TToMapTo))
                _mappedInstances[TToMapTo] = instance;
            else
                _mappedInstances.Add(TToMapTo, instance);

            //Map the interface to its name
            if (!_mappedInterfaces.ContainsKey(TToMapTo.FullName))
                _mappedInterfaces.Add(TToMapTo.FullName, TToMapTo);

            //If recursive map all parent interfaces of the interface
            if (!ignoreInheritance)
                foreach (Type intface in TToMapTo.GetInterfaces())
                    MapInstance(intface, instance, true);
        }

        #endregion

        #region PlayBack

        /// <summary>
        /// Play back the events that have been logged to a specified events.
        /// </summary>
        /// <param name="events">The events to be played back.</param>
        /// <param name="stepping">Whether the events should be played back in sequence or stepped through.</param>
        public void PlayRecording(XmlDocument events, bool stepping, bool timing) {
            //If there was already a sequence being stepped through cancel it.
            _currentEvent = null;

            XmlNodeList eventNodes = events.GetElementsByTagName("Events");

            if (stepping)
                _currentEvent = eventNodes[0].FirstChild;
            else
                foreach (XmlNode evt in eventNodes[0].ChildNodes)
                    if (!(evt is XmlComment))
                        PlayEvent(evt, timing);
        }

        public bool HasNextEvent {
            get {
                return _currentEvent != null;
            }
        }

        public int NextEventWait {
            get {
                if (!HasNextEvent)
                    return 0;
                XmlAttribute timeAttr = _currentEvent.Attributes["Time"];
                string timeStr = timeAttr != null ? timeAttr.Value : "0";
                double time = 0;
                double.TryParse(timeStr, out time);
                return (int) time;
            }
        }

        /// <summary>
        /// Cause the next event to be played.
        /// </summary>
        public void PlayNextEvent() {
            PlayNextEvent(false);
        }

        public void StopPlayback() {
            _currentEvent = null;
            Util.Wake(this);
        }

        /// <summary>
        /// Cause the next event to be played.
        /// </summary>
        public void PlayNextEvent(bool timing) {
            if (HasNextEvent)
                PlayEvent(_currentEvent, timing);
            do
                _currentEvent = _currentEvent.NextSibling;
            while (_currentEvent != null && _currentEvent is XmlComment);
        }

        private void PlayEvent(XmlNode evt, bool timing) {
            XmlAttribute timeAttr = evt.Attributes["Time"];
            string timeStr = timeAttr != null ? timeAttr.Value : "0";
            try {
                MethodCall call = new MethodCall(evt, SwitchArgumentBack);
                if (timing) {
                    double time = 0;
                    if (double.TryParse(timeStr, out time) && time > 0)
                        Util.Wait((int) time, target: this);
                }
                if (call.Type.Equals("Method") || call.Type.Equals("PropertySet"))
                    CallMethod(call);
            } catch (Exception e) {
                _logger.Warn("Unable to invoke event. " + e.Message/*, e*/);
            } 
        }

        #region Parse Parameters

        /// <summary>
        /// Used for extensibility.
        /// 
        /// Override this and check the type of the argument to define special behaviour for special types. 
        /// This will return the previously switched argument to the correct type.
        /// 
        /// Original defined so that instance specific IDs can be swapped to general but less unique names in the XML
        /// which can then be resolved back to new specific IDs when the sequence is played back.
        /// </summary>
        /// <param name="arg">The argument to swap for a different type.</param>
        /// <returns>The type to swap the argument to.</returns>
        protected virtual object SwitchArgumentBack(XmlNode paramNode, object arg) {
            return arg;
        }

        #endregion

        #region Call Method

        private object CallMethod(MethodCall call) {
            object ret = null;
            Pair<object, MethodInfo> callPair = GetCallPair(call);
            if (callPair.A != null && callPair.B != null) {
                try {
                    //_logger.Warn("Calling " + callPair.B.Name + " on " + callPair.A.ToString());
                    ret = callPair.B.Invoke(callPair.A, call.Parameters);
                } catch (Exception e) {
                    _logger.Warn(_name + " unable to invoke " + call.Name, e);
                }
            }
            return ret;
        }

        private Pair<object, MethodInfo> GetCallPair(MethodCall call) {
            Type interfaceType = _mappedInterfaces.ContainsKey(call.Interface) ? _mappedInterfaces[call.Interface] : ResolveType(call.Interface);
            if (interfaceType == null) {
                _logger.Warn("Unable to invoke " + call.Name + ". " + call.Interface + " could not be resolved to a valid interface. Have you mapped all the required assemblies?");
                return new Pair<object,MethodInfo>();
            }

            object instance = _hashedInstances.ContainsKey(call.Hash) ? _hashedInstances[call.Hash] : null;
            
            if (instance == null)
                instance = _mappedInstances.ContainsKey(interfaceType) ? _mappedInstances[interfaceType] : null;
            else if (!_hashedInterfaces[call.Hash].Contains(interfaceType)) {
                _logger.Warn("Unable to invoke " + call.Name + ". The instance mapped to hash " + call.Hash + " is not mapped to " + call.Interface + ".");
                return new Pair<object, MethodInfo>();
            }

            
            if (instance == null) {
                _logger.Warn("Unable to invoke " + call.Name + ". No instance was mapped to " + call.Interface + ". Have you mapped all the required instances?");
                return new Pair<object, MethodInfo>();
            } else if (!interfaceType.IsInstanceOfType(instance) && _mappedInstances.ContainsKey(interfaceType))
                instance = _mappedInstances[interfaceType];

            if (!interfaceType.IsInstanceOfType(instance)){
                _logger.Warn("Unable to invoke " + call.Name + ". The instance mapped to " + call.Interface + " with hash " + call.Hash + " does not implement " + interfaceType.Name + ".");
                return new Pair<object, MethodInfo>();
            } 

            MethodInfo method = interfaceType.GetMethod(call.FullName, Wrapper.GetTypes(call.Parameters));
            if (method == null) {
                _logger.Warn("Unable to invoke " + call.Interface + "." + call.Name + ". The given method signature could not be resolved on that interface.");
                return new Pair<object, MethodInfo>();
            }

            return new Pair<object, MethodInfo>(instance, method);
        }

        private Type ResolveType(string name) {
            Type t = Type.GetType(name);
            if (t != null)
                return t;

            foreach (Assembly assembly in Assemblies) {
                t = assembly.GetType(name);
                if (t != null)
                    return t;
            }
            return t;
        }

        #endregion

        #endregion
    }
}