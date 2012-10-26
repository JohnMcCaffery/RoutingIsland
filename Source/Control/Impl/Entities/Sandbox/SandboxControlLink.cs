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
using common.framework.interfaces.entities;
using Diagrams.Control.impl.Module;
using OpenMetaverse;
using Diagrams.Common;
using Diagrams.Common.interfaces.keytable;
using Diagrams.Control.Impl.Module;
using Diagrams.Control.impl.Controls.Dialogs;
using common.interfaces.entities;
using Diagrams.Control.impl.Buttons.ControlButtons;
using common.framework.impl.util;
using Diagrams.MRM.Controls;

namespace Diagrams.Control.impl.Entities {
    class SandboxControlLink : ControlLink {

        private const string MED_DECREMENT = "-= .05";

        private const string MED_INCREMENT = "+= .05";

        private const string SMALL_DECREMENT = "-= .01";

        private const string SMALL_INCREMENT = "+= .01";

        public const string RANDOM = "Random";

        public const string UNIT = "Unit";

        public const string LENGTH = "Length";

        private const string LARGE_INCREMENT = "+= .1";

        private const string LARGE_DECREMENT = "-= .1";

        private const string DELETE = "Delete";

        private static readonly string[] LINK_BUTTONS = { SMALL_INCREMENT, MED_INCREMENT, LARGE_INCREMENT, SMALL_DECREMENT, MED_DECREMENT, LARGE_DECREMENT, UNIT, RANDOM, LENGTH, Dialog.OK, Dialog.CANCEL, DELETE };

        #region Static

        private const float UNIT_WEIGHT = 1f;

        public static LinkType DefaultWeight {
            get { return _defaultWeight; }
            set { _defaultWeight = value; }
        }

        private static Random _random = new Random();

        private static LinkType _defaultWeight = LinkType.Length;

        private readonly static Dictionary<UUID, LinkType> _types = new Dictionary<UUID, LinkType>();

        public static void SetType(string name, UUID id, LinkType type, IControlUtil control, Parameters parameters) {
            if (_types.ContainsKey(id) && type != LinkType.Random && _types[id] == type)
                return;
            if (!_types.ContainsKey(id))
                _types.Add(id, type);
            else
                _types[id] = type;

            control.ForAllLinks(name, id, link => {
                link.Parameters.Append(parameters);
                link.Weight = GetWeight(type);
            });
        }

        public static float GetWeight(UUID id) {
            if (!_types.ContainsKey(id))
                _types.Add(id, DefaultWeight);
            return GetWeight(_types[id]); 
        }

        public static float GetWeight(LinkType type) {
            switch (type) {
                case LinkType.Random: return (float)_random.NextDouble();
                case LinkType.Unit: return UNIT_WEIGHT;
                default: return default(float);
            }
        }

        #endregion

        private readonly SandboxControl _control;

        private readonly IKeyTable<double> _bufferedChanges;

        private readonly Dialog _dialog;
        
        protected new SandboxControl Control {
            get { return _control; }
        }

        public SandboxControlLink(ILink link, INode from, INode to, SandboxControl control, IKeyTableFactory tableFactory, IPrimFactory primFactory, IPermissions permissions)
            : base(link, from, to, control, permissions) {
            
            
            _control = control;
            _bufferedChanges = tableFactory.MakeKeyTable<double>();
            _dialog = new Dialog(control.HostPrim, primFactory, LINK_BUTTONS);
            _dialog.ResponseReceived += DialogPressed;
        }

        protected override void OnDeleted() {
            Control.RemoveLink(ID, GetParameters(Owner));
        }

        protected override void Touched(UUID entity, TouchEventArgs args) {
            if (!Authorize(args.AvatarName, args.AvatarID))
                return;

            switch (Control.State.GetState(args.AvatarName, args.AvatarID)) {
                case Impl.Module.Control.NothingSelected:
                    OnSelected(args.AvatarName, args.AvatarID);
                    break;
                case SandboxControl.RouterSelected:
                    ILogicEntity selectedRouter = Control.State.GetSelectedEntity(args.AvatarName, args.AvatarID);
                    selectedRouter.Selected = 0d;

                    OnSelected(args.AvatarName, args.AvatarID);
                    break;
                case SandboxControl.EPSelected:
                    ILogicEntity selectedEP = Control.State.GetSelectedEntity(args.AvatarName, args.AvatarID);
                    selectedEP.Selected = 0d;

                    OnSelected(args.AvatarName, args.AvatarID);
                    break;
                case SandboxControl.LinkSelected:
                    ILogicEntity selectedLink = Control.State.GetSelectedEntity(args.AvatarName, args.AvatarID);
                    if (selectedLink.ID.Equals(entity)) {
                        Control.State.ResetState(args.AvatarName, args.AvatarID);
                    } else {
                        selectedLink.Selected = 0d;
                        Control.State.SetSelectedPrim(ID, args.AvatarName, args.AvatarID);
                    }
                    break;
                case SandboxControl.VisualiseAlgorithmSelected:
                    Control.State.ResetState(args.AvatarName, args.AvatarID);
                    Control.State.SetState(SandboxControl.LinkSelected, args.AvatarName, args.AvatarID);
                    Control.State.SetSelectedPrim(ID, args.AvatarName, args.AvatarID);
                    break;
                case SandboxControl.DeleteSelected:
                    Control.Record.RemoveLink(ID, GetParameters(args.AvatarID));
                    break;
                case SandboxControl.DisplayTableSelected:
                    //Do Nothing
                    break;
            }
        }

        protected Parameters GetParameters(UUID id) {
            return new Parameters("Visualise", Control.State.GetToggleState(SandboxControl.ShowUpdatePackets, id));
        }

        private void OnSelected(string name, UUID id) {
            Control.State.SetState(SandboxControl.LinkSelected, name, id);
            Control.State.SetSelectedPrim(ID, name, id);

            if (!_bufferedChanges.ContainsKey(id))
                _bufferedChanges.Add(id, Weight);
            else 
                _bufferedChanges[id] = Weight;
            ShowLinkDialog(name, id);
        }

        private void DialogPressed(string name, UUID id, string text, string chat) {
            if (Control.State.GetState(name, id) != SandboxControl.LinkSelected)
                return;
            switch (text) {
                case SMALL_INCREMENT: BufferWeightChange(.01d, name, id); break;
                case MED_INCREMENT: BufferWeightChange(.05d, name, id); break;
                case LARGE_INCREMENT: BufferWeightChange(.1d, name, id); break;
                case SMALL_DECREMENT: BufferWeightChange(-.01d, name, id); break;
                case MED_DECREMENT: BufferWeightChange(-.05d, name, id); break;
                case LARGE_DECREMENT: BufferWeightChange(-.1d, name, id); break;

                case UNIT:
                    float unitWeight = GetWeight(LinkType.Unit);
                    ChangeWeight(unitWeight, name, id);
                    Control.State.ResetState(name, id);
                    Say("Set weight to " + unitWeight);
                    break;
                case RANDOM:
                    float randomWeight = GetWeight(LinkType.Random);
                    ChangeWeight(randomWeight, name, id);
                    Say("Set weight to " + randomWeight);
                    Control.State.ResetState(name, id);
                    break;
                case LENGTH:
                    DistanceWeight = true;
                    Control.State.ResetState(name, id);
                    Say("Set weight to " + Weight);
                    break;
                case Dialog.OK:
                    double weight;
                    if (chat != null && double.TryParse(chat, out weight))
                        BufferWeightChange(weight, name, id);
                    else if (Weight != _bufferedChanges[id]) {
                        ChangeWeight((float)_bufferedChanges[id], name, id);
                        Say("Set weight to " + Weight);
                    }
                    Control.State.ResetState(name, id);
                    break;
                case DELETE:
                    Control.State.ResetState(name, id);
                    Control.Record.RemoveLink(ID, GetParameters(id));
                    break;
                case Dialog.CANCEL:
                    Control.State.ResetState(name, id);
                    break;
                default: /*Do Nothing*/ break;
            }
        }

        private void ChangeWeight(float weight, string name, UUID id) {
            Parameters.Set<bool>("Visualise", Control.State.GetToggleState(SandboxControl.ShowUpdatePackets, id));
            DistanceWeight = false;
            Weight = weight;
        }


        private void BufferWeightChange(double change, string name, UUID id) {
            _bufferedChanges[id] += change;
            if (_bufferedChanges[id] > 1f)
                _bufferedChanges[id] = 1f;
            else if (_bufferedChanges[id] <= 0f)
                _bufferedChanges[id] = .01f;
            ShowLinkDialog(name, id);
        }

        private void ShowLinkDialog(string name, UUID id) {
            _dialog.Show(Name, id, Name + " weight = " + Math.Round(_bufferedChanges[id], 2) + "." +
                "\nTo set an arbitrary weight type '/" + Dialog.ChatChannel + " WEIGHT' then press Ok." +
                "\nOtherwise use the buttons provided.");
        }
    }
}
