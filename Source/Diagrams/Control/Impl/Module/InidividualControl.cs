using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Diagrams.Control.Impl.Module;
using Diagrams.Common.interfaces.keytable;
using common.Queue;
using common.interfaces.entities;
using OpenMetaverse;
using Nini.Config;
using OpenSim.Region.OptionalModules.Scripting.Minimodule;

namespace Diagrams.Control.impl.Module {
    public class IndividualControl : AdvancedControl {
        UUID _owner;
        public IndividualControl(IHost host, IKeyTableFactory tableFactory, IAsynchQueueFactory queueFactory, IPrimFactory primFactory, IModel model, UUID hostID, IConfigSource config)
            : base(tableFactory, queueFactory, primFactory, model, hostID, config) {
            
            _owner = host.Object.OwnerId;
        }

        protected override bool Authorize(UUID entity, string name, UUID id) {
            return id.Equals(_owner);
        }
    
    }
}
