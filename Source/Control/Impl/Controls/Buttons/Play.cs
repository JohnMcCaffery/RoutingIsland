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
using Diagrams.Control.Impl.Module;
using OpenMetaverse;
using common.framework.impl.util;
using Diagrams.MRM.Controls.Buttons;

namespace Diagrams.Control.impl.Buttons.ControlButtons {
    public class Play : ControlButton {
        private SandboxControl _control;

        public Play(IButton button, SandboxControl control, UUID owner)
            : base(button) {

            _control = control;
            _control.State.AddStateButton(owner, button);
            button.SetVisualState(_control.Fade, 0);
        }

        protected override void Touched(UUID entity, TouchEventArgs args) {
            string state = _control.State.GetState(args.AvatarName, args.AvatarID);
            if (state == SandboxControl.RouterSelected) {
                _control.State.ResetState(args.AvatarName, args.AvatarID);
                _control.Model.VisualiseRouting(_control.State.GetSelectedEntity(args.AvatarName, args.AvatarID).ID, new Parameters());
            } else if (state == SandboxControl.EPSelected) {
                _control.State.SetState(SandboxControl.VisualiseAlgorithmSelected, args.AvatarName, args.AvatarID);
                Button.SetVisualState(1, _control.ToggleGlow);
            }
        }
    }
}
