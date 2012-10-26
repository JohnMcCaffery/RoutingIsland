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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Region.OptionalModules.Scripting.Minimodule;
using Ninject;
using dependencyinjection.factory;
using System.IO;
using Nini.Config;
using dependencyinjection.interfaces;
using MRM;
using common.Queue;
using dependencyinjection.impl;
using OpenMetaverse;
using common.interfaces.entities;
using log4net;
using common;

namespace Diagrams.MRM {
    public class MRMMetaSystem : MRMBase {
        private int CHAN = -49;
        private const string PING = "UP";
        private const string PING_ACK = "INSTANCE";
        private IKernel _k;
        private Dictionary<UUID, MRMSystem> _liveSystems = new Dictionary<UUID,MRMSystem>();

        private ILog log = LogManager.GetLogger(typeof(MRMMetaSystem));

        public override void Start(string[] args) {
            Host.Object.Say("Starting MRM Meta System.");
            _k = NinjectFactory.getKernel<DynamicLoaderModule>();
            BindableHost.Host = Host;
            BindableWorld.World = World;

            _k.Bind<IHost>().To<BindableHost>().InSingletonScope();
            _k.Bind<IWorld>().To<BindableWorld>().InSingletonScope();
            
            string queueLibrary = typeof(AsynchQueueFactory).Assembly.Location;
            string queueType = typeof(AsynchQueueFactory).FullName;
            try {
                IConfig masterConfig = new DotNetConfigSource(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile).Configs["Bootstrap"];
                queueLibrary = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, masterConfig.GetString(MRMSystem.QUEUE + MRMSystem.ASSEMBLY, queueLibrary));
                queueType = masterConfig.GetString(MRMSystem.QUEUE + MRMSystem.CLASS, queueType);
            } catch (Exception e) {
            }
            _k.Get<IDynamicLoaderModule>().BindDynamic(typeof(IAsynchQueueFactory), queueLibrary, queueType, true);

            World.OnChat += (world, chatArgs) => {
                lock (this) {
                    if (chatArgs.Channel == CHAN && chatArgs.Text.StartsWith(PING_ACK)) {
                        string configFile = chatArgs.Text.Split(new char[] { ':' }, 2)[1];
                        string baseFolder = Path.GetDirectoryName(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
                        configFile = Path.Combine(baseFolder, configFile);
                        Console.WriteLine("Tying to start " + chatArgs.Sender.Name + " from " + configFile);
                        if (!File.Exists(configFile)) {
                            //throw new Exception("Unable to start MRM system. No config file found at '" + Path.GetFullPath(configFile) + "'.");
                            return;
                        }
                        IConfig config = new DotNetConfigSource(configFile).Configs["Bootstrap"];
                        if (config == null) {
                            //throw new Exception("Unable to start MRM system. No 'Bootstrap' section found in config file '" + Path.GetFullPath(configFile) + "'.");
                            return;
                        }

                        try {
                            UUID id = chatArgs.Sender.GlobalID;
                            MRMSystem system = new MRMSystem();
                            system.InitMiniModule(World, Host, UUID.Random());
                            system.Start(_k, config, configFile, id, false);

                            _liveSystems.Add(chatArgs.Sender.GlobalID, system);
                            _k.Get<IPrimFactory>()[id].OnWorldDelete += destroyedID => {
                                Stop(system);
                                system.Stop();
                                _liveSystems.Remove(id);
                            };
                        } catch (Exception e) {
                            log.Warn("Unable to start MRM system from " + chatArgs.Sender.Name + ". " + e);
                            Host.Object.Say("Unable to start MRM system from " + chatArgs.Sender.Name, 0, true, MRMChatTypeEnum.Region);
                        }
                    }
                }
            };
            Host.Object.Say(PING, CHAN, ChatTargetEnum.LSL);
        }

        public override void Stop() {
            foreach (var system in _liveSystems.Values)
                Stop(system);

            IAsynchQueueFactory _queueFactory = _k.Get<IAsynchQueueFactory>();
            _queueFactory.ShutdownAll();
            Console.WriteLine(_queueFactory.Queues.Count + " Queues:");
            foreach (IAsynchQueue q in _queueFactory.Queues)
                Console.WriteLine("{0,-40} - Active: {1,7:0.###}% - Mean time per task: {2,8:0.###}ms - Helpers (Max: {3,2}, Tot: {4,6}) - {5,-40}"/* - Time: {6,-18} - Helper Time: {7,17}"*/,
                    q.IsRunning ? q.Name : "(" + q.Name + ")",
                    q.Statistics.PercentageActive,
                    q.Statistics.MeanProcessTime,
                    q.Statistics.MaxHelperThreads,
                    q.Statistics.TotalHelpersSpawned,
                    string.Format(q.Statistics.UnprocessedTasks > 0 ?
                        "# Tasks: {0,-6} - # Unprocessed Tasks: {1}" :
                        "# Tasks: {0}",
                        q.Statistics.QueuedTasks,
                        q.Statistics.UnprocessedTasks)/*,
                        q.Statistics.Time,
                        q.Statistics.TotalHelperTime*/
                    );
        }

        private void Stop(MRMSystem system) {
            try {
                system.Stop();
            } catch (Exception e) {
                log.Warn("Unable to stop MRM system. " + e);
                Host.Object.Say("Unable to stop MRM system.", 0, true, MRMChatTypeEnum.Region);
            }
        }
    }
}
