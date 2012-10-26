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
using System.Drawing;
using OpenMetaverse;
using common.framework.impl.util;
using common.framework.interfaces.basic;
using common.framework.interfaces.entities;
using common.framework.interfaces.layers;
using common.interfaces.entities;
using Diagrams.Common.interfaces.keytable;
using common.Queue;
using System.Xml;
using System.Globalization;
using log4net;
using common.config;
using System.IO;
using System.Xml.Schema;
using StAndrews.NeworkIsland.Framework.Util.Logger.Opensim;
using Diagrams.Common;
using jm726.lib.wrapper.logger;
using StAndrews.NetworkIsland.Control;
using common;
using Nini.Config;
using System.Threading;
using jm726.lib.Serialization;
using System.Linq;
using System.Diagnostics;
using JM726.Lib.Static;
using Diagrams.Control.impl.Entities;
using Diagrams.Control.impl.Module;
using Diagrams.Control.impl.Util;
using Diagrams.MRM.Controls;
using Diagrams.MRM.Controls.Buttons;
using Diagrams.Control.Impl.Util;
using Diagrams.Control.Impl.Util.State;
using Diagrams.Control.Impl.Controls.Buttons;

namespace Diagrams.Control.Impl.Module {

    public abstract class Control : AbstractModule<IControlNode, IControlLink>, IControl, IControlUtil {
        public const string OwnerID = "OwnerID";
        public const string Owner = "Owner";
        public const string NothingSelected = "NothingSelected";

        protected const string CONTROL_CONFIG = "Control";
        protected const string TABLE_KEY = "TableName";
        protected const string TABLE_HUD_KEY = "TableHUDName";
        protected const string TABLE_COUNT = "TableCount";

        private double _toggleGlow;

        private double _fade;

        private bool _stopped = false;

        private readonly int _multiNumber;

        private readonly int _multiSendDelay;

        private readonly string _defaultTopologyName;

        private readonly string _defaultRecordingName;

        public double Fade { get { return _fade; } }

        public double ToggleGlow { get { return _toggleGlow; } }

        public int MultiSendNumber {
            get { return _multiNumber; }
        }

        public int MultiSendWait {
            get { return _multiSendDelay; }
        }

        public string DefaultRecordingName {
            get { return _defaultRecordingName; }
        }

        public string DefaultTopologyName {
            get { return _defaultTopologyName; }
        }

        #region Private Fields

        private readonly ISequenceManager _record;

        private readonly ITopologyManager _topology;

        private readonly IState _state;

        private readonly IModel _model;
        private readonly IPrimFactory _factory;
        private readonly IPermissions _permissions;
        private readonly List<RoutingTable> _tables;


        private int _wait;

        #endregion

        #region Protected fields

        //TODO get + set god + Name
        protected readonly UUID _godID = UUID.Random();
        protected readonly string _godName = "Routing Project";

        #endregion

        #region Protected Properties

        public IPermissions Permissions {
            get { return _permissions; }
        }

        public IPrim HostPrim {
            get { return _factory.Host; }
        }

        public IModel Model {
            get { return _model; }
        }

        /// <summary>
        /// The prim factory which can be used to create primitives or listen for chat events.
        /// </summary>
        protected IPrimFactory Factory {
            get { return _factory; }
        }

        protected readonly IConfig ControlConfig;

        #endregion

        #region Constructor

        public Control(IKeyTableFactory tableFactory, IAsynchQueueFactory queueFactory, IPrimFactory primFactory, IModel model, IConfigSource config)
            : base(tableFactory, queueFactory) {
            

            ControlConfig = config.Configs[CONTROL_CONFIG];
            IConfig commonConfig = config.Configs["Common"];
            if (ControlConfig == null)
                ControlConfig = config.Configs[0];
            if (commonConfig == null)
                commonConfig = config.Configs[0];

            _toggleGlow = ControlConfig.GetDouble("ToggleGlow", .15);
            _fade = ControlConfig.GetDouble("Fade", .8);
            _defaultRecordingName = ControlConfig.Get("DefaultSequenceName", "Sequence");
            _defaultTopologyName = ControlConfig.Get("DefaultTopologyName", "Topology");
            _multiNumber = ControlConfig.GetInt("MultiSendNumber", 10);
            _multiSendDelay = ControlConfig.GetInt("MultiSendDelay", 750);
            _wait = commonConfig.GetInt("Wait", 50);

            _factory = primFactory;

            _record = MakeRecord(ControlConfig, tableFactory, queueFactory, primFactory);
            _topology = MakeTopology(ControlConfig);
            _permissions = MakePermissions(tableFactory, config);
            _state = MakeState(tableFactory, config);
            _model = Record.Make<IModel>(model, false);

            string tableName = ControlConfig.Get(TABLE_KEY, null);
            int tableCount = ControlConfig.GetInt(TABLE_COUNT, 1);
            _tables = new List<RoutingTable>();
            if (tableName != null && tableCount > 1) {
                for (int i = 1; i <= tableCount; i++)
                    _tables.Add(new RoutingTable(Factory.MakeButton(tableName + i, Permissions), this, Factory));
            } else if (tableName != null) 
                _tables.Add(new RoutingTable(Factory.MakeButton(tableName, Permissions), this, Factory));
            _tables.Add(new RoutingTable(Factory.MakeButton(ControlConfig.Get(TABLE_HUD_KEY, "TableDisplayHUD"), Permissions), this, Factory)); 

            Logger.Info("Control started.");
        }

        public IButton MakeButton(string button) {
            button = ControlConfig.Get(button + ".Name", button);
            return Factory.MakeButton(button, Permissions);
        }

        public Toggle MakeToggle(string toggle) {
            bool startSelected = ControlConfig.GetBoolean(toggle + ".StartSelected", false);
            return new Toggle(MakeButton(toggle), Fade, ToggleGlow, startSelected);
        }

        #endregion

        protected virtual IPermissions MakePermissions(IKeyTableFactory tableFactory, IConfigSource config) {
            return new FreePermissions();
        }

        protected virtual IState MakeState(IKeyTableFactory tableFactory, IConfigSource config) {
            return new SharedState(this);
        }

        #region IModule Members

        public override bool Paused {
            get { return _model.Paused; }
            set { _model.Paused = value; }
        }

        /// <summary>
        /// Perform the specified action on every node mapped by the module.
        /// </summary>
        /// <param name="doThis">The delegate function to be performed on each node.</param>
        public void ForAllNodes(string name, UUID id, Action<IControlNode> doThis) {
            ForAllNodes(node => {
                if (node.Authorize(name, id))
                    doThis(node);
            });
        }

        /// <summary>
        /// Perform the specified action on every link mapped by the module.
        /// </summary>
        /// <param name="doThis">The delegate function to be performed on each link.</param>
        public void ForAllLinks(string name, UUID id, Action<ILink> doThis) {
            ForAllLinks(link => {
                if (link.Authorize(name, id))
                    doThis(link);
                else
                    Console.WriteLine("Not authorized.");
            });
        }

        public override int Wait {
            get { return Model.Wait; }
            set { Model.Wait = value; }
        }

        public override void Stop() {
            Logger.Debug("Control stopping.");
            _stopped = true;
            Record.Stop();
            base.Stop();
            Model.Stop();
            Logger.Info("Control stopped.");
        }

        public override void Clear() {
            base.Clear();
            Model.Clear();
        }

        public override void Clear(params UUID[] nodes) {
            base.Clear(nodes);
            Model.Clear(nodes);
        }

        #endregion

        #region IControl Members

        public void Clear(string name, UUID id) {
            /*
            Queue.Paused = true;
            int queued = 0;
            int processed = 0;
            ForAllLinks(name, id, link =>  {
                Queue.QWork("Clear Link " + link.Name, () => {
                    Record.RemoveLink(link.ID, new Parameters("Clearing", true));
                    processed++;
                });
                queued++;
            });
            */
            //Queue.Paused = false;
            //Queue.BlockWhileWorking();
            //Console.WriteLine("Removed all links - {0} queued - {1} processed.", queued, processed);
            //Queue.Paused = true;

            List<UUID> nodeList = new List<UUID>();
            ForAllNodes(name, id, node => nodeList.Add(node.ID));
            UUID[] nodes = nodeList.ToArray();
            Logger.Info("Clearing " + nodes.Length + " nodes.");
            Record.Clear(nodes.ToArray());
            Model.Clear(nodes.ToArray());
            //Queue.Paused = false;
            //Queue.BlockWhileWorking();
        }

        #endregion

        #region IControlUtil Members

        public ISequenceManager Record {
            get { return _record; }
        }

        public ITopologyManager Topology {
            get { return _topology; }
        }

        public IState State {
            get { return _state; }
        }

        public virtual ILink AddLink(UUID from, UUID to, Parameters parameters, string name, UUID id, float weight = default(float), bool bidirectional = true) {
            parameters.Set<string>(Owner, name);
            parameters.Set<UUID>(OwnerID, id);
            return Record.AddLink(from, to, parameters, weight, bidirectional);
        }

        public virtual INode AddNode(string name, Parameters parameters, string ownerName, UUID ownerID, Vector3 position = default(Vector3), Color colour = default(Color)) {
            parameters.Set<string>(Owner, ownerName);
            parameters.Set<UUID>(OwnerID, ownerID);
            return Record.AddNode(name, parameters, position, colour);
        }
                
        protected override void RemoveLink(IControlLink link, Parameters parameters) {
            if (_stopped)
                return;
            base.RemoveLink(link, parameters);
            Model.RemoveLink(link.ID, parameters);
        }

        public override void RemoveNode(UUID node, Parameters parameters) {
            if (!IsNode(node) || _stopped)
                return;
            base.RemoveNode(node, parameters);
            Model.RemoveNode(node, parameters);
        }

        public bool DisplayForwardingTable(UUID node, string user, UUID userID) {
            if (State.GetState(user, userID).Equals(SandboxControl.DisplayTableSelected)) {
                _tables.ForEach(table => table.DisplayForwardingTable(node));
                State.ResetState(user, userID);
                return true;
            }
            return false;
        }

        //void IControlUtil.RemoveLink(UUID link, Parameters parameters) {
        //    Record.RemoveLink(link, parameters);
        //    Model.RemoveLink(link, parameters);
        //}

        //void IControlUtil.RemoveLink(UUID from, UUID to, Parameters parameters) {
        //    Record.RemoveLink(from, to, parameters);
        //    Model.RemoveLink(from, to, parameters);
        //}

        //void IControlUtil.RemoveNode(UUID node, Parameters parameters) {
        //    foreach (ILink l in GetLinks(node))
        //        RemoveLink(l.ID, parameters);
        //    Record.RemoveNode(node, parameters);
        //    Model.RemoveNode(node, parameters);
        //}


        #endregion

        #region AbstractModule implementations

        protected override IControlLink MakeLink(UUID from, UUID to, Parameters parameters, float weight = default(float), bool bidirectional = true) {
            Logger.Debug("Creating link between '" + GetNode(from).Name + "' and '" + GetNode(to).Name + "'.");
            IControlLink link = new ControlLink(_model.AddLink(from, to, parameters, weight, bidirectional), GetNode(from), GetNode(to), this, Permissions);
            Logger.Debug("Created  '" + link.Name + "'.");
            return Record.MakeMapped<IControlLink>(link);
        }

        protected override IControlNode MakeNode(string name, Parameters parameters, Vector3 position = default(Vector3), Color colour = default(Color)) {
            Logger.Debug("Creating node '" + name + "'.");
            return RecordNode(new ControlNode(_model.AddNode(name, parameters, position, colour), this, Permissions));
        }

        protected IControlNode RecordNode(IControlNode node) {
            node = Record.MakeMapped<IControlNode>(node);
            node.OnWorldMove += (entity, oldPosition, newPosition) => node.Pos = newPosition;
            return node;
        }

        #endregion

        public void Say(string msg) {
            Logger.Info(msg);
            HostPrim.Say(msg);
        }

        /// <summary>
        /// Override this to use the body of an xml element to load in parameters to a newly created node or link.
        /// </summary>
        /// <param name="node">The xml node to parse parameters from.</param>
        /// <returns>A parameters object representing any information stored in the body of node.</returns>
        public virtual Parameters MakeParameters(XmlNode node, string creator, UUID creatorID) {
            return new Parameters();
        }

        #region Util

        public static Color GetColour(int i) {
            
            switch (i % 9) {
                case 1:
                    return Color.Red;
                case 2:
                    return Color.OrangeRed;
                case 3:
                    return Color.Orange;
                case 4:
                    return Color.Yellow;
                case 5:
                    return Color.YellowGreen;
                case 6:
                    return Color.Green;
                case 7:
                    return Color.Blue;
                case 8:
                    return Color.BlueViolet;
                case 0:
                    return Color.Purple;
            }

            return Color.Orange;
        }

        #endregion

        protected virtual ISequenceManager MakeRecord(IConfig config, IKeyTableFactory tableFactory, IAsynchQueueFactory queueFactory, IPrimFactory primFactory) {
            return new SequenceManager(this, this, config, primFactory, tableFactory, Queue);
        }

        protected virtual ITopologyManager MakeTopology(IConfig config) {
            return new TopologyManager(config, this);
        }

    }
}