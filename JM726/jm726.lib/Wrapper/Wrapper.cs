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
using System.Reflection.Emit;
using log4net;
using System.Reflection;
using System.ComponentModel;
using OpenMetaverse;
using common;
using System.Diagnostics;

namespace jm726.lib.wrapper {
    [Serializable]
    public class Wrapper {
        /// <summary>
        /// Get an array of all the types of the parameters being passed to a method. Used if the method name is overloaded so the signature is needed to get the MethodInfo object for the method.
        /// </summary>
        /// <param name="parameters">The parameters for the method.</param>
        /// <returns>An array of Type object.s</returns>
        public static Type[] GetTypes(params object[] parameters) {
            return parameters.Select(obj => obj.GetType()).ToArray();
        }
    }
    [Serializable]
    public abstract class Wrapper<TToWrap> : Wrapper, IWrapper<TToWrap> where TToWrap : class {
        /// <summary>
        /// The log4net logger object used to log information to the console or log events.
        /// </summary>
        protected ILog Logger { get { return _wrapperLogger; } }

        /// <summary>
        /// The type of object instance which is being wrapped.
        /// </summary>
        protected Type InstanceType { get { return _instanceType; } }

        /// <summary>
        /// The name of this wrapper. This is generally the name of the type being spied on with '_Wrap' added onto the end.
        /// </summary>
        protected string Name { get { return _wrapperType.Namespace + _name; } }
        /// <summary>
        /// The original instance which has been wrapped
        /// </summary>
        protected TToWrap WrappedInstance {
            get { return _wrappedInstance; }
        }

        #region Private Static fields
        /// <summary>
        /// Checker used to check whether an argument is of the right type to be a parameter for a given method.
        /// </summary>
        private static readonly ParamChecker ParamComparer = new ParamChecker();

        /// <summary>
        /// The Module used to create all the dynamic types that will implement the logging.
        /// </summary>
        private static readonly ModuleBuilder ModuleBuilder =
            AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName("DynamicProxyAssembly"),
                AssemblyBuilderAccess.RunAndSave)
                .DefineDynamicModule("DynamicProxyAssembly.dll");

        #endregion

        #region Private Fields
        /// <summary>
        /// Logger used by the proxy object to log events.
        /// </summary>
        private readonly ILog _wrapperLogger;
        /// <summary>
        /// Logger used by the proxy object to log events.
        /// </summary>
        private readonly ILog _proxyLogger;

        /// <summary>
        /// The type of the wrapper which is being created.
        /// </summary>
        private readonly Type _wrapperType;
        /// <summary>
        /// The type of the interface being wrapped.
        /// </summary>
        private readonly Type _wrappedInterface;
        /// <summary>
        /// The type of object instance which is being wrapped.
        /// </summary>
        private readonly Type _instanceType;

        /// <summary>
        /// The name of this wrapper. This is generally the name of the type being spied on with '_Wrap' added onto the end.
        /// </summary>
        private readonly string _name;

        /// <summary>
        /// The method which logs calls to void methods.
        /// </summary>
        private readonly MethodInfo _logMethodVoid;
        /// <summary>
        /// The method which logs calls to methods which return a value.
        /// </summary>
        private readonly MethodInfo _logMethodReturn;
        /// <summary>
        /// The method which logs when an event is triggered.
        /// </summary>
        private readonly MethodInfo _logMethodEvent;
        /// <summary>
        /// The method used to output logging text.
        /// </summary>
        private readonly MethodInfo _detailedLogMethod;
        
        /// <summary>
        /// All the methods that the proxy implements.
        /// </summary>
        private readonly Dictionary<string, MethodInfo> _methods;
        /// <summary>
        /// All the overloaded methods that the proxy implements.
        /// </summary>
        private readonly Dictionary<string, List<MethodInfo>> _overloadedMethods;
        /// <summary>
        /// All events the proxy listens for.
        /// </summary>
        private Dictionary<string, EventInfo> _events;

        /// <summary>
        /// The instance of TToLog being wrapped.
        /// </summary>
        private readonly TToWrap _wrappedInstance;
        /// <summary>
        /// The dynamically created instance of TToLog which tracks method calls
        /// </summary>
        private readonly TToWrap _wrappingInstance;

        /// <summary>
        /// The field in the generated class which stores a reference to the wrap object.
        /// </summary>
        private FieldInfo _wrapperField;
        /// <summary>
        /// The field in the generated class which stores a reference to the instance being wrapped.
        /// </summary>
        private FieldInfo _wrappedInstanceField;
        /// <summary>
        /// The field in the generated class which stores a reference to the logger object used for output.
        /// </summary>
        private FieldInfo _logField;

        /// <summary>
        /// Whether to listen for any events thrown from the wrapped instance
        /// </summary>
        private bool _listen;

        #endregion

        #region Wrapper Properties

        public bool Listen {
            get { return _listen; }
            set {
                if (_listen == value) return;
                _listen = value;
                RegisterListeners(Instance, WrappedInterface, !value);
            }
        }

        /// <inheritdoc />
        public TToWrap Instance {
            get { return _wrappingInstance; }
        }

        /// <summary>
        /// The type of the interface being wrapped.
        /// </summary>
        public Type WrappedInterface { get { return _wrappedInterface; } }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor which creates a generic logger. Instantiates a series of helper fields and stores the TToLog instance which is to be wrapped by this logger.
        /// </summary>
        /// <param name="instance">The instance to wrap.</param>
        /// <param name="nameSuffix">The suffix to append to the name. By default is 'Wrapper'.</param>
        /// <param name="recursive">Whether to wrap members from the full inheritance of interfaces TToWrap inherits from or just TToWrap's members.</param>
        /// <param name="wrapEvents">Whether to trigger a generic event whenever the wrapped instance triggers and event or not.</param>
        public Wrapper(TToWrap instance, string nameSuffix = "Wrapper", bool recursive = true, bool wrapEvents = false) {
            if (instance == null)
                throw new NullReferenceException("Unable to create wrapper. Instance cannot be null.");

            _wrapperType = this.GetType();
            _wrappedInterface = typeof(TToWrap);
            _instanceType = instance.GetType();

            if (!WrappedInterface.IsInterface)
                throw new Exception("Only interfaces can be wrapped. " + WrappedInterface.Name + " is not an interface");

            _name = WrappedInterface.FullName + "_" + _instanceType.FullName  + "_" + nameSuffix + (recursive ? "R" : "") + new Random().Next(Int16.MinValue, int.MaxValue);
            _listen = wrapEvents;

            _wrapperLogger = LogManager.GetLogger(typeof(Wrapper).Namespace + ".Wrapper." + _name);
            _proxyLogger = LogManager.GetLogger(typeof(Wrapper).Namespace + ".Proxy." + _name);

            _logMethodVoid = _wrapperType.GetMethod("ReportMethodCallVoid");
            _logMethodReturn = _wrapperType.GetMethod("ReportMethodCallReturn");
            _logMethodEvent = _wrapperType.GetMethod("ReportEventTriggered");
            _detailedLogMethod = Logger.GetType().GetMethod("Debug", new Type[] { typeof(string) });

            _methods = new Dictionary<string, MethodInfo>();
            _overloadedMethods = new Dictionary<string, List<MethodInfo>>();
            _events = new Dictionary<string, EventInfo>();

            _wrappedInstance = instance; 
            _wrappingInstance = BuildInstance(recursive);
        }

        #endregion

        #region Listeners

        /// <summary>
        /// Report on a method call that does not return a value.
        /// </summary>
        /// 
        /// <param name="methodName">The name of the method being called.</param>
        /// <param name="parameters">Any arguments associated with the method call.</param>
        public abstract void ReportMethodCallVoid(string methodName, object[] parameters);
        /// <summary>
        /// Report on a method call that returns a value.
        /// </summary>
        /// 
        /// <param name="methodName">The name of the method being called.</param>
        /// <param name="parameters">Any arguments associated with the method call.</param>
        /// <returns>Whatever the method call returned.</returns>
        public abstract object ReportMethodCallReturn(string methodName, object[] parameters);
        /// <summary>
        /// Report that an event in the wrapped class was triggered.
        /// </summary>
        /// 
        /// <param name="eventName">The name of the event that was triggered.</param>
        /// <param name="parameters">The parameters that were passed to all listeners.</param>
        public abstract void ReportEventTriggered(string eventName, object[] parameters);

        #endregion

        #region Util

        /// <summary>
        /// Call a method specifying the signature of the method to call.
        /// </summary>
        /// <param name="methodName">The name of the method.</param>
        /// <param name="parameters">The parameters to pass into the method.</param>
        /// <returns></returns>
        protected object CallMethod(string methodName, object[] parameters) {
            return CallMethod(GetMethod(methodName, parameters), parameters);
        }
        protected object CallMethod(MethodInfo wrappedMethod, object[] parameters) {
            if (wrappedMethod == null) {
                Logger.Debug(Name + " unable to invoke method. Supplied method was null.");
                return new ArgumentException(Name + " unable to invoke method. Supplied method was null.");
            }

            try {
                return wrappedMethod.Invoke(WrappedInstance, parameters);
            } catch (Exception e) {
                //Logger.Debug(Name + " unable to invoke " + wrappedMethod.DeclaringType.FullName + "." + wrappedMethod.Name, e);
                Console.Error.WriteLine("Wrapper line 261 - " + e.Message + "\n" + e.StackTrace + "\n");
                Exception inner = e.InnerException;
                while (inner != null) {
                    Console.Error.WriteLine("Inner Exception - " + inner.Message + "\n" + inner.StackTrace + "\n");
                    inner = inner.InnerException;
                }
                throw Activator.CreateInstance(e.InnerException.GetType(), e.InnerException.Message, e.InnerException) as Exception;
            }
        }

        protected MethodInfo GetMethod(string name, object[] parameters) {
            //If the method is not overloaded look it up
            if (_methods.ContainsKey(name))
                return _methods[name];

            //If the method is overloaded check for the method with corresponding parameters
            return _overloadedMethods[name].Find(method => 
                    method.GetParameters().Length == parameters.Length &&
                    method.GetParameters().SequenceEqual<object>(parameters, ParamComparer)
            );
        }

        protected EventInfo GetEvent(string name) {
            return _events[name];
        }

        private void RegisterListeners(TToWrap instance, Type intface, bool remove = false) {
            MethodInfo[] methods = intface.GetMethods();
            //Register the pre created listener method for each event the interface defines
            foreach (EventInfo e in intface.GetEvents())
                if (!remove)
                    e.AddEventHandler(_wrappedInstance, Delegate.CreateDelegate(e.EventHandlerType, instance, intface.FullName + "_" + e.Name));
                else
                    e.RemoveEventHandler(_wrappedInstance, Delegate.CreateDelegate(e.EventHandlerType, instance, intface.FullName + "_" + e.Name));

            //Recurse down to all parents of the interface
            foreach (Type parantInterface in intface.GetInterfaces())
                RegisterListeners(instance, parantInterface, remove);
        }

        private class ParamChecker : IEqualityComparer<object> {
            public new bool Equals(object x, object y) {
                if (!(x is ParameterInfo)) return false;

                return (x as ParameterInfo).ParameterType.Equals(y.GetType());
            }

            public int GetHashCode(object obj) {
                return obj is ParameterInfo ? (obj as ParameterInfo).ParameterType.GetHashCode() : obj.GetType().GetHashCode();
            }
        }

        #endregion

        #region Class Builder

        private static Type VOID_TYPE = typeof(void);
        private static Type OBJECT_TYPE = typeof(object);
        private static Type OBJECT_ARRAY_TYPE = typeof(object[]);

        /// <summary>
        /// Build an instance of a proxy class which will interact with the wrapper to give the desired behaviour.
        /// The instance will implement every interface the wrapped instance implements. It was also have every method the wrapped instance has.
        /// Any methods which are part of the interface being wrapped will call one of the notify methods. Every other method will forward its call
        /// directly to the wrapped instance.
        /// </summary>
        /// <param name="recursive">Whether to recursively implement parent interface members or just implement the members defined in the wrapped interface.</param>
        /// <returns>An object which implements the TToWrap interface. Any calls made on this object through that interface will get redirected to one of the notify methods.</returns>
        private TToWrap BuildInstance(bool recursive) {
            TToWrap instance = InstantiateExisting(recursive);
            if (instance != null)
                return instance;
            var proxyType = ModuleBuilder.DefineType(Name, TypeAttributes.Public | TypeAttributes.Class/*, _instanceType*/);

            CreateFields(proxyType);
            BuildConstructor(proxyType);
            List<MethodInfo> builtMethods = new List<MethodInfo>();
            HashSet<Type> implementedInterfaces = new HashSet<Type>();
            //Implement the interface to be wrapped, this method is recursive and will implement any parent interfaces as well
            LoadInterface(WrappedInterface, proxyType, recursive, builtMethods, implementedInterfaces);
            //Implement any methods not defined through the interface as forwarding methods.
            ForwardNonWrappedMembers(proxyType, builtMethods, implementedInterfaces);

            //Create the instance which has already been defined as a built type.
            instance = Activator.CreateInstance(proxyType.CreateType(), this, _proxyLogger, WrappedInstance) as TToWrap;
            //If listening for events triggered in the original instance register those listeners
            if (_listen)
                RegisterListeners(instance, WrappedInterface);

            return instance;
        }

        /// <summary>
        /// Any methods that are not in the wrapped interface still need to be able to function. These methods are just forwared straight on to the wrapped instance.
        /// </summary>
        /// <param name="proxyType">The type that is being built.</param>
        /// <param name="builtMethods">Methods that have been built so far.</param>
        /// <param name="implementedInterfaces">Interfaces which have been implemented.</param>
        private void ForwardNonWrappedMembers(TypeBuilder proxyType, List<MethodInfo> builtMethods, HashSet<Type> implementedInterfaces) {
            foreach (Type intface in _instanceType.GetInterfaces())
                ImplementNonWrappedInterfaces(proxyType, intface, builtMethods, implementedInterfaces);

            foreach (MethodInfo m in _instanceType.GetMethods().Except(builtMethods, new MethodComparer()))
                BuildForwardMethod(proxyType, m);
        }

        /// <summary>
        /// Recursively implement all interface members which are not being wrapped.
        /// </summary>
        /// <param name="proxyType">The type that is being built.</param>
        /// <param name="intface">The interface to implement currently.</param>
        /// <param name="builtMethods">All methods that have been built so far.</param>
        /// <param name="implementedInterfaces">All interfaces that have been implemented so far.</param>
        private void ImplementNonWrappedInterfaces(TypeBuilder proxyType, Type intface, List<MethodInfo> builtMethods,  HashSet<Type> implementedInterfaces) {
            InterfaceMapping map = _instanceType.GetInterfaceMap(intface);

            if (!implementedInterfaces.Contains(intface)) {
                proxyType.AddInterfaceImplementation(intface);
                implementedInterfaces.Add(intface);
                for (int i = 0; i < map.InterfaceMethods.Length; i++) {
                    BuildForwardMethod(proxyType, map.TargetMethods[i], map.InterfaceMethods[i]);
                    builtMethods.Add(map.TargetMethods[i]);
                }
            }

            foreach (Type parentInterface in intface.GetInterfaces())
                ImplementNonWrappedInterfaces(proxyType, parentInterface, builtMethods, implementedInterfaces);
        }

        /// <summary>
        /// Class which will compare two method objects to find out if they have the same name and signature and define equality as such.
        /// </summary>
        private class MethodComparer : IEqualityComparer<MethodInfo> {
            public bool Equals(MethodInfo x, MethodInfo y) {
                return GetHashCode(x) == GetHashCode(y);
            }

            public int GetHashCode(MethodInfo obj) {
                int hash = obj.Name.GetHashCode();
                foreach (ParameterInfo p in obj.GetParameters())
                    hash += p.ParameterType.GetHashCode();
                return hash;
            }
        }

        private TToWrap InstantiateExisting(bool recursive) {
            Type t = ModuleBuilder.GetType(Name);
            if (t == null)
                return null;
            LoadInterface(WrappedInterface, null, recursive, new List<MethodInfo>(), new HashSet<Type>());
            foreach (ConstructorInfo constructor in t.GetConstructors())
                LogMethodCreated("Found: " + constructor.Name, constructor.GetParameters());

            return Activator.CreateInstance(t, this, _proxyLogger, WrappedInstance) as TToWrap;
        }

        /// <summary>
        /// Create the fields the proxy will need.
        /// </summary>
        /// <param name="proxyType"></param>
        private void CreateFields(TypeBuilder proxyType) {
            _wrapperField = proxyType.DefineField("_wrapperObj", _wrapperType, FieldAttributes.Private);
            _wrappedInstanceField = proxyType.DefineField("_wrappedInstance", _instanceType, FieldAttributes.Private);
            _logField = proxyType.DefineField("_logger", typeof(ILog), FieldAttributes.Private);
        }

        private void LoadInterface(Type intface, TypeBuilder proxyType, bool recursive, List<MethodInfo> builtMethods, HashSet<Type> implementedInterfaces) {
            if (implementedInterfaces.Contains(intface))
                return;

            implementedInterfaces.Add(intface);
            //Implement all the methods defined in intface
            foreach (MethodInfo m in intface.GetMethods())
                if (proxyType != null) {
                    BuildMethod(proxyType, m, GetReportMethod(m));
                    builtMethods.Add(m);
                } else {
                    Logger.Debug("Storing " + intface.FullName + "_" + m.Name);
                    StoreMethod(intface.FullName + "_" + m.Name, m);
                }

            if (proxyType != null) 
                proxyType.AddInterfaceImplementation(intface);
            //Create listeners for every event defined in intface
            foreach (EventInfo e in intface.GetEvents()) {
                if (proxyType != null) {
                    MethodInfo listenerType = e.EventHandlerType.GetMethod("Invoke");
                    BuildMethod(proxyType, listenerType, _logMethodEvent, intface.FullName + "_" + e.Name);
                }
                _events.Add(intface.FullName + "_" + e.Name, e);
            }

            if (!recursive) return;

            //Implement all parent interfaces of this interface
            foreach (Type parantInterface in intface.GetInterfaces())
                LoadInterface(parantInterface, proxyType, true, builtMethods, implementedInterfaces);
        }

        private void StoreMethod(string name, MethodInfo method) {       
            if (!_methods.ContainsKey(name) && !_overloadedMethods.ContainsKey(name)) 
                _methods.Add(name, method);
            else if (_overloadedMethods.ContainsKey(name))
                _overloadedMethods[name].Add(method);
            else {
                _overloadedMethods.Add(name, new List<MethodInfo>());
                _overloadedMethods[name].Add(_methods[name]);
                _overloadedMethods[name].Add(method);

                _methods.Remove(name);
            }
        }

        private MethodInfo GetReportMethod(MethodInfo m) {
            if (m.ReturnType.Equals(VOID_TYPE))
                return _logMethodVoid;
            return _logMethodReturn;
        }

        #region Method body builders

        /// <summary>
        /// Build the constructor for an object which extends AbstractGenericLogger and implements XmlLogWriter for TToCreate. The constructor sets a private field _wrapper to be equal to the passed in wrapper (this).
        /// </summary>
        /// <param name="proxyType">The type builder which is building up the type which will implement XmlLogWriter for TToCreate.</param>
        private void BuildConstructor(TypeBuilder proxyType) {
            ConstructorBuilder constructor = proxyType.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { _wrapperType, typeof(ILog), _wrappedInterface });
            ILGenerator ctorIL = constructor.GetILGenerator();
            ctorIL.Emit(OpCodes.Ldarg_0);
            ctorIL.Emit(OpCodes.Ldarg_1);
            ctorIL.Emit(OpCodes.Stfld, _wrapperField);

            ctorIL.Emit(OpCodes.Ldarg_0);
            ctorIL.Emit(OpCodes.Ldarg_2);
            ctorIL.Emit(OpCodes.Stfld, _logField);

            ctorIL.Emit(OpCodes.Ldarg_0);
            ctorIL.Emit(OpCodes.Ldarg_3);
            ctorIL.Emit(OpCodes.Stfld, _wrappedInstanceField);

            ctorIL.Emit(OpCodes.Ret);
            //LogMethodCreated("Built: " + logger.Name, constructor.GetParameters());
        }

        /// <summary>
        /// Build a method defined in the interface TToCreate. The method will call the method call logging superclass method.
        /// <param name="proxyType">The Type being built.</param>
        /// <param name="copiedMethod">The method to be copied.</param>
        /// <param name="calledMethod">The method to call from the the IL code to notify the wrapper that an event has occurred.</param>
        /// <param name="name">The name to give the method. If null then the name is the concatenation of the declaring type of the copied method and the name of the method, seperated by an underscore.</param>
        /// <returns>Returns the method that was built.</returns>
        /// </summary>
        private MethodBuilder BuildMethod(TypeBuilder proxyType, MethodInfo copiedMethod, MethodInfo calledMethod, string name = null) {
            if (!copiedMethod.IsVirtual || !copiedMethod.IsPublic || copiedMethod.IsFinal) {
                Logger.Debug("Skipped " + copiedMethod.Name + " because it is " + (!copiedMethod.IsVirtual ? "not virtual" : !copiedMethod.IsPublic ? "not public" : "final"));
                return null;
            }

            Type[] argTypes = GetArgTypes(copiedMethod);
            int numArgs = argTypes.Length;
            bool implementingInterface = name == null;
            name = name != null ? name : copiedMethod.DeclaringType.FullName + "_" + copiedMethod.Name;
            MethodBuilder method = proxyType.DefineMethod(name, MethodAttributes.Virtual | MethodAttributes.Public, copiedMethod.ReturnType, argTypes);

            // Generate IL for 'GetGreeting' method.
            ILGenerator methodIL = method.GetILGenerator();
            LogFromIL(methodIL, "");
            LogFromIL(methodIL, "Running " + name);
            LocalBuilder array = methodIL.DeclareLocal(OBJECT_ARRAY_TYPE);
            LogFromIL(methodIL, "Specified loc_" + array.LocalIndex + " as an array of objects");

            ////Push the size of the array onto the stack
            methodIL.Emit(OpCodes.Ldc_I4, numArgs);
            ////Create the array, a pointer to the array will be left on the stack
            methodIL.Emit(OpCodes.Newarr, OBJECT_TYPE);
            LogFromIL(methodIL, "Created a new array, a pointer to it is now on the stack");

            ////Store the array into the pre created local variable
            methodIL.Emit(OpCodes.Stloc, array.LocalIndex);
            LogFromIL(methodIL, "Stored the newly created array in loc_" + array.LocalIndex);

            //Push the parameters onto the stack
            for (int i = 0; i < numArgs; i++)
                PushParameter(methodIL, array, argTypes, i);

            //Put wrapper instance on the stack
            methodIL.Emit(OpCodes.Ldarg_0);
            methodIL.Emit(OpCodes.Ldfld, _wrapperField);
            LogFromIL(methodIL, "");
            LogFromIL(methodIL, "Pushed wrapper onto the stack so one of its methods can be called");

            //Put the name of the method on the stack
            methodIL.Emit(OpCodes.Ldstr, name);
            LogFromIL(methodIL, "Pushed " + name + " onto the stack");

            //Put the array of parameters onto the stack
            methodIL.Emit(OpCodes.Ldloc, array.LocalIndex);
            LogFromIL(methodIL, "Pushed the parameters array (loc_" + array.LocalIndex + ") onto the stack");

            //Call the method and return any return value it pushes onto the stack
            LogFromIL(methodIL, "About to call " + calledMethod.Name);
            methodIL.Emit(OpCodes.Call, calledMethod);
            LogFromIL(methodIL, "");

            //If the returned value was a primitive type box it
            if (!copiedMethod.ReturnType.Equals(VOID_TYPE) && copiedMethod.ReturnType.IsValueType) {
                LogFromIL(methodIL, "Putting actual bits for return value (" + copiedMethod.ReturnType + ") onto the stack");

                methodIL.Emit(OpCodes.Unbox, copiedMethod.ReturnType);
                methodIL.Emit(OpCodes.Ldobj, copiedMethod.ReturnType);
            } else
                LogFromIL(methodIL, "Returning");

            methodIL.Emit(OpCodes.Ret);
            
            if (implementingInterface) 
                // Mark that method implements m from an interface
                proxyType.DefineMethodOverride(method, copiedMethod);

            StoreMethod(name, copiedMethod);
            LogMethodCreated("Built wrapper method: " + method.Name, copiedMethod.GetParameters());

            return method;
        }

        /// <summary>
        /// Build a method that will just forward the method call directly on to the wrapped instance.
        /// </summary>
        /// <param name="proxyType">The type being built.</param>
        /// <param name="copiedMethod">The method to copy.</param>
        /// <param name="interfaceMethod">The method in an interface which is being implemented. If null then this method is not implementing an interface.</param>
        /// <returns></returns>
        private MethodBuilder BuildForwardMethod(TypeBuilder proxyType, MethodInfo copiedMethod, MethodInfo interfaceMethod = null) {
            Type[] argTypes = GetArgTypes(copiedMethod);
            MethodBuilder method = proxyType.DefineMethod(copiedMethod.Name, MethodAttributes.Virtual | MethodAttributes.Public, copiedMethod.ReturnType, argTypes);

            // Generate IL for 'GetGreeting' method.
            ILGenerator methodIL = method.GetILGenerator();
            LogFromIL(methodIL, "");
            LogFromIL(methodIL, "Running " + copiedMethod.Name);

            methodIL.Emit(OpCodes.Ldarg_0);
            methodIL.Emit(OpCodes.Ldfld, _wrappedInstanceField);
            LogFromIL(methodIL, "Pushed wrapped instance onto the stack so one of its methods can be called.");
            //methodIL.Emit(OpCodes.Pop);

            //Push the parameters onto the stack
            for (int i = 0; i < argTypes.Length ; i++) {
                methodIL.Emit(OpCodes.Ldarg, i + 1);
                LogFromIL(methodIL, "Pushed argument " + i + " onto the stack.");
            }

            //Call the method and return any return value it pushes onto the stack
            LogFromIL(methodIL, "About to call " + copiedMethod.Name);
            methodIL.Emit(OpCodes.Call, copiedMethod);
            LogFromIL(methodIL, "");

            //LogFromIL(methodIL, "Returning");
            //methodIL.Emit(OpCodes.Ldstr, "The return value");
            methodIL.Emit(OpCodes.Ret);

            if (interfaceMethod != null)
                // Mark that method implements m from an interface
                proxyType.DefineMethodOverride(method, interfaceMethod);
            //else if (copiedMethod.IsVirtual)
            //    proxyType.DefineMethodOverride(method, copiedMethod);

            LogMethodCreated("Built forward method: " + method.Name, copiedMethod.GetParameters());

            return method;
        }

        #region Helpers

        private void PushParameter(ILGenerator methodIL, LocalBuilder array, Type[] argTypes, int i) {
            //Push the array onto the stack from the local variable
            methodIL.Emit(OpCodes.Ldloc, array.LocalIndex);
            LogFromIL(methodIL, "");
            LogFromIL(methodIL, "Pushed the array onto the stack from loc_" + array.LocalIndex);

            //Push the index to put the value at onto the stack
            methodIL.Emit(OpCodes.Ldc_I4, i);
            LogFromIL(methodIL, "Pushed the array index of " + i + " onto the stack");

            //Push argument i onto the stack
            methodIL.Emit(OpCodes.Ldarg, i + 1);
            LogFromIL(methodIL, "Pushed argument " + i + " onto the stack");

            //Put the argument into the array at position i (if necessary box it as a reference)
            if (argTypes[i].IsValueType)
                methodIL.Emit(OpCodes.Box, argTypes[i]);
            methodIL.Emit(OpCodes.Stelem_Ref);
            LogFromIL(methodIL, "Replaced the index value of " + i + " with The value on the stack");
        }

        private void LogMethodCreated(string msg, ParameterInfo[] parameters) {
            string builtStr = msg + "(";
            for (int i = 0; i < parameters.Length - 1; i++)
                builtStr += parameters[i].ParameterType.Name + ",";
            builtStr += (parameters.Length > 0 ? parameters[parameters.Length - 1].ParameterType.Name : "") + ")";
            Logger.Debug(builtStr);
        }

        private void LogFromIL(ILGenerator ilGen, string msg) {
            //return;
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldfld, _logField);
            ilGen.Emit(OpCodes.Ldstr, msg);
            ilGen.Emit(OpCodes.Call, _detailedLogMethod);
        }

        /// <summary>
        /// Get an array of types representing the types of the arguments that should be passed into the specified method.
        /// </summary>
        /// <param name="m">The method to get the argument types for</param>
        private Type[] GetArgTypes(MethodInfo m) {
            Type[] methodArgs = new Type[m.GetParameters().Length];
            int i = 0;
            foreach (ParameterInfo p in m.GetParameters())
                methodArgs[i++] = p.ParameterType;
            return methodArgs;
        }

        #endregion

        #endregion

        #endregion
    }
}
