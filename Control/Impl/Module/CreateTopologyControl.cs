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
using Diagrams.Common.interfaces.keytable;
using common.Queue;
using common.interfaces.entities;
using OpenMetaverse;
using Nini.Config;
using Diagrams.Control.impl.Buttons.ControlButtons;
using Diagrams.Control.impl.Controls.Dialogs;
using Diagrams.MRM.Controls.Buttons;
using Diagrams.Control.impl.Util;
using Diagrams.Control.Impl.Util.State;
using Diagrams.Control.Impl.Util;

namespace Diagrams.Control.Impl.Module {
    public class CreateTopologyControl : SandboxControl {
        private ChatDelegate _listener;
        private const string GOD_KEY = "TopologyOwner";
        private const string TOPOLOGY_KEY = "Topology";

        private const string GOD = ".";
        private const string TOPOLOGY = "Topology";

        public CreateTopologyControl(IKeyTableFactory tableFactory, IAsynchQueueFactory queueFactory, IPrimFactory primFactory, IModel model, IConfigSource config)
            : base(tableFactory, queueFactory, primFactory, model, config) {
            
            /*
            //Touch Button
            IButton floor = Factory.MakeButton("Floor", Permissions, HostPrim.ID);
            floor.OnTouched += (source, args) =>  AddRouter(args.AvatarName, args.AvatarID, args.TouchPosition);
            */
            IConfig controlConfig = config.Configs["Control"];

            string god = controlConfig.Get(GOD_KEY, GOD);
            string topologyDefault = controlConfig.Get(TOPOLOGY_KEY, GOD);

            _listener = (name, id, text, channel) => {
                string[] args = text.Split(' ');
                if (id.Equals(HostPrim.Owner) && args[0].ToUpper().Equals("SAVE")) {
                    if (args.Length > 1) {
                        HostPrim.Say("Saving topology as " + args[1]);
                        Topology.SaveTopology(name, id, args[1]);
                    } else {
                        HostPrim.Say("Showing Dialog");
                        SaveDialog save = new SaveDialog(HostPrim, primFactory, "Topology", topologyDefault, user => Topology.GetFolder(god));
                        save.OnSave += (userName, userID, file) => Topology.SaveTopology(name, id, file);
                        save.Show(name, id);
                    }
                }
            };

            primFactory.OnChat += _listener;
        }

        protected override IState MakeState(IKeyTableFactory tableFactory, IConfigSource config) {
            return new CreateTopologyState(this);
        }

        public override void Stop() {
            Factory.OnChat -= _listener;
            base.Stop();
        }
    }
}
