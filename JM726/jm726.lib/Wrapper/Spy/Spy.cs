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
using System.Reflection;
using System.Xml;
using log4net;
using System.Reflection.Emit;
using jm726.lib.wrapper;
using jm726.lib.wrapper.spy;

namespace jm726.lib.wrapper.spy {

    /// <summary>
    /// Delegate for when a method call is made.
    /// </summary>
    /// <param name="source">The object that triggered the event.</param>
    /// <param name="method">The method of the method.</param>
    /// <param name="ret">The object that was returned. Null if the method returns void.</param>
    /// <param name="parameters">Any arguments passed to the method.</param>
    public delegate void MethodCallDelegate(object source, MethodInfo method, object ret, object[] parameters);

    /// <summary>
    /// Delegate for when a listener is added to or removed from an event.
    /// </summary>
    /// <param name="source">The object that triggered the event.</param>
    /// <param name="event">The event that is being listened for.</param>
    /// <param name="listener">The @value being added.</param>
    public delegate void EventModifyDelegate(object source, EventInfo @event, MulticastDelegate listener);

    /// <summary>
    /// Delegate for when an event is triggered in the spied upon object.
    /// </summary>
    /// <param name="source">The object that triggered the event.</param>
    /// <param name="eventName">The name of the event that was triggered.</param>
    /// <param name="parameters">The parameters that the event was triggered with.</param>
    public delegate void EventTriggeredDelegate(object source, EventInfo eventName, object[] parameters);

    /// <summary>
    /// Delegate for when a getter or setter is invoked on a property.
    /// </summary>
    /// <param name="source">The object that triggered the event.</param>
    /// <param name="property">The property being set.</param>
    /// <param name="value">The value set for the property.</param>
    /// <param name="@value">The @value being added.</param>
    public delegate void PropertyInteractDelegate(object source, PropertyInfo property, object @value);

    /// <summary>
    /// TODO IMPLEMENTED
    /// Implemententation of the ISpy interface.
    /// 
    /// Dynamically creates an instance of TToSpy which wraps a given instance and then logs all calls to the instance.
    /// 
    /// This class provides a series of events. Any time a method is called, an event has a listener added or removed or a property is got or set one of these events triggered. 
    /// Any object interested in tracking events that happen to an instance of TToSpy can use a spy object to track what is happening.
    /// </summary>
    /// <typeparam name="TToSpy">The interface which is being logged.</typeparam>
    public class Spy<TToSpy> : Wrapper<TToSpy>, ISpy<TToSpy> where TToSpy : class {
        #region Constructor

        /// <summary>
        /// Constructor which creates a generic logger. Instantiates a series of helper fields and stores the TToLog instance which is to be wrapped by this logger.
        /// </summary>
        /// <param name="instance">The instance of TToLog which this logger will log calls to.</param>
        /// <param name="recursive">Whether to spy recursively on all the parent interfaces of TToSpy or just the methods defined in TToSpy itself.</param>
        /// <param name="debug">If set to true any errors thrown from the reporting events will not be caught. Otherwise they will be caught and a warning printed to the logger.</param>
        public Spy(TToSpy instance, bool recursive = true, bool debug = true)
            : base(instance, "Spy", recursive) {
                
            _debugMode = debug;
        }

        #endregion
        
        #region Notify Methods
        
        /// <summary>
        /// Report on a method call that does not return a value.
        /// </summary>
        /// <param name="source">The object that triggered the event.</param>
        /// <param name="methodName">The name of the method being called.</param>
        /// <param name="parameters">Any arguments associated with the method call.</param>
        public override void ReportMethodCallVoid(string methodName, object[] parameters) {
            ReportMethodCall(methodName, parameters);
        }
        /// <summary>
        /// Report on a method call that returns a value.
        /// </summary>
        /// <param name="source">The object that triggered the event.</param>
        /// <param name="methodName">The name of the method being called.</param>
        /// <param name="parameters">Any arguments associated with the method call.</param>
        /// <returns>Whatever the method call returned.</returns>
        public override object ReportMethodCallReturn(string methodName, object[] parameters) {
            return ReportMethodCall(methodName, parameters);
        }

        private object ReportMethodCall(string methodName, object[] parameters) {
            MethodInfo method = GetMethod(methodName, parameters);
            object ret = CallMethod(method, parameters);
            ReportMethodCall(method, ret, parameters);
            return ret;
        }

        /// <summary>
        /// Report that an event in the wrapped class was triggered.
        /// </summary>
        /// <param name="source">The object that triggered the event.</param>
        /// <param name="eventName">The name of the event that was triggered.</param>
        /// <param name="parameters">The parameters that were passed to all listeners.</param>
        public override void ReportEventTriggered(string eventName, object[] parameters) {
            OnEventTriggered(WrappedInstance, GetEvent(eventName), parameters);
        }

        #endregion

        #region Util

        /// <summary>
        /// Report on a method call. If the method call is a special method (add/remove event or get/set property) triggers the appropriate event. Otherwise triggers the method call event.
        /// </summary>
        /// <param name="method">The method that was invoked.</param>
        /// <param name="ret">The value that the method returned.</param>
        /// <param name="parameters">Any parameters associated with the method call.</param>
        private void ReportMethodCall(MethodInfo method, object ret, object[] parameters) {
            if (method == null) return;

            //Trigger the general OnMethodEvent event.
            if (OnMethodEvent != null && !_debugMode) {
                try {
                    OnMethodEvent(WrappedInstance, method, ret, parameters);
                } catch (Exception e) {
                    Logger.Warn(Name + " had problem triggering report event for " + method.Name, e);
                }
            } else if (OnMethodEvent != null)
                OnMethodEvent(WrappedInstance, method, ret, parameters);


            //Triger the specific Property/Event/Method event
            if (_debugMode)
                ReportSpecific(method, ret, parameters);
            else {
                try {
                    ReportSpecific(method, ret, parameters);
                } catch (Exception e) {
                    Logger.Warn(Name + " had problem triggering report event for " + method.Name, e);
                }
            }
        }

        private void ReportSpecific(MethodInfo method, object ret, object[] parameters) {
                if (method.IsSpecialName) {
                    if (method.Name.StartsWith("add_") && OnEventAdd != null)
                        OnEventAdd(WrappedInstance, GetMember<EventInfo>(WrappedInterface, method.Name.Substring(4)), parameters[0] as MulticastDelegate);
                    else if (method.Name.StartsWith("remove_") && OnEventRemove != null)
                        OnEventRemove(WrappedInstance, GetMember<EventInfo>(WrappedInterface, method.Name.Substring(7)), parameters[0] as MulticastDelegate);
                    else if (method.Name.StartsWith("get_") && OnPropertyGet != null)
                        OnPropertyGet(WrappedInstance, GetMember<PropertyInfo>(WrappedInterface, method.Name.Substring(4)), ret);
                    else if (method.Name.StartsWith("set_") && OnPropertySet != null)
                        OnPropertySet(WrappedInstance, GetMember<PropertyInfo>(WrappedInterface, method.Name.Substring(4)), parameters[0]);
                } else if (OnMethodCall != null)
                    OnMethodCall(WrappedInstance, method, ret, parameters);
        }

        private bool _debugMode;

        private TMember GetMember<TMember>(Type intface, string name) where TMember : MemberInfo {
            TMember member = null;
            if (typeof(TMember).Name.Equals("PropertyInfo"))
                member = intface.GetProperty(name) as TMember;
            else if (typeof(TMember).Name.Equals("EventInfo"))
                member = intface.GetEvent(name) as TMember;

            if (member != null)
                return member;

            foreach (Type parentIntface in intface.GetInterfaces()) {
                member = GetMember<TMember>(parentIntface, name);
                if (member != null)
                    return member;
            }
            return null;                 
        }

        #endregion

        #region Implementation of ISpy<TToSpy>

        /// <inhertidoc />
        public event MethodCallDelegate OnMethodCall;
        /// <inhertidoc />
        public event MethodCallDelegate OnMethodEvent;
        /// <inhertidoc />
        public event EventModifyDelegate OnEventAdd;
        /// <inhertidoc />
        public event EventModifyDelegate OnEventRemove;
        /// <inhertidoc />
        public event EventTriggeredDelegate OnEventTriggered;
        /// <inhertidoc />
        public event PropertyInteractDelegate OnPropertySet;
        /// <inhertidoc />
        public event PropertyInteractDelegate OnPropertyGet;

        #endregion
    }
}