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
using System.Drawing;

namespace Diagrams.MRM.Controls.Buttons {
    public class ToggleEventArgs : EventArgs {
        public readonly UUID Source;
        public readonly string Name;
        public readonly UUID ID;
        public ToggleEventArgs(UUID source, string name, UUID id) {
            Source = source;
            Name = name;
            ID = id;
        }
    }
    public static class ToggleButtonExtension {
        public static void SetVisualState(this IButton button, double fade, double glow) {
            foreach (var prim in button.Prims) {
                prim.Glow = glow;
                int component = (int)(fade * 255f);
                prim.Colour = Color.FromArgb(component, component, component);
            }
        }
    }
    public interface IToggle : IButton {
        /// <summary>
        /// What happens when the toggle is toggled to on.
        /// </summary>
        event EventHandler<ToggleEventArgs> OnToggledOn;

        /// <summary>
        /// What happens when the toggle is toggled to off.
        /// </summary>
        event EventHandler<ToggleEventArgs> OnToggledOff;

        /// <summary>
        /// What happens when the toggle state switches.
        /// </summary>
        event EventHandler<ToggleEventArgs> OnToggled;

        /// <summary>
        /// Whether the toggle is currently selected.
        /// </summary>
        bool IsOn {
            get;
            set;
        }
    }
}
