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
using Diagrams.Control.impl.Controls.Dialogs;
using Diagrams.Control.impl.Module;
using common.framework.interfaces.basic;
using common.interfaces.entities;
using Diagrams.Control.impl.Entities;
using Diagrams.Control.Impl.Module;
using common.framework.impl.util;
using Diagrams.MRM.Controls.Buttons;

namespace Diagrams.Control.impl.Buttons.ControlButtons {
    public class ModifyLinks : ControlButton {

        private readonly Dialog _dialog;

        private readonly SandboxControl _control;

        private Parameters GetParameters(UUID id) {
            Parameters p = new Parameters();
            p.Set<bool>("Visualise", _control.State.GetToggleState(SandboxControl.ShowUpdatePackets, id));
            return p;
        }

        public ModifyLinks(IButton button, SandboxControl control, IPrimFactory factory)
            : base(button) {
            _control = control;
            _dialog = new Dialog(button.Prims.Count() > 0 ? button.Prims.First() : factory.Host, factory, SandboxControlLink.UNIT, SandboxControlLink.LENGTH, SandboxControlLink.RANDOM, Dialog.CANCEL);
            _dialog.ResponseReceived += (name, id, text, chat) => {
                switch (text) {
                    case SandboxControlLink.UNIT:
                        SandboxControlLink.SetType(name, id, ControlLink.LinkType.Unit, _control, GetParameters(id));
                        break;
                    case SandboxControlLink.RANDOM:
                        SandboxControlLink.SetType(name, id, ControlLink.LinkType.Random, _control, GetParameters(id));
                        break;
                    case SandboxControlLink.LENGTH:
                        SandboxControlLink.SetType(name, id, ControlLink.LinkType.Length, _control, GetParameters(id));
                        break;
                }
            };
        }
    
        protected override void Touched(UUID entity, TouchEventArgs args) {
            _dialog.Show(args.AvatarName, args.AvatarID, "Are you sure you want to clear your entire topology?");
        }
    }
}
