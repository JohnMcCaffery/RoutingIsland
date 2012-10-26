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
using OpenMetaverse;
using Diagrams.Common.interfaces.keytable;
using common.Queue;
using common.interfaces.entities;
using Nini.Config;
using Diagrams.MRM.Controls.Buttons;
using System.IO;
using common.framework.impl.util;
using System.Drawing;
using common.framework.interfaces.entities;
using Diagrams.Control.impl.Controls.Dialogs;
using Diagrams.Control.Impl.Util;
using Diagrams.Control.Impl.Util.State;
using Diagrams.Control.Impl.Controls.Buttons;

namespace Diagrams.Control.Impl.Module {
    public class CreateSequenceControl : SandboxControl {
        private ChatDelegate _listener;

        private const string TOPOLOGY_KEY = "Topology";
        private const string SEQUENCE_KEY = "Sequence";
        private const string GOD_KEY = "SequenceOwner";

        private const string SEQUENCE = "Sequence";
        private const string GOD = ".";

        public CreateSequenceControl(IKeyTableFactory tableFactory, IAsynchQueueFactory queueFactory, IPrimFactory primFactory, IModel model, IConfigSource config)
            : base(tableFactory, queueFactory, primFactory, model, config) {

            IConfig controlConfig = config.Configs["Control"];

            string god = controlConfig.Get(GOD_KEY, GOD);
            string topology = controlConfig.Get(TOPOLOGY_KEY, null);
            string sequenceDefault = controlConfig.Get(SEQUENCE_KEY, SEQUENCE);

            if (topology != null)
                Topology.LoadTopology(god, Factory.Host.Owner, topology);

            _listener = (name, id, text, channel) => {
                string[] args = text.Split(' ');
                if (id.Equals(HostPrim.Owner) && args[0].ToUpper().Equals("SAVE")) {
                    if (args.Length > 1) {
                        HostPrim.Say("Saving sequence as " + args[1]);
                        if (topology != null)
                            Record.SaveRecording(god, args[1], topology);
                        else
                            Record.SaveRecording(god, args[1]);
                    } else {
                        HostPrim.Say("Showing Dialog");
                        SaveDialog save = new SaveDialog(HostPrim, primFactory, "Sequence", sequenceDefault, user => Record.GetFolder(god));
                        save.OnSave += (user, userID, file) => {
                            if (topology == null)
                                Record.SaveRecording(god, file);
                            else
                                Record.SaveRecording(god, file, topology);
                        };
                        save.Show(name, id);
                    }
                }
            };

            primFactory.OnChat += _listener;

            Record.StartRecording();
        }

        public override void Stop() {
            Factory.OnChat -= _listener;
            base.Stop();
        }
    }
}