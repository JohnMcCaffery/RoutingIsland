using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Xml;
using OpenMetaverse;
using common.framework.impl.util;
using common.interfaces.entities;
using log4net;

namespace Diagrams {
    public class GenericLoggerOriginal<TToLog> where TToLog : class {
        // ReSharper disable StaticFieldInGenericType
        private static readonly Dictionary<Type, XmlSerializeDelegate> NodeCreators =
            new Dictionary<Type, XmlSerializeDelegate>();

        private static readonly ConstructorInfo LoggerConstructor =
            typeof (AbstractLogger).GetConstructor(new Type[2] {typeof (TToLog), typeof (IPrimFactory)});

        private static readonly MethodInfo LogMethodVoid = typeof (AbstractLogger).GetMethod("LogMethodCallVoid");

    private static readonly ModuleBuilder Module = 
            AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName("LoggerAssembly"), 
                AssemblyBuilderAccess.RunAndCollect)
            .DefineDynamicModule("LoggerAssembly.dll");
        // ReSharper restore StaticFieldInGenericType


        private readonly AbstractLogger _logger;
        private readonly TToLog _logWrapper;
        private IPrimFactory _primFactory;

        public TToLog LogWrapper { get { return _logWrapper; } }
        public  XmlDocument Log { get { return _logger.Log; } }

        public static void AddNodeCreator(Type type, XmlSerializeDelegate serializeDelegate) {
            if (NodeCreators.ContainsKey(type))
                NodeCreators[type] = serializeDelegate;
            else
                NodeCreators.Add(type, serializeDelegate);
        }

        public static void Test1(params object[] args) { }
        public void Test2(params string[] args) { }
        public void Test3(params Object[] args) { }
        public void Test4(params String[] args) { }
        public void Test5(params Object[] parameters) { }
        public void Test6(params String[] parameters) { }

        public GenericLoggerOriginal(TToLog toWrap, IPrimFactory primFactory) {
            MethodInfo m1 = typeof(GenericLoggerOriginal<TToLog>).GetMethod("Test1");
            MethodInfo m2 = typeof(GenericLoggerOriginal<TToLog>).GetMethod("Test2");
            MethodInfo m3 = typeof(GenericLoggerOriginal<TToLog>).GetMethod("Test3");
            MethodInfo m4 = typeof(GenericLoggerOriginal<TToLog>).GetMethod("Test4");
            MethodInfo m5 = typeof(GenericLoggerOriginal<TToLog>).GetMethod("Test5");
            MethodInfo m6 = typeof(GenericLoggerOriginal<TToLog>).GetMethod("Test6");

            _primFactory = primFactory;

            Type loggerType = typeof (TToLog);

            if (!loggerType.IsInterface)
                throw new Exception("The logger type specified must be an interface");

            TypeBuilder logger = Module.DefineType(loggerType.Name + "LogWrapper", TypeAttributes.Public | TypeAttributes.Class, typeof(AbstractLogger));
            logger.AddInterfaceImplementation(loggerType);

            buildConstructor(logger);

            Type absLoggerType = typeof (AbstractLogger);

            FieldInfo[] fields = absLoggerType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

            MethodInfo test = loggerType.GetMethod("Test");
            FieldInfo parametersField = typeof (AbstractLogger).GetField("parameters", BindingFlags.NonPublic | BindingFlags.Instance);

            MethodBuilder method = logger.DefineMethod("TestAttached", MethodAttributes.Public, typeof(void), getArgs(test));
            ILGenerator methodIL = method.GetILGenerator();
            methodIL.EmitWriteLine("Testing");

            LocalBuilder array = methodIL.DeclareLocal(typeof(object[]));
            methodIL.EmitWriteLine("Specified local_" + array.LocalIndex + " as an array of objects");

            //Declare the size of the array
            Type[] argTypes = getArgs(typeof(AbstractLogger).GetMethod("Test"));
            int numArgs = argTypes.Length;
            //Push the size of the array onto the stack
            methodIL.Emit(OpCodes.Ldc_I4, numArgs);
            //Create the array, a pointer to the array will be left on the stack
            methodIL.Emit(OpCodes.Newarr, typeof(object));
            methodIL.EmitWriteLine("Created a new array, a pointer to it is now on the stack");

            //Store the array in loc_0
            methodIL.Emit(OpCodes.Stloc_0);
            methodIL.EmitWriteLine("Stored the array from field 'parameters' at loc_0");

            for (int i = 0; i < numArgs; i++) {
                //Push the array onto the stack from the local variable
                methodIL.Emit(OpCodes.Ldloc_0);
                methodIL.EmitWriteLine("Pushed the array onto the stack from loc_0");

                //Push the index to put the value at onto the stack
                methodIL.Emit(OpCodes.Ldc_I4, i);
                methodIL.EmitWriteLine("Pushed the array index of " + i + " onto the stack");
                //Push a test string onto the stack. Test string is the 4th argument given to the method
                methodIL.Emit(OpCodes.Ldarg, i+1);
                methodIL.EmitWriteLine("Pushed parameter " + (i+1) + " onto the stack");

//                methodIL.Emit(OpCodes.Stloc_1);
//                methodIL.Emit(OpCodes.Ldloca, 1);

                //Put the test string into the array at position i
                if (argTypes[i].IsValueType) 
                    methodIL.Emit(OpCodes.Box, argTypes[i]);
                methodIL.Emit(OpCodes.Stelem_Ref);
                methodIL.EmitWriteLine("Replaced the index value of 0 with The value on the stack\n");
            }
            
            //Put 'this' on the stack
            methodIL.Emit(OpCodes.Ldarg_0);
            methodIL.EmitWriteLine("Pushed 'this' onto the stack");

            //Put the name of the method on the stack
            methodIL.Emit(OpCodes.Ldstr, test.Name);
            methodIL.EmitWriteLine("Pushed " + test.Name + " onto the stack");

            //Put the number of arguments onto the stack
            methodIL.Emit(OpCodes.Ldc_I4, numArgs);
            methodIL.EmitWriteLine("Pushed the number of arguments onto the stack");

            //Put the array onto the stack
            methodIL.Emit(OpCodes.Ldloc_0);
            methodIL.EmitWriteLine("Pushed the array onto the stack");

            methodIL.Emit(OpCodes.Call, typeof(AbstractLogger).GetMethod("TestMethod"));
//            methodIL.Emit(OpCodes.Call, LogMethodVoid);

//            methodIL.Emit(OpCodes.Pop);
//            methodIL.Emit(OpCodes.Pop);
//            methodIL.Emit(OpCodes.Pop);

            methodIL.Emit(OpCodes.Ret);

            foreach (MethodInfo m in loggerType.GetMethods())
                buildMethod(logger, m);
            
            Type t = logger.CreateType();
            _logWrapper = Activator.CreateInstance(t, toWrap, primFactory) as TToLog;
            _logger = _logWrapper as AbstractLogger;
        }

        private void buildConstructor(TypeBuilder logger) {
            ConstructorBuilder constructor = logger.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[2] { typeof(TToLog), typeof(IPrimFactory) });
            ILGenerator ctorIL = constructor.GetILGenerator();
            ctorIL.Emit(OpCodes.Ldarg_0);
            ctorIL.Emit(OpCodes.Ldarg_1);
            ctorIL.Emit(OpCodes.Ldarg_2);
            ctorIL.Emit(OpCodes.Call, LoggerConstructor);
            ctorIL.Emit(OpCodes.Ret);
        }

        private Type[] getArgs(MethodInfo m) {
            Type[] methodArgs = new Type[m.GetParameters().Length];
            int i = 0;
            foreach (ParameterInfo p in m.GetParameters())
                methodArgs[i++] = p.ParameterType;
            return methodArgs;
        }

        private void buildMethod(TypeBuilder logger, MethodInfo m) {
            Type[] argTypes = getArgs(m);
            int numArgs = argTypes.Length;
            MethodBuilder method = logger.DefineMethod(m.Name, MethodAttributes.Public | MethodAttributes.Virtual, m.ReturnType, argTypes);
            
            // Generate IL for 'GetGreeting' method.
            ILGenerator methodIL = method.GetILGenerator();
            LocalBuilder array = methodIL.DeclareLocal(typeof(object[]));
            methodIL.EmitWriteLine("Specified local_" + array.LocalIndex + " as an array of objects");

            //Push the size of the array onto the stack
            methodIL.Emit(OpCodes.Ldc_I4, numArgs);
            //Create the array, a pointer to the array will be left on the stack
            methodIL.Emit(OpCodes.Newarr, typeof(object));
            methodIL.EmitWriteLine("Created a new array, a pointer to it is now on the stack");

            //Store the array in loc_0
            methodIL.Emit(OpCodes.Stloc_0);
            methodIL.EmitWriteLine("Stored the array from field 'parameters' at loc_0");

            for (int i = 0; i < numArgs; i++) {
                //Push the array onto the stack from the local variable
                methodIL.Emit(OpCodes.Ldloc_0);
                methodIL.EmitWriteLine("Pushed the array onto the stack from loc_0");

                //Push the index to put the value at onto the stack
                methodIL.Emit(OpCodes.Ldc_I4, i);
                methodIL.EmitWriteLine("Pushed the array index of " + i + " onto the stack");
                //Push a test string onto the stack. Test string is the 4th argument given to the method
                methodIL.Emit(OpCodes.Ldarg, i + 1);
                methodIL.EmitWriteLine("Pushed parameter " + (i + 1) + " onto the stack");

                //Put the test string into the array at position i
                if (argTypes[i].IsValueType)
                    methodIL.Emit(OpCodes.Box, argTypes[i]);
                methodIL.Emit(OpCodes.Stelem_Ref);
                methodIL.EmitWriteLine("Replaced the index value of " + i + " with The value on the stack\n");
            }

            //Put 'this' on the stack
            methodIL.Emit(OpCodes.Ldarg_0);
            methodIL.EmitWriteLine("Pushed 'this' onto the stack");

            //Put the name of the method on the stack
            methodIL.Emit(OpCodes.Ldstr, m.Name);
            methodIL.EmitWriteLine("Pushed " + m.Name + " onto the stack");

            //Put the number of arguments onto the stack
            methodIL.Emit(OpCodes.Ldc_I4, numArgs);
            methodIL.EmitWriteLine("Pushed the number of arguments onto the stack");

            //Put the array onto the stack
            methodIL.Emit(OpCodes.Ldloc_0);
            methodIL.EmitWriteLine("Pushed the array onto the stack");

            //Call the method and return any return value it pushes onto the stack
            methodIL.Emit(OpCodes.Call, LogMethodVoid);
            methodIL.Emit(OpCodes.Ret);

            // Mark that method implements m from TToWrap
            logger.DefineMethodOverride(method, m);
        }

        public void Start() {
            _logger.Start();
        }

        public void Stop() {
            _logger.Stop();
        }

        public abstract class AbstractLogger {
            public void Test(UUID testID, Vector3 testVector, Parameters testParameters, String testString, int testInt, float testFloat, double testDouble) {
                
            }
            private ILog _logger;

            private TToLog _toWrap;
            private Type _logType;
            private string _name;
            private IPrimFactory _primFactory;

            private XmlDocument _doc;
            private XmlNode _root;
            private DateTime _lastEvent;

            private bool _logging;

            protected object[] parameters = new object[1000];

            internal XmlDocument Log { get { return _doc; } }

            public void TestMethod(String test, int numParameters, params object[] parameters) {
                if (parameters == null) {
                    Console.WriteLine("Test method was called from IL code with param " + test +
                                  " and parameters was not set");
                    return;
                }

                Console.WriteLine("Test method was called from IL code with param " + test + " and " + numParameters + " other parameters");
                for (int i = 0; i < numParameters; i++)
                    Console.WriteLine("Parameter " + i + " of type " + parameters[i].GetType().Name + " is " + parameters[i]);
            }

            public AbstractLogger(TToLog toWrap, IPrimFactory primFactory) {
                _toWrap = toWrap;
                _primFactory = primFactory;
                _logType = typeof(TToLog);
                _lastEvent = default(DateTime);

                _name = typeof(TToLog).FullName + "GenericLogger";
                _logger = LogManager.GetLogger(_name);

                AddNodeCreator(Parameters.VectorType, LogVector);
                AddNodeCreator(Parameters.IdType, LogID);
                AddNodeCreator(typeof(Parameters), LogParameters);
            }

            internal void Start() {
                _doc = new XmlDocument();
                XmlNode declaration = _doc.CreateXmlDeclaration("1.0", "utf-8", "yes");
                _root = _doc.CreateElement("Events");

                _doc.AppendChild(declaration);
                _doc.AppendChild(_root);

                _logging = true;
            }

            internal void Stop() {
                _logging = false;
            }

            //TODO do a return version of this
            public void LogMethodCallVoid(string methodName, int numParameters, params object[] parameters) {
//                var parameters = new object[numParameters];
//                for (int i = 0; i < numParameters; i++)
//                    parameters[i] = this.parameters[i];
                if (_logging) {
                    try {
                        XmlNode method = _doc.CreateElement(methodName);

                        LogTime(method);
                        LogMethodParameters(method, parameters);

                        _doc.AppendChild(method);
                    } catch (Exception e) {
                        _logger.Debug(_name + " unable to log " + methodName, e);
                    }
                }
                try {
                    MethodInfo wrappedMethod = _logType.GetMethod(methodName);
                    wrappedMethod.Invoke(_toWrap, parameters);
                } catch (Exception e) {
                    _logger.Debug(_name + " unable to invoke " + methodName, e);
                }
            }

            private void LogTime(XmlNode method) {
                XmlAttribute time = _doc.CreateAttribute("time");
                if (_lastEvent.Equals(default(DateTime)))
                    time.Value = "0";
                else
                    time.Value = DateTime.Now.Subtract(_lastEvent).TotalMilliseconds + "";
                method.Attributes.Append(time);
                _lastEvent = DateTime.Now;
            }

            private void LogMethodParameters(XmlNode method, object[] parameters) {
                for (int i = 0; i < parameters.Length; i += 2) {
                    if (!(parameters[i] is string) || i + 1 == parameters.Length) continue;

                    var param = parameters[i + 1];
                    var paramType = param.GetType();

                    method.AppendChild(LogMethodParameter(paramType.FullName, param, paramType));
                }
            }

            private XmlNode LogMethodParameter(string name, object param, Type paramType) {
                if (NodeCreators.ContainsKey(paramType))
                    return NodeCreators[paramType](name, param);

                XmlNode node = _doc.CreateTextNode(name);
                node.Value = param.ToString();
                return node;
            }

            private XmlNode LogParameters(string name, object toSerialize) {
                Parameters parameters = toSerialize as Parameters;
                XmlNode node = _doc.CreateElement(name);

                foreach (String key in parameters.Keys)
                    node.AppendChild(LogMethodParameter(key, parameters.Get(key), parameters.Get(key).GetType())) ;

                return node;
            }

            private XmlNode LogID(string name, object toSerialize) {
                UUID id = (UUID) toSerialize;
                XmlNode node = _doc.CreateTextNode(name);
                node.Value = _primFactory[id].Name;
                return node;
            }

            private XmlNode LogVector(string name, object toSerialize) {
                Vector3 vector = (Vector3) toSerialize;
                XmlNode x = _doc.CreateTextNode("X");
                XmlNode y = _doc.CreateTextNode("Y");
                XmlNode z = _doc.CreateTextNode("Z");

                x.Value = vector.X + "";
                y.Value = vector.Y + "";
                z.Value = vector.Z + "";

                XmlNode node = _doc.CreateElement(name);
                node.AppendChild(x);
                node.AppendChild(y);
                node.AppendChild(z);

                return node;
            }
        }
    }
}
