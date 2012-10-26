using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Diagrams.Control.Impl.Module;
using common.framework.interfaces.basic;
using common.interfaces.entities;
using OpenSim.Region.OptionalModules.Scripting.Minimodule;
using Diagrams.Common.interfaces.keytable;
using common.Queue;
using OpenMetaverse;
using Nini.Config;

namespace Diagrams.Control.impl.Module {
    public class HudControl : AdvancedControl {


        public HudControl(IKeyTableFactory tableFactory, IAsynchQueueFactory queueFactory, IPrimFactory primFactory, IModel model, UUID hostID, IConfigSource config)
            : base(tableFactory, queueFactory, primFactory, model, hostID, config) {
        }

        protected override Dictionary<string, List<IPrim>> GetPrimNames(IPrimFactory primFactory) {
            Dictionary<string, List<IPrim>> primNames = new Dictionary<string, List<IPrim>>();
            foreach (IPrim child in HostPrim.Children) {
                if (child == null)
                    continue;
                if (!primNames.ContainsKey(child.Name))
                    primNames.Add(child.Name, new List<IPrim>());
                primNames[child.Name].Add(child);
            }
            return primNames;
        }
    }
}
