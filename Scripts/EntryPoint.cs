using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;
using System.Threading;
using common;
using common.debug;
using OpenMetaverse;
using OpenSim.Region.OptionalModules.Scripting.Minimodule;
using System.Xml;
using System.Configuration;
using common.config;

namespace scripts {
    /// <summary>
    /// An entry point. This is the point at which the script ceases to be code written inside an opensim client and goes to being library code.
    /// 
    /// An instance of this class is instantiated within opensim and passes in a parameter defining the configuration file it is to load. From this
    /// the rest of the system is started up.
    /// 
    /// This class creates an instance of the root class running in a seperate application domain. This instance is accessed through a proxy.
    /// 
    /// The root class then dynamically creates an instance of the ISystem interface and the system itself is started up.
    /// </summary>
    ///  TODO Make this use IAsnychQueue
    public class EntryPoint {
        #region Private Fields

        private IAsynchQueue<Pair<IEntity, String>> chatQ;
        private IAsynchQueue<Pair<UUID, TouchEventArgs>> touchQ;
        private IAsynchQueue<UUID> touchAddQ;

        private HashSet<UUID> listening;

        private HashSet<UUID> touchListeners;

        private AppDomain domain;

        private Root proxy;

        private IHost host;

        private UUID hostID;

        private IWorld world;

        private volatile bool cont = true;

        private OnTouchDelegate touchDelegate;

        private OnChatDelegate chatDelegate;

        private string configFile;

        private string startString = null;

        private string restartString = null;

        private string shutdownString = null;

        private string clearString = null;

        private string baseFolder = null;

        private bool clean = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor. Takes as a parameter the configuration file which defines how the system is to be initialised.
        /// </summary>
        /// <param name="configFile">The configuration file with all the details of how the system is to be loaded and any parameters that are relevant</param>
        public EntryPoint(string configFile) {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(uncaughtException);
            this.configFile = configFile;
            
            listening = new HashSet<UUID>();
            touchListeners = new HashSet<UUID>();
            touchDelegate = touchListener;
            chatDelegate = chatListener;

            init();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Start the system up
        /// </summary>
        /// <param name="host">The host which hosts the primitive in which the script is running</param>
        /// <param name="world">The world object which is the key to the in world scenegraph</param>
        public void Start(IHost host, IWorld world) {
            world.OnChat += chatDelegate;

            this.world = world;
            this.host = host;

            if (AppDomain.CurrentDomain.FriendlyName.Equals("OpenSim.exe") || AppDomain.CurrentDomain.FriendlyName.Equals("OpenSim.32BitLaunch.exe")) {
                try {
                    hostID = host.Object.GlobalID;
                    listening.Add(hostID);
                } catch (Exception e) {
                    DB.Exception(e, "Error getting host id", Levels.SCRIPTS);
                }
            }

            if (proxy == null) 
                unloadDomain("Unable to start, Proxy not running, shutting down");
            else
                start();
        }

        /// <summary>
        /// Stop the system running
        /// </summary>
        public void Stop() {
            DB.Print("EntryPoint stopping", Levels.SCRIPTS);
            shutdown();
            DB.Print("EntryPoint stopped", Levels.SCRIPTS);
        }

        /// <summary>
        /// Handler for touch events from the world
        /// </summary>
        /// <param name="sender">The object which sent the touch event</param>
        /// <param name="args">Any arguments attached to the touch event</param>
        public void touchListener(IObject sender, TouchEventArgs args) {
            if (listening.Contains(sender.GlobalID)) {
                touchQ.qItem(new Pair<UUID, TouchEventArgs>(sender.GlobalID, args));
                DB.Print("Entry Point received touch event", Levels.SCRIPTS);
            }
        }

        /// <summary>
        /// Handler for chat events 
        /// </summary>
        /// <param name="world">The world object which is the key to the in world scenegraph</param>
        /// <param name="args">Any arguments related to the chat event</param>
        public void chatListener(IWorld world, ChatEventArgs args) {
            try {
                this.world = world;
                if (proxy != null)
                    proxy.updateWorld(world);
                if (listening.Contains(args.Sender.GlobalID) || !(args.Sender is IAvatar))
                    return;
            } catch (Exception e) {
                DB.Exception(e, "Problem updating proxy with world", Levels.SCRIPTS);
            }
            try {
                if (world.Objects[hostID] == null || !world.Objects[hostID].Exists) {
                    DB.Print("Controller object removed, closing down script completely", Levels.ALWAYS);
                    shutdown();
                    return;
                }
            } catch (Exception e) {
                DB.Exception(e, "Unable to perform checks on host object", Levels.SCRIPTS);
                DB.Print("Controller object removed, closing down script completely", Levels.ALWAYS);
                shutdown();
                return;
            }
            if (!isRestart(args.Text) &&
                !isStart(args.Text) &&
                !isShutdown(world, args.Text, args.Sender.Name) &&
                !isClear(args.Text, args.Sender)) {
                if (chatQ != null && chatQ.IsRunning) {
                    chatQ.qItem(new Pair<IEntity, string>(args.Sender, args.Text));
                    DB.Print("Entry Point passed down chat event: " + args.Text, Levels.SCRIPTS);
                }
            }
        }

        public static void uncaughtException(object sender, UnhandledExceptionEventArgs args) {
            Exception e = args.ExceptionObject as Exception;
            if (e != null)
                DB.Exception(e, "Unhandled exception was caught somewhere within the application domain", Levels.SCRIPTS);
            else
                DB.Print("Unhandled exception was caught somewhere within the application domain", Levels.SCRIPTS);
        }

        #endregion

        #region Main Function methods

        private void init() {
            baseFolder = loadConfigValues(configFile);
            if (baseFolder == null)
                return;

            AppDomain rootDomain = AppDomain.CurrentDomain;

            try {
                if (domain == null) {
                    AppDomainSetup domainSetup = new AppDomainSetup();

                    domainSetup.ConfigurationFile = Path.GetFullPath(configFile);
                    domainSetup.ApplicationBase = baseFolder;
                    domainSetup.ShadowCopyFiles = "true";

                    string name = configFile.Replace(".config", "");
                    name = name.Substring(name.LastIndexOf('/') + 1);

                    domain = AppDomain.CreateDomain(name, null, domainSetup);
                }

                string assembly = GetType().Assembly.Location;
                Object[] args = new Object[] { rootDomain };
                DB.Print("Creating root", Levels.SCRIPTS);

                proxy = (Root)domain.CreateInstanceFromAndUnwrap(assembly, "scripts.Root", false, BindingFlags.CreateInstance, null, args, null, null, null);
                if (proxy.Shutdown)
                    unloadDomain("Shutdown flagged after creating root");
                else 
                    DB.Print("Root created", Levels.SCRIPTS);
            } catch (Exception e) {
                DB.Exception(e, "Problem creating root", Levels.SCRIPTS);
                unloadDomain("Exception creating root");
            }
        }

        private void start() {
            clearup();
            DB.Print("Starting root", Levels.SCRIPTS);
            try {
                proxy.start(host, world);
                if (proxy.Shutdown)
                    shutdownProxy("Shutdown flagged after starting root");
                else {
                    if (chatQ != null) {
                        chatQ.stop();
                        chatQ = new AsynchQueue<Pair<IEntity, String>>();
                    }
                    if (touchQ != null) {
                        touchQ.stop();
                        touchQ = new AsynchQueue<Pair<UUID, TouchEventArgs>>();
                    }
                    if (touchAddQ != null) {
                        touchAddQ.stop();
                        touchAddQ = new AsynchQueue<UUID>();
                    }

                    chatQ = new AsynchQueue<Pair<IEntity, String>>();
                    touchQ = new AsynchQueue<Pair<UUID, TouchEventArgs>>();
                    touchAddQ = new AsynchQueue<UUID>();

                    chatQ.start(proxy.chat, "Chat Event Process", Levels.SCRIPTS);
                    touchQ.start(proxy.touched, "Touch Event Process", Levels.SCRIPTS);
                    touchAddQ.start(addTouchListener, "Touch Add Process", Levels.SCRIPTS);

                    Thread t = new Thread(checkThread);
                    t.Name = "Entry point check shutdown thread";
                    t.Start();
                    DB.Print("Root started", Levels.SCRIPTS);
                }
            } catch (Exception e) {
                DB.Exception(e, "Problem starting up root", Levels.SCRIPTS);
                shutdownProxy("Exception starting up root");
            }
        }

        private void shutdown() {
            cont = false;
            shutdownProxy();
            world.OnChat -= chatDelegate;

            if (chatQ != null) 
                chatQ.stop();
            if (touchQ != null) 
                touchQ.stop();
            if (touchAddQ != null)
                touchAddQ.stop();

            chatQ = null;
            touchQ = null;
            touchAddQ = null;

            DB.Print("Entry Point shut down queues", Levels.SCRIPTS);
        }

        private void shutdownProxy() {
            shutdownProxy("Finalising shutdown");
        }

        private void shutdownProxy(String msg) {
            DB.ThreadStarted();
            DB.Print("EntryPoint shutting down", Levels.SCRIPTS);
            if (proxy != null) {
                try {
                    DB.Print("Stopping root", Levels.SCRIPTS);
                    proxy.stop();
                    DB.Print("Root stopped", Levels.SCRIPTS);
                    if (host.Object != null && host.Object.Exists)
                        host.Object.Say("System shut down");
                } catch (Exception e) {
                    DB.Exception(e, "Exception shutting down root", Levels.SCRIPTS);
                }
            }
            unloadDomain(msg);

            DB.Print("System Stopped", Levels.ALWAYS);
            DB.Print("", Levels.ALWAYS);
            DB.Print("EntryPoint shut down", Levels.SCRIPTS);
            DB.ThreadStopped();
        }

        #endregion 

        #region Utility Methods

        /// <summary>
        /// Load all the necessary values from the config file.
        /// </summary>
        /// <param name="configFile">The config file that contains the configuration settings</param>
        /// <returns>The base folder</returns>
        private string loadConfigValues(string configFile) {
            //String baseConfig = null;
            KeyValueConfigurationElement baseConfig;
            if (configFile.Substring(configFile.Length - 7).Equals(".config"))
                configFile = configFile.Substring(0, configFile.Length - 7);
            try {
                Configuration configFig = ConfigurationManager.OpenExeConfiguration(configFile);
                KeyValueConfigurationCollection config = ConfigurationManager.OpenExeConfiguration(configFile).AppSettings.Settings;
                
                baseConfig      = config[Config.Scripts.BaseFolder];
                KeyValueConfigurationElement restartConfig = config[Config.Scripts.RestartStr];
                KeyValueConfigurationElement startConfig = config[Config.Scripts.StartStr];
                KeyValueConfigurationElement shutdownConfig = config[Config.Scripts.ShutdownStr];
                KeyValueConfigurationElement clearConfig = config[Config.Scripts.ClearStr];
                KeyValueConfigurationElement cleanConfig = config[Config.Scripts.Clean];
                KeyValueConfigurationElement debugConfig = config[Config.Common.DebugFile];

                if (baseConfig != null && debugConfig != null)
                    DB.File = Path.Combine(baseConfig.Value, debugConfig.Value);
                if (startConfig != null)
                    startString = startConfig.Value;
                if (restartConfig != null)
                    restartString = restartConfig.Value;
                if (shutdownConfig != null)
                    shutdownString = shutdownConfig.Value;
                if (clearConfig != null)
                    clearString = clearConfig.Value;
                if (cleanConfig != null) {
                    bool parsed = bool.TryParse(cleanConfig.Value, out clean);
                    clean = parsed && clean;
                }
            } catch (ConfigurationErrorsException e) {
                DB.Exception(e, "Unable to load config from " + configFile, Levels.SCRIPTS);
                DB.Print("Stopping startup", Levels.EXCEPTIONS, Levels.SCRIPTS);
                return null;
            }

            if (baseConfig == null) {
                DB.Print("Unable to load base folder from config file, shutting down", Levels.DEBUG, Levels.SCRIPTS);
                return null;
            }
            return baseConfig.Value;
        }

        private bool isStart(string text) {
            if (cont || restartString == null || (!text.Equals(startString) && !text.Equals(restartString))) return false;
            try {
                DB.Print("EntryPoint received start command, initialising", Levels.SCRIPTS);
                init();
                DB.Print("EntryPoint received start command, starting up", Levels.SCRIPTS);
                start();
                DB.Print("EntryPoint received start command, started up", Levels.SCRIPTS);
            } catch (Exception e) {
                DB.Exception(e, "Error shutting down from user command", Levels.SCRIPTS);
            }
            return true;
        }

        private bool isRestart(string text) {
            if (!cont || startString == null || !text.Equals(restartString)) return false;
            try {
                DB.Print("EntryPoint received restart command, restarting", Levels.SCRIPTS);
                shutdownProxy();
                DB.Print("EntryPoint received restart command, shut down system", Levels.SCRIPTS);
                init();
                DB.Print("EntryPoint received restart command, initialised", Levels.SCRIPTS);
                start();
                DB.Print("EntryPoint restarted the system", Levels.SCRIPTS);
            } catch (Exception e) {
                DB.Exception(e, "Error shutting down from user command", Levels.SCRIPTS);
            }
            return true;
        }

        private bool isShutdown(IWorld world, string text, string sender) {
            if (!cont || startString == null || !text.Equals(shutdownString)) return false;
            try {
                DB.Print("EntryPoint received shutdown command, shutting down", Levels.SCRIPTS);
                shutdownProxy();                
            } catch (Exception e) {
                DB.Exception(e, "Error shutting down from user command", Levels.SCRIPTS);
            }
            return true;
        }

        private bool isClear(string text, IEntity sender) {
            if (!cont || clearString == null || !text.Equals(clearString)) return false;
            try {
                DB.Print("EntryPoint received clear command, clearing", Levels.SCRIPTS);
                if (proxy != null)
                    proxy.clear(sender.GlobalID, sender.Name);
            } catch (Exception e) {
                DB.Exception(e, "Error clearing", Levels.SCRIPTS);
            }
            return true;
        }

        /// <summary>
        /// Will unload from memory the domain that was created.
        /// </summary>
        /// <param name="msg"></param>
        private void unloadDomain(string msg) {
            try {
                if (domain != null) {
                    cont = false;
                    Util.Wake(this);
                    AppDomain.Unload(domain);
                    domain = null;
                    proxy = null;

                    listening.Clear();
                    DB.Print("Domain unloaded and listeners removed (" + msg + ")", Levels.SCRIPTS);
                }
            } catch (Exception e) {
                DB.Exception(e, "Exception unloading domain", Levels.SCRIPTS);
            }
        }

        /// <summary>
        /// Method used to add a touch listener to an object in world
        /// </summary>
        /// <param name="target"></param>
        private void addTouchListener(UUID target) {
            IObject targetObj = world.Objects[target];

            if (targetObj == null)
                throw new Exception("EntryPoint received invalid listener add request for " + target + ", world does not know about target");
            if (!AppDomain.CurrentDomain.FriendlyName.Equals("OpenSim.exe") && 
                !AppDomain.CurrentDomain.FriendlyName.Equals("OpenSim.32BitLaunch.exe"))
                throw new Exception("EntryPoint received invalid listener add request, app domain is invalid (" + AppDomain.CurrentDomain + ")");

            if (!touchListeners.Contains(targetObj.GlobalID))
                targetObj.OnTouch += touchDelegate;
            DB.Print("Entry Point adding listener to " + targetObj.Name, Levels.SCRIPTS);
            listening.Add(targetObj.GlobalID);
            touchListeners.Add(targetObj.GlobalID);
        }

        /// <summary>
        /// Used to clear away any entities which might have been created by previous incarnations of the system
        /// </summary>
        private void clearup() {
            if (!clean)
                return;
            DB.Print("Clearing up", Levels.SCRIPTS);
            LinkedList<IObject> delList = new LinkedList<IObject>();

            foreach (IObject obj in world.Objects) {
                try {
                    if ((obj.Name.Length > 2 && obj.Name.Substring(0, 3).Equals("EP ")) ||
                        (obj.Name.Length > 3 && obj.Name.Substring(0, 4).Equals("Link")) ||
                        (obj.Name.Length > 5 && (obj.Name.Substring(0, 6).Equals("Packet") || obj.Name.Substring(0, 6).Equals("Router"))))
                        delList.AddLast(obj);
                } catch (Exception e) {
                    DB.Exception(e, "Error up trying check " + (obj != null && obj.Exists ? obj.Name : "object") + " for deletion", Levels.SCRIPTS);
                }
            }
            if (delList.Count > 0)
                DB.Print("Found " + delList.Count + " objects to delete", Levels.ALWAYS);
            int i = 0;
            foreach (IObject del in delList) {
                try {
                    DB.Print("Deleting " + del.Name);
                    world.Objects.Remove(del);
                    i++;
                } catch (Exception e) {
                    DB.Exception(e, "Error trying to delete " + (del != null && del.Exists ? del.Name : "object"), Levels.SCRIPTS);
                }
            }
            if (delList.Count > 0)
                DB.Print("Deleted " + i + " objects out of " + delList.Count, Levels.ALWAYS);
        }

        #endregion

        #region Threads

        /// <summary>
        /// Thread that consumes events which are queued when opensim triggers chat or touch events
        /// </summary>
        private void checkThread() {
            DB.ThreadStarted();

            cont = true;            
            while (cont) {
                UUID target = proxy.getListenerTarget();
                while (!proxy.Shutdown && !target.Equals(UUID.Zero)) {
                    touchAddQ.qItem(target);   
                    target = proxy.getListenerTarget();
                }
                if (proxy == null || proxy.Shutdown)
                    shutdownProxy();
                else
                    Util.Wait(50, proxy != null && !proxy.Shutdown);
            }
            DB.ThreadStopped();
        }

        #endregion
    }
}