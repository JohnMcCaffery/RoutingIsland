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
using common.framework.interfaces.basic;
using common.interfaces.entities;
using Nini.Config;
using OpenMetaverse;
using Diagrams.MRM.Controls.Buttons;

namespace Diagrams.Control.impl.Buttons.ControlButtons {
    public class Pause : ControlToggle {
        private readonly IModule _control;

        public Pause(IToggle toggle, IModule control, IPrim hostPrim, IPrimFactory factory, IConfig controlConfig)
            : base(toggle) {

            _control = control;
        }

        protected override void Toggled(object sender, ToggleEventArgs args) {
            _control.Paused = IsOn;
        }

        protected override void ToggledOn(object sender, ToggleEventArgs args) { }
        protected override void ToggledOff(object sender, ToggleEventArgs args) { }
        protected override void Touched(UUID entity, TouchEventArgs args) { }
    }
}