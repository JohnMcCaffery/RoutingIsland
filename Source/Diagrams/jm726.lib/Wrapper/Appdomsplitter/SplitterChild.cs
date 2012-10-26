using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using common;
using System.Reflection;
using common.Queue;
using jm726.lib.wrapper;

namespace Diagrams {
    /// <summary>
    /// Child class which will be instantiated in the child application domain. Handles most of the marshalling and wrapping.
    /// </summary>
    /// <typeparam name="TToSplit">The tyoe if the root object which is initialised in the child application domain.</typeparam>
    internal class SplitterChild<TToSplit> : Wrapper<TToSplit> where TToSplit : class {

        /// <summary>
        /// A count of how many method calls have been made. This count is used as the unique ID for each method call with a return value in either direction.
        /// </summary>
        private static int IDCount;
        /// <summary>
        /// The queue of method calls which have been made from this application domain to parameter proxies.
        /// Items rom this queue are processed by the parent application domain on the original objects.
        /// 
        /// A.A = name of the class which made the call
        /// A.B = name of the method which was called
        /// B.A = the ID of the method call
        /// B.B = any parameters passed in to the method call
        /// </summary>
        private Queue<Pair<Pair<int, string>, Pair<int, object[]>>> _parameterCallQ;
        /// <summary>
        /// A make of all the values returned for method calls to methods which return values.
        /// If an exception is thrown whilst calling a method the exception should be put into_returnValues instead.
        /// </summary>
        private static Dictionary<int, object> _returnValues;
        /// <summary>
        /// The type of the class being split
        /// </summary>
        private Type _toSplitType;

        /// <summary>
        /// Asynchronous queue used to process method calls made from the proxy object in the parent application domain to the instance in this (the child) application domain.
        /// Each method call needs to be processed asynchronously in a separate thread so that it happens fully within the child application domain.
        /// </summary>
        private IAsynchQueue _methodProcessingQueue;

        /// <summary>
        /// The IDs of the most recently created parameter objects
        /// </summary>
        private int[] _mostRecentIDs = null;
        /// <summary>
        /// Mapping of ids to parameter objects
        /// </summary>
        private Dictionary<int, object> _parameters;

        /// <summary>
        /// Queue of all the events that have been triggered on the wrapped object
        /// </summary>
        private Queue<Pair<string, object[]>> _events;
        /// <summary>
        /// Any listeners that have been registered for given events.
        /// </summary>
        private Dictionary<int, Dictionary<string, List<MethodInfo>>> _listeners;


        /// <summary>
        /// Find out if there is a method call to process.
        /// </summary>
        public bool HasCall {
            get { return _parameterCallQ.Count > 0; }
        }

        /// <summary>
        /// Get the next method call to process. The first two strings are the class and method. The int is the ID of the method call. Used for identifying the call when returning a value. The objects are the parameters.
        /// </summary>
        public Pair<Pair<int, string>, Pair<int, object[]>> NextCall {
            get { return _parameterCallQ.Dequeue(); }
        }

        /// <summary>
        /// Flag for when an event has been generated so that the code in the parent AppDomain can process it
        /// </summary>
        public bool HasEvent { get { return _events.Count > 0; } }
        /// <summary>
        /// The next event that needs to be processed by the parent app domain
        /// </summary>
        public Pair<string, object[]> NextEvent { get { return _events.Dequeue(); } }

        /// <summary>
        /// The IDs of the parameters passed in to the last method call.
        /// </summary>
        public int[] ParamIDs { get {
            //If _mostRecentIDs has not been initialised then still in constructor which means
            //A: New app domain so _idCount is reset to 0
            //B: The constructor params will be ids 0 -> params.lengh
            //TODO What happens if another method call from a different threat sets new parameter IDs before the parent application domain has a chance to get the IDs of the previous method call?
            if (_mostRecentIDs == null) {
                ///The maxium number of parameters the system will allow for a constructor
                int max = 500;
                int[] defaultIDs = new int[max];
                for (int i = 0; i < max; i++)
                    defaultIDs[i] = i;
                return defaultIDs;
            }
            return _mostRecentIDs; }
        }

        #region Constructor

        /// <summary>
        /// The constructor for a helper. Takes the type of object being split and an array of the types of the arguments for the constructor.
        /// 
        /// Creates proxies for each of the parameter types then invokes the constructor passing in these proxy objects and passes the result up to the
        /// base class to be wrapped.
        /// 
        /// The result is an object where any method calls made on the object will cause a listener method to be called and any methods called
        /// on the parameters passed in to the constructor will also trigger listeners.
        /// </summary>
        /// <param name="toSplitType">The type of object to be split.</param>
        /// <param name="constructorParameters">Any parameters the constructor for toSplitType takes.</param>
        public SplitterChild(Type toSplitType, IAsynchQueueFactory queueFactory, object[] constructorParameters)
            : base(buildInstance<TToSplit>(toSplitType, queueFactory, constructorParameters), "Splitter") {
            _parameterCallQ = new Queue<Pair<Pair<int, string>, Pair<int, object[]>>>();
            _methodProcessingQueue = queueFactory.MakeQueue();

            ListenToParams(constructorParameters);
        }

        private static TToBuild buildInstance<TToBuild>(Type toSplitType, IAsynchQueueFactory queueFactory, params object[] constructorParameters) where TToBuild : class {
            return Activator.CreateInstance(toSplitType, ReplaceParameters(constructorParameters)) as TToBuild;
        }

        #endregion

        #region Method Calls

        #region Local Calls (calls made from this application domain to be processed in the parent application domain)

        private void QueueLocalCall(int param, string methodName, int id, object[] parameters) {
            _parameterCallQ.Enqueue(new Pair<Pair<int, string>, Pair<int, object[]>>(
            new Pair<int, string>(param, methodName),
            new Pair<int, object[]>(id, parameters)));
        }

        public void NotifyMethodReturn(int methodCall, object value) {
            _returnValues.Add(methodCall, value);
        }

        #endregion

        #region Proxy Calls (Calls made from the parent application domain which are to be processed in this application domain)

        public override void ReportMethodCallVoid(string methodName, object[] parameters) {
            WrappedInstance.GetType().GetMethod(methodName, GetTypes(parameters)).Invoke(WrappedInstance, parameters);
        }

        public override object ReportMethodCallReturn(string methodName, object[] parameters) {
            return WrappedInstance.GetType().GetMethod(methodName, GetTypes(parameters)).Invoke(WrappedInstance, parameters);
        }

        private void ProcessRemoteCall(string methodName, int id, object[] parameters) {
            _methodProcessingQueue.QWork("Call " + methodName, () => {
                MethodInfo method = WrappedInstance.GetType().GetMethod(methodName, GetTypes(parameters));
                if (method.ReturnParameter.Equals(typeof(void)))
                    method.Invoke(WrappedInstance, parameters);
                else
                    _returnValues.Add(id, method.Invoke(WrappedInstance, parameters));
            }); 
        }

        #endregion

        #region Public Parameter Members

        /// <summary>
        /// Take an array of parameters passed as objects to a method call and wrap each object in a ParamSplitter.
        /// The values in the original array are replaced with param splitter wrappers and an array of the instances
        /// wrapped by the param splitters is returned.
        /// Any method calls made to the objects in the returned aray will trigger events on the objects in the original array.
        /// </summary>
        /// <param name="parameters">The original array of constructor parameters to replace. After the method call this will be an array of ParamSplitters.</param>
        /// <returns>An array of objects which wrap and listen to method calls on the original parameters.</returns>
        private static object[] ReplaceParameters(object[] parameters) {
            object[] args = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++) {
                ParamSplitter param = new ParamSplitter(parameters[i]); ;
                parameters[i] = param;
                args[i] = param.Instance;
            }

            return args;
        }

        /// <summary>
        /// Take an array of ParamSplitter objects and add listeners 
        /// </summary>
        /// <param name="parameters"></param>
        private void ListenToParams(object[] parameters) {
            _mostRecentIDs = new int[parameters.Length];
            for (int i = 0; i < parameters.Length; i++) {
                ParamSplitter param = parameters[i] as ParamSplitter;
                if (param == null)
                    throw new Exception("Parameters must be wrapped in ParamSplitters to be listened to");
                _mostRecentIDs[i] = param.ID;
                param.OnMethodCall += (paramID, name, callID, @params) => {
                    MethodInfo method = _toSplitType.GetMethod(name, GetTypes(@params));
                    if (method.IsSpecialName && method.Name.StartsWith("add_"))
                        AddListener(param.ID, name, (@params[0] as MulticastDelegate).Method);
                    else if (method.IsSpecialName && method.Name.StartsWith("remove_"))
                        RemoveListener(param.ID, name, (@params[0] as MulticastDelegate).Method);
                    else
                        QueueLocalCall(param.ID, name, callID, @params);
                };
            }
        }

        #region Parameter Creation

        private delegate void ReportMethodCallDelegate(int paramID, string methodName, int callID, object[] parameters);

        private class ParamSplitter : Wrapper<object> {
            public int ID { get { return _id; } }
            public event ReportMethodCallDelegate OnMethodCall;

            private int _id;

            public ParamSplitter(object param)
                : base(param) {
                _id = IDCount++;
            }

            public override void ReportMethodCallVoid(string methodName, object[] parameters) {
                MakeCall(this, methodName, parameters);
            }

            public override object ReportMethodCallReturn(string methodName, object[] parameters) {
                return MakeCall(this, methodName, parameters);
            }

            private object MakeCall(object source, string methodName, object[] parameters) {
                int callID = IDCount++;

                OnMethodCall(_id, methodName, callID, parameters);

                while (!_returnValues.ContainsKey(callID))
                    Util.Wait(target: _returnValues);
                object ret = _returnValues[callID];
                if (ret is Exception)
                    throw ret as Exception;
                return ret;
            }

            public override void ReportEventTriggered(string eventName, object[] parameters) {
                //throw new NotImplementedException();
            }
        }

        #endregion

        #endregion

        #region Listener Members

        #region Local Events (Triggered from this App Dom)

        public override void ReportEventTriggered(string eventName, object[] parameters) {
            _events.Enqueue(new Pair<string, object[]>(eventName, parameters));
        }

        #endregion

        #region Proxy Events (Listened for in this App Dom)

        #endregion

        /// <summary>
        /// Used by the parent application domain to notify a proxy in this application domain that an event has been triggered.
        /// </summary>
        /// <param name="param">The parameter to notify.</param>
        /// <param name="eventName">The name of the event that was triggered.</param>
        /// <param name="parameters">The parameters that the event was triggered with.</param>
        public void NotifyEventTriggered(int param, string eventName, object[] parameters) {
            if (_listeners.ContainsKey(param) && _listeners[param].ContainsKey(eventName))
                foreach (MethodInfo method in _listeners[param][eventName])
                    method.Invoke(_parameters[param], parameters);
        }

        private void AddListener(int param, string eventName, MethodInfo listener) {
            if (!_listeners.ContainsKey(param))
                _listeners.Add(param, new Dictionary<string, List<MethodInfo>>());
            if (!_listeners[param].ContainsKey(eventName))
                _listeners[param].Add(eventName, new List<MethodInfo>());

            _listeners[param][eventName].Add(listener);

            if (!_parameters.ContainsKey(param))
                _parameters.Add(param, param);
        }

        private void RemoveListener(int param, string eventName, MethodInfo listener) {
            if (_listeners.ContainsKey(param) && _listeners[param].ContainsKey(eventName))
                _listeners[param][eventName].Remove(listener);

        }

        #endregion

        #endregion
    }
}
