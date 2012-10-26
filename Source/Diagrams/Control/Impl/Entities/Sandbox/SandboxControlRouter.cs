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
using Diagrams.Control.impl.Buttons.ControlButtons;
using Diagrams.Control.Impl.Module;
using common.framework.interfaces.entities;
using Diagrams.MRM.Controls;

namespace Diagrams.Control.impl.Entities {
    public class SandboxControlRouter : SandboxControlNode {

        public SandboxControlRouter(INode node, Vector3 pos, SandboxControl control, IPermissions permissions)
            : base(node, pos, control, permissions) {
        }

        protected override void Touched(UUID entity, TouchEventArgs args) {
            if (!Authorize(args.AvatarName, args.AvatarID))
                return;

            Logger.Debug(Name + " touched. Initial state " + Control.State.GetState(args.AvatarName, args.AvatarID) + ".");
            switch (Control.State.GetState(args.AvatarName, args.AvatarID)) {
                case Impl.Module.Control.NothingSelected:
                    Control.State.SetState(SandboxControl.RouterSelected, args.AvatarName, args.AvatarID);
                    Control.State.SetSelectedPrim(ID, args.AvatarName, args.AvatarID);
                    break;
                case SandboxControl.RouterSelected:
                    UUID selectedRouter = Control.State.GetSelectedEntity(args.AvatarName, args.AvatarID).ID;
                    if (!selectedRouter.Equals(ID) && !Control.AreLinked(selectedRouter, ID))
                        AddLink(selectedRouter, args.AvatarName, args.AvatarID);
                    else if (!selectedRouter.Equals(ID))
                        RemoveLink(selectedRouter,args.AvatarName, args.AvatarID);
                    else
                        Control.State.ResetState(args.AvatarName, args.AvatarID);
                    break;
                case SandboxControl.EPSelected:
                    UUID selectedEP = Control.State.GetSelectedEntity(args.AvatarName, args.AvatarID).ID;
                    if (Control.AreLinked(selectedEP, ID))
                        RemoveLink(selectedEP, args.AvatarName, args.AvatarID);
                    else {
                        foreach (var neighbour in Control.GetNeighbours(selectedEP))
                            RemoveLink(selectedEP, neighbour.ID, args.AvatarName, args.AvatarID);
                        AddLink(selectedEP, args.AvatarName, args.AvatarID);
                    }
                    break;
                case SandboxControl.LinkSelected:
                    ILogicEntity selectedLink = Control.State.GetSelectedEntity(args.AvatarName, args.AvatarID);
                    selectedLink.Selected = 0d;

                    Control.State.SetState(SandboxControl.RouterSelected, args.AvatarName, args.AvatarID);
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
