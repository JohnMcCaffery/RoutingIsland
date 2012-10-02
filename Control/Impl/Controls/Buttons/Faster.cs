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
using common.framework.interfaces.basic;
using OpenMetaverse;
using Diagrams.MRM.Controls.Buttons;

namespace Diagrams.Control.impl.Buttons.ControlButtons {
    class Faster : ControlButton {
        private int TimeInc {
            get {
                return _control.Wait > 4 ? _control.Wait / 4 : 1;
            }
        }

        private readonly IModule _control;

        private readonly IPrim _hostPrim;

        public Faster(IButton button, IModule control, IPrim hostPrim)
            : base(button) {

            _control = control;
            _hostPrim = hostPrim;
        }

        protected override void Touched(UUID entity, TouchEventArgs args) {
            _control.Wait -= TimeInc;
            _hostPrim.Say("Removed " + TimeInc + " to wait to make " + _control.Wait);
        }
    }
}
