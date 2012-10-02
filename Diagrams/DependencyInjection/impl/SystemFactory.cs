using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Region.OptionalModules.Scripting.Minimodule;
using System.Threading;
using common.debug;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting;
using System.Reflection;
using System.Configuration;
using common;
using common.framework.interfaces.layers;
using bootstrap.impl.factories;

namespace Bootstrap {
    /// <summary>
    /// Factory used to dynamically create a system
    /// </summary>
    public class SystemFactory : MarshalByRefObject {
        #region Private Fields

        /// <summary>
        /// The controller to control the system
        /// </summary>
        private IControllerLayer controller;

        private IInitialisationLayer initLayer;
        
        private IModelLayerNetwork net;

        private IModelConfig sim;

        private IViewLayerPhysical phys;

        private IViewLayerConfig view;

        /// <summary>
        /// Whether the system is already running (used to tell if the system has been reset)
        /// </summary>
        private bool running = false;

        /// <summary>
        /// Whether startup failed
        /// </summary>
        private static bool failed = false;

        /// <summary>
        /// The name of the primitive before the system ran so it can be put back as it was when the script stops
        /// </summary>
        private string name;

        private AppDomain domain;

        /// <summary>
        /// Create the new system, takes the AppDomain in which the system is to function as a parameter
        /// </summary>
        /// <param name="domain">The applicaton domain in which the system will function</param>
        public SystemFactory(AppDomain domain) {
            
            DB.Print("EntryPoint created");

            rootBinDir = domain.BaseDirectory;

            foreach (string file in Directory.GetFiles(rootBinDir)) 
                currentLibraries.Add(fileName(file));

            INSTANCE = this;
        }

        #endregion

        #region IEntryPoint Members

        /// <summary>
        /// Start the system
        /// </summary>
        /// <param name="host"></param>
        /// <param name="world"></param>
        public void start(IHost host, IWorld world) {            
            try {
                //DB.Print("EntryPoint.Start()");
                domain = AppDomain.CurrentDomain;

                name = host.Object.Name;

                if (running)
                    Stop();
                
                if (getLevel() > 3) {
                    phys = ViewLayerFactory.createPhysicalLayer(host, world);
                    view = ViewLayerFactory.createViewLayer(host, world);
                    net = ModelLayerFactory.createNetworkLayer();
                    sim = ModelLayerFactory.createSimulationLayer();
                    initLayer = InitialisationFactory.createInitialisationLayer(net, sim, phys, view);
                    controller = ControllerLayerFactory.createControllerLayer(initLayer, host, world);
                }

                host.Object.Name = "Controller";

                try {
                    //controller.startup();
                } catch (Exception e) {
                    DB.Exception(e, "Unable to start controller", Levels.STARTUP);
                }

                running = true;
            } catch (Exception e) {
                DB.Exception(e, "Startup failed", Levels.STARTUP);
                Stop();
            }
            Thread autoShutdown = new Thread(autoShutdownThread);
            autoShutdown.Name = "Auto Shutdown Thread";
            autoShutdown.Start();
            Util.Wait(500);
            DB.Print("I am root");
            DB.Print("Root: I just started auto shutdown thread and I am running in thread " + Thread.CurrentThread.Name, Levels.DEBUG);
            DB.Print("Root: My stack trace is:");
            DB.StackTrace();
        }

        private void autoShutdownThread() {            
            DB.ThreadStarted();
            DB.Print("Shutdown: I am running in thread " + Thread.CurrentThread.Name, Levels.DEBUG);

            DB.Print("Shutdown: My stack trace is:");
            DB.StackTrace();

            Util.Wait(30000);

            DB.Print("AutoShutdownThread forcing shutdown");
            stop();
            failed = true;
            DB.ThreadStopped(); 
        }

        public void stop() {
            
            DB.Print("EntryPoint stopping");
            try {
                if (controller != null)
                    controller.shutdown();
                DB.Print("Shutting down controller", Levels.SHUTDOWN, Levels.BOOTSTRAP);
            } catch (Exception e) {
                DB.Exception(e, "Shutdown failed", Levels.SHUTDOWN);
                Util.WaitForThreads(30000);
                DB.PrintThreads();
            }
            Util.WaitForThreads(30000);
            DB.PrintThreads();
        }

        private void catchException(Exception e, string msg) {
            DB.Exception(e, msg, Levels.BOOTSTRAP);
            failed = true;
        }

        #endregion
        
        #region Static Behaviour

        private static SystemFactory INSTANCE;

        private static readonly string LEVEL = "Global.Level";

        private static List<string> currentLibraries;

        private static string rootBinDir;

        public static bool Failed {
            get { return failed; }
        }

        public static void Stop() {
            
            if (INSTANCE != null) {
                INSTANCE.stop();
                failed = true;
            }
        }

        public Object createInstance(string assembly, string type, Object[] args) {
            
            string assemblyFolder = assembly.Substring(0, assembly.LastIndexOf('/') - 1);
            DB.Print(assemblyFolder, Levels.DEBUG);
            string assemblyFile = fileName(assembly);
            DB.Print(assemblyFile, Levels.DEBUG);

            
            foreach (string file in Directory.GetFiles(assemblyFolder)) {
                string name = fileName(file);
                if (!currentLibraries.Contains(file)) {
                    File.Copy(file, Path.Combine(rootBinDir, name));
                    currentLibraries.Add(name);
                }
            }

            try {
                ObjectHandle handle = Activator.CreateInstanceFrom(assemblyFile, type, false, BindingFlags.CreateInstance, null, args, null, null, null);
                if (handle == null)
                    return null;
                return handle.Unwrap();
            } catch (Exception e) {
                DB.Exception(e, "Fucked up trying to load in " + assemblyFile + "." + type, Levels.BOOTSTRAP);
                Stop();
                return null;
            }
        }

        private static string fileName(string file) {
            
            return file.Substring(file.LastIndexOf('/') + 1);
        }

        public static int getLevel() {
            
            try {
                return Int32.Parse(ConfigurationManager.AppSettings[LEVEL]);
            } catch (Exception e) {
                return 1;
            }
        }


        #endregion
    }
}
