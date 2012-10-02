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
using common.framework.interfaces.entities;
using System.Drawing;
using common.framework.impl.util;
using common.framework.interfaces.basic;
using Diagrams.MRM.Controls;
using Diagrams.Control.impl.Util;
using System.Threading;
using Diagrams.Control.Impl.Controls.Buttons;
using Diagrams.Control.Impl.Util;
using Diagrams.Control.Impl.Util.State;

namespace Diagrams.Control.Impl.Module {
    public class SequenceControl : Control {
        private const string SEQUENCE_KEY = "Sequence";
        private const string GOD_KEY = "SequenceOwner";
        private const string TABLE_COUNT = "TableCount";

        private const string GOD = ".";

        private INode _selected;

        public SequenceControl(IKeyTableFactory tableFactory, IAsynchQueueFactory queueFactory, IPrimFactory primFactory, IModel model, IConfigSource config)
            : base(tableFactory, queueFactory, primFactory, model, config) {

            IConfig controlConfig = config.Configs[CONTROL_CONFIG];
            if (controlConfig == null)
                controlConfig = config.Configs[0];

            Init(controlConfig, HostPrim.Owner, this, Factory);
        }

        public static void Init(IConfig controlConfig, UUID godID, Control control, IPrimFactory factory) {
            factory.AddLinkSetRoot(factory.Host.ID);

            string userFolder = controlConfig.Get("UserFolder", ".");
            string godName = controlConfig.GetString(GOD_KEY, GOD);
            string sequenceFolder = controlConfig.Get("SequenceFolder", ".");
            string sequence = controlConfig.GetString(SEQUENCE_KEY);

            if (sequence == null) {
                control.HostPrim.Say("Unable to start sequence control. No sequence file specified.");
                throw new Exception("Unable to start sequence control. No sequence file specified.");
            }
            string location = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.Combine(userFolder, Path.Combine(godName, Path.Combine(sequenceFolder, sequence))));
            if (!File.Exists(location)) {
                control.HostPrim.Say("Unable to start sequence control. Sequence File '" + location + "' does not exist.");
                throw new Exception("Unable to start sequence control. Sequence File '" + location + "' does not exist.");
            }

            IButton PlayButton = control.MakeButton("Play");
            IButton PauseButton = control.MakeButton("Pause");
            IButton StopButton = control.MakeButton("Stop");
            IButton StepButton = control.MakeButton("Step");
            IToggle PauseToggle = new Toggle(PauseButton, 1, control.ToggleGlow);

            foreach (var pause in PauseToggle.Prims)
                pause.Colour = Color.White;

            PlayButton.OnTouched += (source, args) => {
                if (!control.Record.PlayingSequence) {
                    control.Clear(godName, godID);
                    control.Record.PlayRecording(godName, godID, sequence);
                }
            };
            StopButton.OnTouched += (source, args) => {
                control.Record.StopPlayback();
                control.Clear(godName, godID);
                control.HostPrim.Say("Stopped playback.");
            };
            PauseToggle.OnToggled += (source, args) => {
                control.Record.Paused = PauseToggle.IsOn;
                foreach (var prim in PauseToggle.Prims) {
                    prim.Glow = PauseToggle.IsOn ? .1d : 0d;
                    prim.Colour = Color.White;
                }
            };
            StepButton.OnTouched += (source, args) => {
                if (!control.Model.Step() && control.Record.PlayingSequence)
                    control.Record.PlayNextEvent();
            };
        }

        public static Parameters MakeParameters(Parameters parameters) {
            if (parameters == null)
                parameters = new Parameters();
            parameters.Set<bool>("Lock", true);
            return parameters;
        }

        public override ILink AddLink(UUID from, UUID to, Parameters parameters, float weight = default(float), bool bidirectional = true) {
            ILink link = base.AddLink(from, to, parameters, weight, bidirectional);
            link.OnWorldTouch += (source, args) => link.Say("Weight: " + link.Weight);
            return link;
        }

        public override INode AddNode(string name, Parameters parameters, Vector3 position = default(Vector3), Color colour = default(Color)) {
            INode node = base.AddNode(name, MakeParameters(parameters), position, colour);
            if (!parameters.Get<bool>("IsEP")) {
                node.OnWorldTouch += (source, args) => {
                    if (!DisplayForwardingTable(node.ID, "Blah", UUID.Zero)) {
                        if (_selected != null) {
                            _selected.Selected = 0d;
                            _selected = null;
                        }
                        Model.VisualiseRouting(source, new Parameters());
                    }
                };
            } else {
                node.OnWorldTouch += (source, args) => {
                    if (DisplayForwardingTable(node.ID, "Blah", UUID.Zero) && _selected == null) {
                        _selected = node;
                        _selected.Selected = ToggleGlow;
                    } else if (_selected != null) {
                        if (!_selected.ID.Equals(source)) {
                            UUID from = _selected.ID;
                            UUID to = source;
                            Thread sendThread = new Thread(() => {
                                for (int packet = 0; packet < MultiSendNumber; packet++) {
                                    if (_stopped || !IsNode(from) || !IsNode(to))
                                        break;
                                    Model.Send(from, to, new Parameters());
                                    JM726.Lib.Static.Util.Wait(MultiSendWait);
                                }
                            });
                            sendThread.Name = name + " send thread.";
                            sendThread.Start();
                        }
                        _selected.Selected = 0d;
                        _selected = null;
                    }
                };
            }
            return node;
        }

        protected override IState MakeState(IKeyTableFactory tableFactory, IConfigSource config) {
            return new SharedState(this);
        }

        protected override ITopologyManager MakeTopology(IConfig config) {
            return new SequenceTopologyManager(config, this);
        }

        private bool _stopped = false;

        public override void Stop() {
            base.Stop();
            _stopped = true;
        }
    }
}
