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
using Diagrams.MRM.Controls.Buttons;
using common.framework.interfaces.entities;
using Diagrams.Control.Impl.Module;

namespace Diagrams.Control.Impl.Util {
    public class StateChangeEventArgs : EventArgs {
        public string NewState;
        public string OldState;

        public StateChangeEventArgs(string oldState, string newState) {
            OldState = oldState;
            NewState = newState;
        }
    }

    public delegate void OnStateChangeDelegate(IState state, StateChangeEventArgs args);

    public interface IState {
        event OnStateChangeDelegate OnStateChange;

        void SetSelectedPrim(UUID prim, string name, UUID id);

        ILogicEntity GetSelectedEntity(string name, UUID id);

        void ResetState(string name, UUID id);

        void SetState(string state, string name, UUID id);

        string GetState(string name, UUID id);

        bool GetToggleState(string toggle, UUID id);

        void AddToggle(UUID id, Toggle toggle);

        void AddStateButton(UUID id, IButton button);
    }
}
