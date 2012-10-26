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
using common.interfaces.entities;
using common.framework.interfaces.basic;
using OpenMetaverse;
using JM726.Lib.Static;
using System.Threading;
using System.Drawing;

namespace Diagrams.MRM.Controls.Buttons {
    public class Toggle : IToggle {
        private readonly double _toggleGlow = .3;
        private readonly double _fade = .5;
        private bool _on;

        private IButton _button;

        public Toggle(IButton button, double fade,  double glow)
            : this(button, fade, glow, false) {
        }

        public Toggle(IButton button, double fade, double glow, bool start) {
            _button = button;
            _toggleGlow = glow;
            _fade = fade;
            _on = start;

            _button.OnTouched += new EntityTouchedDelegate(Touched);
            _button.SetVisualState(_on ? 1d : _fade, _on ? _toggleGlow : 0d);
        }

        private void Touched(UUID entity, TouchEventArgs args) {
            _on = !_on;
            ToggleChanged(entity, args.AvatarName, args.AvatarID);
        }

        private void ToggleChanged(UUID source, string name, UUID id) {
            if (OnToggled != null)
                OnToggled(id, new ToggleEventArgs(source, name, id));

            if (_on) {
                _button.SetVisualState(1d, _toggleGlow);
                if (OnToggledOn != null)
                    OnToggledOn(id, new ToggleEventArgs(source, name, id));
            } else {
                _button.SetVisualState(_fade, 0d);
                if (OnToggledOff != null)
                    OnToggledOff(id, new ToggleEventArgs(source, name, id));
            }
        }

        #region IToggle Members

        public event EventHandler<ToggleEventArgs> OnToggledOn;

        public event EventHandler<ToggleEventArgs> OnToggledOff;

        public event EventHandler<ToggleEventArgs> OnToggled;

        public bool IsOn {
            get { return _on; }
            set {
                _on = value;
                ToggleChanged(UUID.Zero, null, UUID.Zero);
            }
        }

        #endregion

        #region IButton Members

        public event EntityTouchedDelegate OnTouched {
            add { _button.OnTouched += value; }
            remove { _button.OnTouched -= value; }
        }

        public string Name {
            get { return _button.Name; }
        }

        public IEnumerable<IPrim> Prims {
            get { return _button.Prims; }
        }

        public void Dispose() {
            _button.Dispose();
        }

        public void SetVisualState(double fade, double glow) {
            _button.SetVisualState(fade, glow);
        }

        #endregion
    }
}
