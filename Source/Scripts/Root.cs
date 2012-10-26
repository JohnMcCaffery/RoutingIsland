using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using OpenSim.Region.OptionalModules.Scripting.Minimodule;
using System.Threading;
using System.IO;
using System.Configuration;
using System.Runtime.Remoting;
using common.debug;
using OpenMetaverse;
using common.config;
using common;

namespace scripts {
    /// <summary>
    /// TODO Completely decouple all method calls using IASynchQueue
    /// </summary>
    internal class Root : MarshalByRefObject {
        //private const string BOOTSTRAP_ASSEMBLY = "Bootstrap.assembly";
        //private const string SYSTEM_CLASS = "System.class";
        private Queue<UUID> listeners;

        private ISystem system;

        private IHost host;
        private IWorld world;

        private AppDomain domain;
        private AppDomain rootDomain;

        private bool shutdownFlag = false;

        private string sender;
        private UUID senderID;

        public bool Shutdown {
            get { return shutdownFlag; }
        }

        public Root(AppDomain rootDomain) {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(EntryPoint.uncaughtException);            
            DB.File = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppSettings[Config.Common.DebugFile]);
            this.listeners = new Queue<UUID>();
            this.rootDomain = rootDomain;
            this.domain = AppDomain.CurrentDomain;

            run(initThread);
        }

        public UUID getListenerTarget() {
            if (listeners.Count > 0) {
                UUID target = listeners.Dequeue();
                Util.Wake(target);
                return target;
            }
            return UUID.Zero;
        }

        public void start(IHost host, IWorld world) {
            this.host = host;
            this.world = world;
            run(startedThread);
        }

        public void stop() {
            DB.Print("Root stopping", Levels.SCRIPTS);
            run(stoppedThread);
            common.Util.WaitForThreads(60000);
            DB.PrintThreads();
        }

        public void clear(UUID id, string sender) {
            DB.Print("Root clearing", Levels.SCRIPTS);
            this.sender = sender;
            this.senderID = id;
            run(clearedThread);
        }

        public void touched(Pair<UUID, TouchEventArgs> argsWrapper) {
            UUID touchee = argsWrapper.A;
            TouchEventArgs args = argsWrapper.B;
            try {
                if (system != null)
                    system.touched(touchee, args);
            } catch (Exception e) {
                DB.Exception(e, "Error processing touch event", Levels.SCRIPTS);
            }
        }
        public void chat(Pair<IEntity, String> args) {
            IEntity sender = args.A;
            string text = args.B;
            try {
                if (system != null)
                    system.chat(sender, text);
            } catch (Exception e) {
                DB.Exception(e, "Error processing chat event", Levels.SCRIPTS);
            }
        }

        public void updateWorld(IWorld world) {
            if (system != null)
                system.updateWorld(world);
        }

        private void run(ThreadStart thread) {
            Thread t = new Thread(thread);
            t.Start();
            t.Join();
        }

        private void initThread() {
            DB.ThreadStarted();
            try {
                string assembly = ConfigurationManager.AppSettings[Config.Scripts.BootstrapAssembly];
                string systemClass = ConfigurationManager.AppSettings[Config.Scripts.BootstrapClass];

                if (assembly == null)
                    throw new Exception("Bootstrap assembly not set");
                if (systemClass == null)
                    throw new Exception("Entry point class not set");
                
                assembly = Path.Combine(domain.BaseDirectory, assembly);
                system = (ISystem) Activator.CreateInstanceFrom(assembly, systemClass).Unwrap();
                
                DB.Print("Root created system from - " + systemClass + " - assembly:", Levels.SCRIPTS);
                DB.Print(assembly, Levels.SCRIPTS);
                DB.Print("With base folder:", Levels.SCRIPTS);
                DB.Print(AppDomain.CurrentDomain.BaseDirectory, Levels.SCRIPTS);
            } catch (Exception e) {
                catchException(e, "Initialisation failed");
            }
            DB.ThreadStopped();
        }

        private void startedThread() {
            DB.ThreadStarted();
            if (system == null) {
                shutdown("entry Point not configured, shutting down");
                DB.ThreadStopped();
                return;
            }
            try {
                system.start(host, world, new ListenerAdderDelegate(listenerAdder), new ShutdownDelegate(shutdown));
                DB.Print("Root started system", Levels.SCRIPTS);
            } catch (Exception e) {
                catchException(e, "Startup failed");
            }
            DB.ThreadStopped();
        }

        private void stoppedThread() {
            DB.ThreadStarted();
            if (system != null) {
                try {
                    system.stop();
                    DB.Print("Root stopped system", Levels.SCRIPTS);
                } catch (Exception e) {
                    catchException(e, "Shutdown failed");
                }
            }
            DB.ThreadStopped();
        }

        private void clearedThread() {
            DB.ThreadStarted();
            if (system != null) {
                try {
                    system.clear(senderID, sender);
                    DB.Print("Root cleared system", Levels.SCRIPTS);
                } catch (Exception e) {
                    DB.Exception(e, "Clear failed", Levels.SCRIPTS);
                }
            }
            DB.ThreadStopped();
        }

        private void listenerAdder(UUID target) {
            listeners.Enqueue(target);
        }

        private void catchException(Exception e, string msg) {
            DB.Exception(e, msg, Levels.SCRIPTS);
            shutdown(e.GetType().Name + " caught");
        }

        private void shutdown(string reason) {
            if (rootDomain != null) {
                DB.Print("Root signallig shutdown - " + reason, Levels.SCRIPTS);
                shutdownFlag = true;
            }
        }
    }
}
