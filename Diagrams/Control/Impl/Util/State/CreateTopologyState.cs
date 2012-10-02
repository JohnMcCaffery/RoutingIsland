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

namespace Diagrams.Control.Impl.Util.State {
    public class CreateTopologyState : SharedState {
        public CreateTopologyState(Module.Control control)
            : base(control) {
        }

        #region IState Members

        public event OnStateChangeDelegate OnStateChange;

        public void SetSelectedPrim(OpenMetaverse.UUID prim, string name, OpenMetaverse.UUID id) {
            throw new NotImplementedException();
        }

        public common.framework.interfaces.entities.ILogicEntity GetSelectedEntity(string name, OpenMetaverse.UUID id) {
            throw new NotImplementedException();
        }

        public void ResetState(string name, OpenMetaverse.UUID id) {
            throw new NotImplementedException();
        }

        public void SetState(string state, string name, OpenMetaverse.UUID id) {
            throw new NotImplementedException();
        }

        public string GetState(string name, OpenMetaverse.UUID id) {
            throw new NotImplementedException();
        }

        public bool GetToggleState(string toggle, UUID id) {
            return toggle.Equals("Build");
        }

        public void AddToggle(OpenMetaverse.UUID id, MRM.Controls.Buttons.Toggle toggle) {
            throw new NotImplementedException();
        }

        public void AddStateButton(OpenMetaverse.UUID id, MRM.Controls.Buttons.IButton button) {
            throw new NotImplementedException();
        }

        #endregion
    }
}
