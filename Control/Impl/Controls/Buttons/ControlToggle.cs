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
using Diagrams.MRM.Controls.Buttons;

namespace Diagrams.Control.impl.Buttons.ControlButtons {
    public abstract class ControlToggle : ControlButton, IToggle {

        protected IToggle Toggle {
            get {
                return Button as IToggle;
            }
        }

        public bool IsOn {
            get {
                return Toggle.IsOn;
            }
            set {
                Toggle.IsOn = value;
            }
        }

        protected abstract void Toggled(object sender, ToggleEventArgs args);

        protected abstract void ToggledOn(object sender, ToggleEventArgs args);

        protected abstract void ToggledOff(object sender, ToggleEventArgs args);

        public ControlToggle(IToggle toggle)
            : base(toggle) {

            toggle.OnToggled += (sender, args) => Toggled(sender, args);
            toggle.OnToggledOn += (sender, args) => ToggledOn(sender, args);
            toggle.OnToggledOff += (sender, args) => ToggledOff(sender, args);
        }

        #region IToggle Members

        public event EventHandler<ToggleEventArgs> OnToggledOn {
            add { Toggle.OnToggledOn += value; }
            remove { Toggle.OnToggledOn -= value; }
        }

        public event EventHandler<ToggleEventArgs> OnToggledOff {
            add { Toggle.OnToggledOff += value; }
            remove { Toggle.OnToggledOff -= value; }
        }

        public event EventHandler<ToggleEventArgs> OnToggled {
            add { Toggle.OnToggled += value; }
            remove { Toggle.OnToggled -= value; }
        }

        #endregion
    }
}
