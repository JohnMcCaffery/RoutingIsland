using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Diagrams.Common.interfaces.keytable;
using common.Queue;
using common.interfaces.entities;
using OpenMetaverse;
using Nini.Config;
using System.IO;
using common.framework.interfaces.basic;

namespace Diagrams.Control.Impl.Module {
    public class AdvancedControl : SandboxControl {

        public AdvancedControl(IKeyTableFactory tableFactory, IAsynchQueueFactory queueFactory, IPrimFactory primFactory, IModel model, UUID hostID, IConfigSource config)
            : base(tableFactory, queueFactory, primFactory, model, hostID, config) {

            IConfig controlConfig = config.Configs["Control"];
            if (controlConfig == null)
                controlConfig = config.Configs[0];
            _defaultTopologyName = controlConfig.Get("DefaultTopologyName", "Topology");
            _defaultRecordingName = controlConfig.Get("DefaultSequenceName", "Sequence");
        }

        protected override void InitButtons(Dictionary<string, List<IPrim>> primNames, IPrimFactory factory) {
            base.InitButtons(primNames, factory);
            float x = 0f;
            float y = -2.5f;
            float z = .75f;
            float inc = 1.5f;
            Vector3 scale = new Vector3(.6f, 1f, 1f);
            AddButton(primNames, "Load", new Vector3(x, y, z), scale, LoadButtonTouched);
            AddButton(primNames, "Save", new Vector3(x, y += inc, z), scale, SaveTopologyTouched);
        }

        protected override void InitToggles(Dictionary<string, List<common.framework.interfaces.basic.IPrim>> primNames) {
            base.InitToggles(primNames);
            float x = 0f;
            float y = 8f;
            float z = .75f;
            Vector3 scale = new Vector3(.6f, 1f, 1f);
            AddToggle(primNames, "Record", new Vector3(x, y, z), scale, "Record", RecordTouched);
        }
    }
}
