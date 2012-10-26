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

using Diagrams.Common.interfaces.keytable;
using common.Queue;
using common.interfaces.entities;
using OpenMetaverse;
using common.framework.interfaces.basic;
using common.framework.impl.util;
using common.framework.interfaces.entities;
using System.Xml;
using System;
using System.Collections.Generic;
using common.config;
using System.Drawing;
using System.Threading;
using common;
using Diagrams.Common;
using Nini.Config;
using Diagrams.MRM;
using System.IO;
using System.Linq;
using StAndrews.NetworkIsland.Control;
using JM726.Lib.Static;
using Diagrams.Control.impl.Entities;
using Diagrams.Control.impl.Util;
using Diagrams.Control.impl.Buttons.ControlButtons;
using Diagrams.Control.impl.Controls.Dialogs;
using Diagrams.Control.impl.Buttons;
using Diagrams.MRM.Controls.Buttons;
using Diagrams.Control.Impl.Controls.Buttons;

namespace Diagrams.Control.Impl.Module {
    public class SandboxControl : Control {
        public const string EPMode = "EndPointMode";
        public const string Build = "Build";
        public const string ShowUpdatePackets = "ShowUpdatePackets";
        public const string SendMultiplePackets = "SendMultiplePackets";

        public const string RouterSelected = "RouterSelected";
        public const string EPSelected = "EPSelected";
        public const string LinkSelected = "LinkSelected";
        public const string VisualiseAlgorithmSelected = "VisualiseAlgorithmSelected";
        public const string DeleteSelected = "DeleteSelected";
        public const string DisplayTableSelected = "DisplayTableSelected";
        public const string IsEP = "IsEP";

        public const string VISUALISE = "Visualise";
        public const string MULTI_SEND = "MultiSend";

        private int TimeInc {
            get {
                //int change = 50;
                //while (Wait - change <= 0)
                //    change /= 2;
                //return change;
                return Wait > 4 ? Wait / 4 : 1;
            }
        }

        //Authorization

        //IsOn
        private IKeyTableFactory _tableFactory;
        
        //Misc
        private readonly bool _reposition;
        private static int _routerCount;
        private static int _epCount;

        #region Constructors

        public SandboxControl(IKeyTableFactory tableFactory, IAsynchQueueFactory queueFactory, IPrimFactory primFactory, IModel model, IConfigSource config)
            : base(tableFactory, queueFactory, primFactory, model, config) {

            _tableFactory = tableFactory;

            //IsOn
                                        
            int chatChannel = ControlConfig.GetInt("PublicInputChannel", 46);            
            if (chatChannel < 0)
                chatChannel = 46;
            Dialog.SetChatChannel(chatChannel);

            //Misc
            _reposition = ControlConfig.GetBoolean("RepositionControls", false);
            float scale = ControlConfig.GetFloat("DefaultScale", .5f);

            
            if (ControlConfig.GetBoolean("AutoRecord", false))
                Record.StartRecording();

            InitControls();
        }

        protected virtual void InitControls() {
            //Basic Toggles
            UUID owner = HostPrim.Owner;
            Factory.AddLinkSetRoot(HostPrim.ID);
            State.AddToggle(owner, MakeToggle(EPMode));
            State.AddToggle(owner, MakeToggle(Build));
            State.AddToggle(owner, MakeToggle(SendMultiplePackets));
            State.AddToggle(owner, MakeToggle(ShowUpdatePackets));

            //Semi Toggles
            new Record(MakeToggle("Record"), this, HostPrim, Factory, ControlConfig);
            new Pause(MakeToggle("Pause"), this, HostPrim, Factory, ControlConfig);

            //State Buttons
            State.AddStateButton(owner, new Delete(MakeButton("Delete"), this, owner));
            State.AddStateButton(owner, new Play(MakeButton("PlayAlgorithm"), this, owner));

            //Buttons
            new Clear(MakeButton("Clear"), this, Factory);
            new Create(MakeButton("CreateNode"), this, Factory);
            new Open(MakeButton("Load"), this, Factory);
            new Save(MakeButton("Save"), this, Factory, ControlConfig);
            new Faster(MakeButton("Faster"), this, HostPrim);
            new Slower(MakeButton("Slower"), this, HostPrim);
            new Step(MakeButton("Step"), this);
            new ModifyLinks(MakeButton("ModifyLinks"), this, Factory);
            new ChangeAlg(MakeButton("ChangeAlg"), this, Factory);

            //Floor
            new Floor(MakeButton("Floor"), this);

            //Display button
            //_forwardingTableButton = new RoutingTable(MakeButton("TableDisplayHUD"), this, Factory);
        }

        #endregion

        #region Entities
        
        protected override IControlLink MakeLink(UUID from, UUID to, Parameters parameters, float weight = default(float), bool bidirectional = true) {
            Logger.Debug("Creating link between '" + GetNode(from).Name + "' and '" + GetNode(to).Name + "'.");
            UUID creator = parameters.Get<UUID>(Control.OwnerID);
            ILink l = Model.AddLink(from, to, parameters, weight, bidirectional);
            IControlLink link = new SandboxControlLink(l, GetNode(from), GetNode(to), this, _tableFactory, Factory, Permissions);
            Logger.Debug("Created  '" + link.Name + "'.");
            return Record.MakeMapped<IControlLink>(link);
        }

        protected override IControlNode MakeNode(string name, Parameters parameters, Vector3 position = default(Vector3), Color colour = default(Color)) {
            Logger.Debug("Creating node '" + name + "'.");
            UUID ownerID = parameters.Get<UUID>(Control.OwnerID);
            bool isEP = parameters.Get<bool>(SandboxControl.IsEP);
            INode n = Model.AddNode(name, parameters, position, colour);
            IControlNode node = null;
            if (isEP)
                node = new SandboxControlEP(n, position, this, ControlConfig, Permissions);
            else
                node = new SandboxControlRouter(n, position, this, Permissions);
            Logger.Info("Created node '" + name + "'.");

            return RecordNode(node);
        }

        protected override ITopologyManager MakeTopology(IConfig config) {
            return new SandboxTopologyManager(config, this);
        }

        #region EP

        public virtual INode AddEP(string creator, UUID creatorID, Vector3 pos) {
            return AddEP("EP" + ++_epCount, creator, creatorID, pos, GetColour(_epCount + _routerCount)); 
        }

        public virtual INode AddEP(string primName, string creator, UUID creatorID, Vector3 pos, Color colour) {
            return AddNode(primName, new Parameters("IsEP", true), creator, creatorID, pos, colour);
        }

        #endregion

        #region Router

        public virtual INode AddRouter(string creator, UUID creatorID, Vector3 pos) {
            return AddRouter("Router" + ++_routerCount, creator, creatorID, pos, GetColour(_epCount + _routerCount)); 
        }

        public virtual INode AddRouter(string primName, string creator, UUID creatorID, Vector3 pos, Color colour) {
            return AddNode(primName, new Parameters("IsEP", false), creator, creatorID, pos, colour);
        }

        #endregion

        #endregion

        #region Controls

        #region Chat


        //private void CommandListener(string name, UUID id, string text) {
        //    string[] command = text.Split(new char[] { ' ' }, 3);
        //    if (command.Length > 0 && command[0].ToUpper().Equals("CLEAR"))
        //        Clear(name, id);
        //    if (command.Length > 1) {
        //        if (command[0].ToUpper().Equals("START") && command[1].ToUpper().StartsWith("REC"))
        //            StartRecording();
        //        else if (command[0].ToUpper().Equals("STOP") && command[1].ToUpper().StartsWith("REC"))
        //            StopRecording();
        //        else if (command[0].ToUpper().Equals("PLAY"))
        //            PlayRecording(name, id, command[1]);
        //    }
        //    if (command.Length > 2) {
        //        if (command[0].ToUpper().StartsWith("SET")) {
        //            if (command[1].ToUpper().Equals("WEIGHT") && GetState(name, id) == States.LinkSelected) {
        //                float weight;
        //                if (float.TryParse(command[2], out weight)) {
        //                    GetLink(GetSelectedEntity(name, id).ID).Parameters.Set<bool>("Visualise", GetToggleState(Toggles.ShowUpdatePackets, name, id));
        //                    GetLink(GetSelectedEntity(name, id).ID).Weight = weight;
        //                } else
        //                    HostPrim.Say("Unable to parse '" + command[2] + "' to a valid weight.");
        //                ResetState(name, id);
        //            } else if (command[1].ToUpper().StartsWith("ALG")) {
        //                try {
        //                    SetAlgorithm(command[2], name, id);
        //                    HostPrim.Say("Set algorithm to " + command[2]);
        //                } catch (Exception e) {
        //                    HostPrim.Say(e.Message);
        //                }
        //            }
        //        } else if (command[0].ToUpper().Equals("SAVE")) {
        //            if (command[1].ToUpper().StartsWith("TOP"))
        //                SaveTopology(name, command[2]);
        //            else if (command[1].ToUpper().StartsWith("SEQ") || command[1].ToUpper().StartsWith("REC"))
        //                SaveRecording(name, command[2]);
        //        } else if (command[0].ToUpper().Equals("LOAD")) {
        //            if (command[1].ToUpper().StartsWith("TOP"))
        //                LoadTopology(command[2], name, id);
        //            else if (command[1].ToUpper().StartsWith("SEQ") || command[1].ToUpper().StartsWith("REC"))
        //                PlayRecording(name, id, command[2]);
        //        }
        //    }
        //}

        #endregion

        #endregion

        #region Util

        #endregion
    }
}