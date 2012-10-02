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
using Diagrams.MRM.Controls.Buttons;
using common.interfaces.entities;

namespace Diagrams.Control.impl.Buttons.ControlButtons {
    public class Create : ControlButton {

        private readonly SandboxControl _control;
        private readonly IPrimFactory _factory;

        public Create(IButton button, SandboxControl control, IPrimFactory factory)
            : base(button) { 
            _control = control;
            _factory = factory;
        }
        protected override void Touched(UUID entity, TouchEventArgs args) {
            if (!_control.State.GetToggleState(SandboxControl.Build, args.AvatarID))
                return;

            Vector3 inputPos = _factory[entity].Pos;
            Vector3 diff = Vector3.Subtract(args.AvatarPosition, inputPos);
            float distance = Vector3.Distance(args.AvatarPosition, inputPos);
            diff = Vector3.Multiply(diff, 1f / distance);
            Vector3 pos = Vector3.Subtract(args.AvatarPosition, diff);
            if (_control.State.GetToggleState(SandboxControl.EPMode, args.AvatarID))
                _control.AddEP(args.AvatarName, args.AvatarID, pos);
            else
                _control.AddRouter(args.AvatarName, args.AvatarID, pos);
        }
    }
}
