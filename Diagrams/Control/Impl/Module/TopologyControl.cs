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
using common.framework.impl.util;
using OpenMetaverse;
using common.framework.interfaces.entities;
using System.Drawing;
using Diagrams.MRM.Controls.Buttons;
using Nini.Config;
using common.interfaces.entities;
using common.Queue;
using Diagrams.Common.interfaces.keytable;
using Diagrams.Control.impl.Util;
using System.IO;

namespace Diagrams.Control.Impl.Module {
    public class TopologyControl : Control {
        private const string TOPOLOGY_KEY = "Topology";
        private const string GOD_KEY = "TopologyOwner";

        private const string GOD = ".";

        private readonly bool _reposition;
        private readonly Vector3 _hostPos;
        private readonly Quaternion _hostRot;

        public TopologyControl(IKeyTableFactory tableFactory, IAsynchQueueFactory queueFactory, IPrimFactory primFactory, IModel model, IConfigSource config)
            : base(tableFactory, queueFactory, primFactory, model, config) {


            IConfig control = config.Configs[CONTROL_CONFIG];
            if (control == null)
                control = config.Configs[0];

            _reposition = !control.GetBoolean("RotateWithHost", true) && control.GetBoolean("Reposition", true);
            _hostPos = HostPrim.Pos;
            _hostRot = HostPrim.Rotation;

            string topology = control.GetString(TOPOLOGY_KEY);
            string godName = control.GetString(GOD_KEY, GOD);

            if (topology == null) {
                HostPrim.Say("Unable to start topology control. No topology file specified.");
                throw new Exception("Unable to start topology control. No topology file specified.");
            }
            Topology.LoadTopology(godName, HostPrim.Owner, topology);
            HostPrim.Say("Started Topology Control.");

            Factory.AddLinkSetRoot(Factory.Host.ID);
            IButton PauseButton = MakeButton("Pause");
            IButton StepButton = MakeButton("Step");
            IToggle PauseToggle = new Toggle(PauseButton, 1, ToggleGlow);

            foreach (var pause in PauseToggle.Prims)
                pause.Colour = Color.White;

            PauseToggle.OnToggled += (source, args) => {
                Record.Paused = PauseToggle.IsOn;
                foreach (var prim in PauseToggle.Prims) {
                    prim.Glow = PauseToggle.IsOn ? .1d : 0d;
                    prim.Colour = Color.White;
                }
            };
            StepButton.OnTouched += (source, args) => Model.Step();
        }

        public override INode AddNode(string name, Parameters parameters, Vector3 position = default(Vector3), Color colour = default(Color)) {
            if (_reposition) {
                Matrix4 x = Matrix4.CreateFromQuaternion(_hostRot);
                Vector3 diff = position - _hostPos;

                position = Vector3.Transform(diff, x) + _hostPos;
            }
            if (parameters == null)
                parameters = new Parameters();
            parameters.Set<bool>("Lock", true);
            INode node = base.AddNode(name, parameters, position, colour);
            node.OnWorldTouch += (source, args) => {
                if (!DisplayForwardingTable(node.ID, "Blah", UUID.Zero))
                    Model.VisualiseRouting(source, new Parameters());
            };
            return node;
        }
    }
}