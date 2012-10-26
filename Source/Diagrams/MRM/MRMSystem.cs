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

#region Namespace imports

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading;
using System.Linq;
using Ninject;
using Ninject.Parameters;
using OpenMetaverse;
using OpenSim.Region.OptionalModules.Scripting.Minimodule;
using common.Queue;
using common.config;
using common.framework.interfaces.layers;
using common.interfaces.entities;
using dependencyinjection.factory;
using dependencyinjection.impl;
using dependencyinjection.interfaces;
using Diagrams;
using log4net;
using Diagrams.Common.interfaces.keytable;
using common.model.framework.interfaces;
using log4net.Config;
using Nini.Config;
using System.IO;
using Diagrams.Common.keytable;
using System.Reflection;
using Ninject.Planning.Bindings;
using Diagrams.MRM;
using JM726.Lib.Static;
using common;

#endregion

namespace MRM {
    /// <summary>
    ///   Implementation of the ISystem interface which creates the system which runs the routing project. Is created dynamically by the script assembly.
    /// 
    ///   The details of the assembly and class name for this system are loaded from a config file. The system expects to be running in its own
    ///   application domain.
    /// 
    ///   This class is the point at which the core starts to be loaded. Whereas the scrips assembly has no knowledge of the system it is to load this system
    ///   class understands the different components which make up the routing project(Model, View, Controller, Initialisation) and how to initalise them.
    ///   Uses the 4 static factory classes to initialise each layer. Will pass on all events (touches, chats, shutdown, startup) through to the layers via the 
    ///   controller layer.
    /// </summary>
    public class MRMSystem : MRMBase {
        private const string MODEL = "Model";
        private const string VIEW = "View";
        private const string CONTROL = "Control";
        internal const string QUEUE = "Queue";
        private const string TABLE = "Table";
        internal const string ASSEMBLY = "Assembly";
        internal const string CLASS = "Class";

        private static ILog Logger = LogManager.GetLogger(typeof(MRMSystem));

        #region Private Fields

        private IAsynchQueueFactory _queueFactory;
        private MRMPrimFactory _primFactory;
        private IControl _controller;

        private bool _clearCreated;
        private bool _stopQueues;
        private bool _shutdown;

        #endregion

        public override void Start(string[] args) {
            IKernel k = NinjectFactory.getKernel<DynamicLoaderModule>();
            BindableHost.Host = Host;
            BindableWorld.World = World;
            
            k.Bind<IHost>().To<BindableHost>().InSingletonScope();
            k.Bind<IWorld>().To<BindableWorld>().InSingletonScope();

            string configFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            if (!File.Exists(configFile))
                throw new Exception("Unable to start MRM system. No config file found at '" + Path.GetFullPath(configFile) + "'.");
            IConfig config = new DotNetConfigSource(configFile).Configs["Bootstrap"];
            if (config == null)
                throw new Exception("Unable to start MRM system. No 'Bootstrap' section found in config file '" + Path.GetFullPath(configFile) + "'.");
            
            string queueLibrary = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, config.GetString(QUEUE + ASSEMBLY, typeof(AsynchQueueFactory).Assembly.Location));
            string queueType = config.GetString(QUEUE + CLASS, typeof(AsynchQueueFactory).FullName);
            k.Get<IDynamicLoaderModule>().BindDynamic(typeof(IAsynchQueueFactory), queueLibrary, queueType, true);

            Start(k, config, configFile, Host.Object.GlobalID, true);
        }

        internal void Start(IKernel k, IConfig config, string configFile, UUID hostID, bool stopQueues) {
            Host.Object.Say("Starting MRMSystem.");
            string oldDir = Environment.CurrentDirectory;
            _shutdown = false;
            _stopQueues = stopQueues;

            BindClasses(k, config, configFile, hostID);

            _queueFactory = k.Get<IAsynchQueueFactory>();
            _primFactory = k.Get<IPrimFactory>() as MRMPrimFactory;
            k.Get<IView>();
            Logger.Debug("View created.");
            k.Get<IModel>();
            Logger.Debug("Model created.");
            _controller = k.Get<IControl>();
            Logger.Debug("Control created.");


            //World.OnChat += (world, args) => {
            //    if (args.Sender.GlobalID.Equals(Host.Object.OwnerId) && args.Text.ToUpper().Equals("CLEAR"))
            //        _controller.Clear(args.Sender.Name, args.Sender.GlobalID);
            //};
            if (Host.Object.Exists) 
                Host.Object.Say(AppDomain.CurrentDomain.FriendlyName + " started.");          
        }

        private void PrintStats() {
            Thread t = new Thread(() => {
                Console.WriteLine("Waiting 30000ms");
                Util.Wait(60000, !_shutdown, this);
                if (_shutdown)
                    return;
                Console.WriteLine(_queueFactory.Statistics.Breakdown);
            });
            t.Name = "Stats Thread";
            t.Start();
        }

        private void BindClasses(IKernel k, IConfig config, string configFile, UUID hostID) {
            k.Unbind<IPrimFactory>();
            k.Unbind<IConfigSource>();

            k.Bind<IPrimFactory>().To<MRMPrimFactory>().InSingletonScope().WithConstructorArgument("hostID", hostID);
            k.Bind<IConfigSource>().To<DotNetConfigSource>().InSingletonScope().WithConstructorArgument("path", configFile);

            if (!config.Contains(CONTROL + ASSEMBLY))
                throw new Exception("Unable to start up. Control is not properly specified in the configuration file. " +
                    "Config must include the key '" + CONTROL + ASSEMBLY + "' in section 'Bootstrap'.");
            if (!config.Contains(MODEL + ASSEMBLY))
                throw new Exception("Unable to start up. Model is not properly specified in the configuration file. " +
                    "Config must include the key '" + MODEL + ASSEMBLY + "' in section 'Bootstrap'.");
            if (!config.Contains(VIEW + ASSEMBLY))
                throw new Exception("Unable to start up. View is not properly specified in the configuration file. " +
                    "Config must include the key '" + VIEW + ASSEMBLY + "' in section 'Bootstrap'.");

            if (!config.Contains(CONTROL + CLASS))
                throw new Exception("Unable to start up. Control is not properly specified in the configuration file. " +
                    "Config must include the key '" + CONTROL + CLASS + "' in section 'Bootstrap'.");
            if (!config.Contains(MODEL + CLASS))
                throw new Exception("Unable to start up. Model is not properly specified in the configuration file. " +
                    "Config must include the key '" + MODEL + CLASS + "' in section 'Bootstrap'.");
            if (!config.Contains(VIEW + CLASS))
                throw new Exception("Unable to start up. View is not properly specified in the configuration file. " +
                    "Config must include the key '" + VIEW + CLASS + "' in section 'Bootstrap'.");

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string controlAssembly = Path.Combine(baseDir, config.GetString(CONTROL + ASSEMBLY));
            string modelAssembly = Path.Combine(baseDir, config.GetString(MODEL + ASSEMBLY));
            string viewAssembly = Path.Combine(baseDir, config.GetString(VIEW + ASSEMBLY));

            string controlName = config.GetString(CONTROL + CLASS);
            string modelName = config.GetString(MODEL + CLASS);
            string ViewName = config.GetString(VIEW + CLASS);

            TestModule(CONTROL, controlAssembly, controlName, typeof(IControl));
            TestModule(MODEL, modelAssembly, modelName, typeof(IModel));
            TestModule(VIEW, viewAssembly, ViewName, typeof(IView));

            IDynamicLoaderModule loader = k.Get<IDynamicLoaderModule>();

            k.Unbind<IView>();
            k.Unbind<IModel>();
            k.Unbind<IControl>();
            loader.BindDynamic(typeof(IView), viewAssembly, ViewName, true);
            loader.BindDynamic(typeof(IModel), modelAssembly, modelName, true);
            loader.BindDynamic(typeof(IControl), controlAssembly, controlName, true);

            k.Unbind<IKeyTableFactory>();
            string tableLibrary = Path.Combine(baseDir, config.GetString(TABLE + ASSEMBLY, typeof(MapTableFactory).Assembly.Location));
            string tableType = config.GetString(TABLE + CLASS, typeof(MapTableFactory).FullName);
            loader.BindDynamic(typeof(IKeyTableFactory), tableLibrary, tableType, true);

            k.Unbind<IAlgorithm>();
            string algorithmFolder = Path.Combine(baseDir, config.GetString("AlgorithmFolder", "."));
            loader.BindAllInFolder(typeof(IAlgorithm), algorithmFolder);

            _clearCreated = config.GetBoolean("ClearCreated", true);
        }

        private void TestModule(string module, string assembly, string clazz, Type intface) {
            if (!File.Exists(assembly))
                throw new Exception("'" + assembly + "'does not exist in .");
            try {
                Type t = Assembly.LoadFrom(assembly).GetType(clazz);
                if (t == null)
                    throw new Exception("Unable to bind " + module + ". There is no class '" + clazz + " in '" + assembly + "'.");
                else if (t.FindInterfaces((checkIntface, criteria) => checkIntface.FullName.Equals(intface.FullName), null).Length == 0)
                    throw new Exception("'" + clazz + "' does not implement '" + intface.Name + "'.");
                else if (!t.IsPublic)
                    throw new Exception(module + " class '" + clazz + " is not public.");
            } catch (BadImageFormatException e) {
                throw new Exception("'" + assembly + "' for " + module + " is not a valid assembly file.");
            }
        }

        public override void Stop() {
            try {
                _shutdown = true;
                Util.Wake(this);
                if (_controller != null)
                    _controller.Stop();
                if (_primFactory != null) {
                    if (_clearCreated)
                        _primFactory.ClearCreated();
                    _primFactory.Shutdown();
                } if (_queueFactory != null && _stopQueues) {
                    _queueFactory.ShutdownAll();
                    Console.WriteLine(_queueFactory.Queues.Count + " Queues:");
                    foreach (IAsynchQueue q in _queueFactory.Queues)
                        Console.WriteLine("{0,-40} - Active: {1,7:0.###}% - Mean time per task: {2,8:0.###}ms - Helpers (Max: {3,2}, Tot: {4,6}) - {5,-40}"/* - Time: {6,-18} - Helper Time: {7,17}"*/,
                            q.IsRunning ? q.Name : "(" + q.Name + ")",
                            q.Statistics.PercentageActive,
                            q.Statistics.MeanProcessTime,
                            q.Statistics.MaxHelperThreads,
                            q.Statistics.TotalHelpersSpawned,
                            string.Format(q.Statistics.UnprocessedTasks > 0 || q.Statistics.FailedTasks > 0 ?
                                q.Statistics.FailedTasks > 0 ?
                                    "# Tasks: {0,-6} - # Failed Tasks: {2}" :
                                    "# Tasks: {0,-6} - # Unprocessed Tasks: {1}" :
                                "# Tasks: {0}",
                                q.Statistics.QueuedTasks,
                                q.Statistics.UnprocessedTasks,
                                q.Statistics.FailedTasks)/*,
                        q.Statistics.Time,
                        q.Statistics.TotalHelperTime*/
                            );
                    foreach (var task in _queueFactory.Statistics.UnprocessedTaskNames)
                        Console.WriteLine(task);
                }
                if (Host.Object.Exists)
                    Host.Object.Say(AppDomain.CurrentDomain.FriendlyName + " stopped.");

                foreach (var pair in BindableWorld.CallCounts.OrderBy<KeyValuePair<string, int>, int>(pair => pair.Value))
                    Console.WriteLine("World.{0,-50} Call Count: {1}", pair.Key, pair.Value);
                foreach (var pair in BindableHost.CallCounts.OrderBy<KeyValuePair<string, int>, int>(pair => pair.Value))
                    Console.WriteLine("Host.{0,-51} Call Count: {1}", pair.Key, pair.Value);
                foreach (var pair in TrackedObjectAccessor.CallCounts.OrderBy<KeyValuePair<string, int>, int>(pair => pair.Value))
                    Console.WriteLine("Object.{0,-49} Call Count: {1}", pair.Key, pair.Value);
            } catch (Exception e) {
                Console.WriteLine(e + " \n\n " + e.StackTrace);
            }
        }
    }
}