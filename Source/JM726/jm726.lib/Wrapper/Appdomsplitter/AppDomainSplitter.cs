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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Policy;
using System.Reflection;
using common.Queue;
using common;
using System.Threading;
using jm726.lib.wrapper.spy;
using JM726.Lib.Static;

namespace Diagrams {
    public static class AppDomainSplitter {
        /// <summary>
        /// TODO use spy to check on calls made to proxy. If adding or removing listener use listener manager instead.
        /// 
        /// Create an instance of the given class which will be instantiated in a new AppDomain but the parameters passed into its constructor will remain in this application domain and will be accessible from the new application domain.
        /// </summary>
        /// <param name="domainName">The name of the new AppDomain.</param>
        //public static TToSplit Build<TToSplit>(AppDomain appDom, IAsynchQueueFactory queueFactory, params object[] constructorParams) where TToSplit : class {
        //    return Build<TToSplit> (appDom, typeof(TToSplit), queueFactory, constructorParams);
        //}
        /// <summary>
        /// Create an instance of the given class which will be instantiated in a new AppDomain but the parameters passed into its constructor will remain in this application domain and will be accessible from the new application domain.
        /// 
        /// Will check to see that there is a constructor for TToSplitType which has a signature matching the objects passed in as constructorParams
        /// </summary>
        /// <param name="domainName">The name of the new AppDomain.</param>
        public static TToSplit Build<TToSplit>(AppDomain appDom, Type toSplitType, IAsynchQueueFactory queueFactory, params object[] constructorParams) where TToSplit : class {            
            if (toSplitType.IsInterface || toSplitType.IsAbstract)
                throw new ArgumentException(toSplitType.Name + " is an " + (toSplitType.IsInterface ? "interface" : "abstract class") + ". The type to split must be a class that can be instantiated.");

            return new Splitter<TToSplit>(appDom, toSplitType, queueFactory, constructorParams).Instance;
        }

        public static void Stop() {
            _cont = false;
        }

        /// <summary>
        /// Whether the check threads should keep going.
        /// </summary>
        private static bool _cont;

        private class Splitter<TToSplit> where TToSplit : class {
            /// <summary>
            /// The instance which has been created and split.
            /// </summary>
            internal TToSplit Instance { get; set; }
            /// <summary>
            /// All parameters which have been passed in to Instance and therefore need to be tracked.
            /// </summary>
            private Dictionary<int, object> _parameters;
            /// <summary>
            /// The proxy object which can interact with the object in the child application domain.
            /// </summary>
            private SplitterChild<TToSplit> _childProxy;
            /// <summary>
            /// Any listeners that have been registered for given events. Mapped to the name of the event they are registered for.
            /// </summary>
            private Dictionary<string, List<MethodInfo>> _listeners;

            /// <summary>
            /// Create a splitter which will create an instance of TToSplitType in appDomain creating it with parameters which are proxies to the objects in constructorParams.
            /// Will also start a thread to check for method calls to the parameter proxies and relay them back to the actual parameter objects in the parent application domain.
            /// TODO Add more constructors for the various arguments CreateInstanceAndUnwrap can have.
            /// </summary>
            /// <param name="appDom">The application domain to create the new object in.</param>
            /// <param name="queueFactory">The queue factory which will be used to do any asynchronous processing in the new application domain. 
            /// It will be used to process method calls from the proxy in this application domain to the instance in the child application domain.</param>
            /// <param name="toSplitType">The type of object to be instantiated in the new application domain.</param>
            /// <param name="constructorParams">Any parameters for the constructor of TToSplitType.</param>
            internal Splitter(AppDomain appDom, Type toSplitType, IAsynchQueueFactory queueFactory, object[] constructorParams) {
                _parameters = new Dictionary<int, object>();

                Type helperType = typeof(SplitterChild<TToSplit>);//BuildRemoteType(toSplitType, constructorParams);
                object[] args = BuildArgs(toSplitType, queueFactory, constructorParams);

                _childProxy = appDom.CreateInstanceAndUnwrap(
                    helperType.Assembly.FullName, helperType.FullName, false,
                    BindingFlags.CreateInstance, null, args, null, null, null)
                    as SplitterChild<TToSplit>;

                ISpy<TToSplit> spy = new Spy<TToSplit>(_childProxy.Instance);
                Instance = spy.Instance;

                spy.OnEventAdd += (source, evt, listener) => AddListener(evt.Name, listener.Method);
                spy.OnEventRemove += (source, evt, listener) => RemoveListener(evt.Name, listener.Method);
                spy.OnMethodEvent += (source, method, ret, parameters) => AddParamEventListeners(parameters);

                _cont = true;
                Thread thread = new Thread(CheckThread);
                thread.Name = toSplitType.Name + "CheckThread";
                thread.Start();
            }

            /// <summary>
            /// Build the array of arguments that will be passed in to the constructor of the dynamically created class in the application domain.
            /// 
            /// The arguments are:
            /// 0: The type object for the object being split
            /// 1: An IAsynchQueueFactory used for decoupling where necessary
            /// 3: An array of all the types of the parameters passed in
            /// </summary>
            /// <param name="toSplitType">The type object for the object being split.</param>
            /// <param name="queueFactory">An IAsynchQueueFactory used for decoupling where necessary.</param>
            /// <param name="constructorParams">An array of all the types of the parameters passed in.</param>
            /// <returns>Any array of objects which can be passed in to the invoke method when creating the root in the child application domain.</returns>
            private object[] BuildArgs(Type toSplitType, IAsynchQueueFactory queueFactory, object[] constructorParams) {
                List<object> args = new List<object>();
                args.Add(toSplitType);
                args.Add(queueFactory);
                args.Add(constructorParams);
                AddParamEventListeners(constructorParams);
                return args.ToArray();
            }

            /// <summary>
            /// Thread which regularly checks to see if _proxy.Helper.HasCall is true.
            /// If it is it uses NextCall to get any queued method calls and then executes them on _parameters[className].
            /// Also checks to see if any events have been triggered by the proxy and if they have triggers the relevant listener.
            /// </summary>
            private void CheckThread() {
                while (_cont) {
                    if (_childProxy.HasEvent) {
                        Pair<string, object[]> evt = _childProxy.NextEvent;
                        TriggerListener(evt.A, evt.B);
                    }
                    if (_childProxy.HasCall) {
                        Pair<Pair<int, string>, Pair<int, object[]>> evt = _childProxy.NextCall;
                        object parameter = _parameters[evt.A.A];
                        MethodInfo method = parameter.GetType().GetMethod(evt.A.B, GetTypes(evt.B.B));
                        try {
                            if (method.ReturnType.Equals(typeof(void))) {
                                method.Invoke(parameter, evt.B.B);
                                _childProxy.NotifyMethodReturn(evt.B.A, null);
                            } else
                                _childProxy.NotifyMethodReturn(evt.B.A, method.Invoke(parameter, evt.B.B));
                        } catch (Exception e) {
                            _childProxy.NotifyMethodReturn(evt.B.A, e);
                        }
                    }
                    Util.Wait(50, _cont);
                }
            }

            /// <summary>
            /// Get an array of type objects corresponding to the types of the objects passed in as a parameter.
            /// 
            /// Useful for making reflecting method calls to overloaded methods.
            /// </summary>
            /// <param name="parameters">The objects to get the types for.</param>
            /// <returns>The type array.</returns>
            private Type[] GetTypes(object[] parameters) {
                Type[] types = new Type[parameters.Length];
                for (int i = 0; i < parameters.Length; i++) {
                    types[i] = parameters[i].GetType();
                }
                return types;
            }

            /// <summary>
            /// Add Listeners to parameter objects which will be passed in to the root object in the child application domain.
            /// </summary>
            /// <param name="parameters">The parameters to add listeners to.</param>
            private void AddParamEventListeners(object[] parameters) {
                int[] paramIDs = _childProxy == null ? new int[parameters.Length] : _childProxy.ParamIDs;
                for (int i = 0; i < parameters.Length; i++) {
                    paramIDs[i] = i;
                    new Spy<object>(parameters[i]).OnEventTriggered += (source, evt, eventParams) => {
                        _childProxy.NotifyEventTriggered(paramIDs[i], evt.Name, eventParams);
                    };
                    _parameters.Add(paramIDs[i], parameters[i]);
                }
            }
            /// <summary>
            /// Add a listener for events triggered from the root object in the child application domain.
            /// </summary>
            /// <param name="eventName">The event to listen for.</param>
            /// <param name="listener">The method to be called when the event is triggered.</param>
            private void AddListener(string eventName, MethodInfo listener) {
                if (!_listeners.ContainsKey(eventName))
                    _listeners.Add(eventName, new List<MethodInfo>());

                _listeners[eventName].Add(listener);
            }
            /// <summary>
            /// Remove a listener for events triggered from the root object in the child application domain.
            /// </summary>
            /// <param name="eventName">The event to listen for.</param>
            /// <param name="listener">The method to be called when the event is triggered.</param>
            private void RemoveListener(string eventName, MethodInfo listener) {
                if (_listeners.ContainsKey(eventName))
                    _listeners[eventName].Remove(listener);
            }
            /// <summary>
            /// Called when the root object in the child application domain triggers an event.
            /// This method will call all listeners which have been registered on the event.
            /// </summary>
            /// <param name="eventName">The event being triggered.</param>
            /// <param name="parameters">The parameters for the event.</param>
            private void TriggerListener(string eventName, object[] parameters) {
                if (_listeners.ContainsKey(eventName))
                    foreach (MethodInfo method in _listeners[eventName])
                        method.Invoke(_childProxy.Instance, parameters);
            }
        }
    }
}
