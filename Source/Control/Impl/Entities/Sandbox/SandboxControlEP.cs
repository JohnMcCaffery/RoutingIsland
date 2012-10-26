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
using OpenMetaverse;
using Diagrams.Control.Impl.Module;
using Diagrams.Control.impl.Buttons.ControlButtons;
using System.Threading;
using Nini.Config;
using Diagrams.Control.impl.Buttons;
using Diagrams.MRM.Controls;

namespace Diagrams.Control.impl.Entities {
    public class SandboxControlEP : SandboxControlNode {
        private bool _alive = true;

        public override bool Destroy() {
            _alive = false;
            return base.Destroy();
        }

        public SandboxControlEP(INode node, Vector3 pos, SandboxControl control, IConfig controlConfig, IPermissions permissions)
            : base(node, pos, control, permissions) {
        }

        private void Send(UUID root, UUID id) {
            if (!Control.State.GetToggleState(SandboxControl.SendMultiplePackets, id))
                Control.Model.Send(root, ID, GetParameters(id));
            else {
                Thread sendThread = new Thread(() => {
                    for (int packet = 0; packet < Control.MultiSendNumber; packet++) {
                        if (!_alive || !Control.IsNode(ID) || !Control.IsNode(root))
                            break;
                        Control.Model.Send(root, ID, GetParameters(id));
                        JM726.Lib.Static.Util.Wait(Control.MultiSendWait);
                    }
                });
                sendThread.Name = Name + " send thread.";
                sendThread.Start();
            }
        }

        protected override void Touched(UUID entity, TouchEventArgs args) {
            if (!Authorize(args.AvatarName, args.AvatarID))
                return;
            Logger.Debug(Name + " touched. Initial state " + Control.State.GetState(args.AvatarName, args.AvatarID) + ".");
            switch (Control.State.GetState(args.AvatarName, args.AvatarID)) {
                case Impl.Module.Control.NothingSelected:
                    Control.State.SetState(SandboxControl.EPSelected, args.AvatarName, args.AvatarID);
                    Control.State.SetSelectedPrim(ID, args.AvatarName, args.AvatarID);
                    break;
                case SandboxControl.RouterSelected:
                    UUID selectedRouter = Control.State.GetSelectedEntity(args.AvatarName, args.AvatarID).ID;
                    Control.State.ResetState(args.AvatarName, args.AvatarID);
                    if (!Control.AreLinked(selectedRouter, ID)) {
                        foreach (ILogicEntity otherEnd in Control.GetNeighbours(ID))
                            RemoveLink(otherEnd.ID, args.AvatarName, args.AvatarID);
                        AddLink(selectedRouter, args.AvatarName, args.AvatarID);
                    } else
                        RemoveLink(selectedRouter, args.AvatarName, args.AvatarID);
                    break;
                case SandboxControl.EPSelected:
                    UUID selectedEP = Control.State.GetSelectedEntity(args.AvatarName, args.AvatarID).ID;
                    if (!selectedEP.Equals(ID))
                        Send(selectedEP, args.AvatarID);
                    Control.State.ResetState(args.AvatarName, args.AvatarID);
                    break;
                case SandboxControl.LinkSelected:
                    ILogicEntity selectedLink = Control.State.GetSelectedEntity(args.AvatarName, args.AvatarID);
                    selectedLink.Selected = 0d;

                    Control.State.SetState(SandboxControl.EPSelected, args.AvatarName, args.AvatarID);
                    Control.State.SetSelectedPrim(ID, args.AvatarName, args.AvatarID);
                    break;
                case SandboxControl.VisualiseAlgorithmSelected:
                    VisualiseRouting(Control.State.GetSelectedEntity(args.AvatarName, args.AvatarID).ID, args.AvatarName, args.AvatarID);
                    break;
                case SandboxControl.DeleteSelected:
                    RemoveThis(args.AvatarName, args.AvatarID);
                    break;
                case SandboxControl.DisplayTableSelected:
                    DisplayForwardingTable(args.AvatarName, args.AvatarID);
                    break;
            }
            Logger.Debug(Name + " touched. New state " + Control.State.GetState(args.AvatarName, args.AvatarID) + ".");
        }
    }
}
