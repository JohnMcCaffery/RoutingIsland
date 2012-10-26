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
using OpenMetaverse;
using common.framework.interfaces.entities;
using Diagrams.Control.impl.Entities;
using Diagrams.Control.Impl.Module;

namespace Diagrams.Control.Impl.Util.State {
    public class SharedState : IState {
        private string _state = Module.Control.NothingSelected;
        private ILogicEntity _selectedPrim;
        private readonly Dictionary<string, IToggle> _toggles;
        private readonly List<IButton> _stateButtons;

        private Module.Control _control;

        public SharedState(Module.Control control) {
            _control = control;

            _toggles = new Dictionary<string, IToggle>();
            _stateButtons = new List<IButton>();
        }

        #region IState Members

        public event OnStateChangeDelegate OnStateChange;

        public void SetSelectedPrim(UUID prim, string name, UUID id) {
            IControlEntity entity;
            if (_control.IsNode(prim))
                entity = _control.GetNode(prim);
            else
                entity = _control.GetLink(prim);
            if (!entity.Authorize(name, id))
                return;

            entity.Selected = _control.ToggleGlow;
            _selectedPrim = entity;
        }

        public ILogicEntity GetSelectedEntity(string name, UUID id) {
            return _selectedPrim;
        }

        public void ResetState(string name, UUID id) {
            SetState(Module.Control.NothingSelected, name, id);

            if (_selectedPrim != null)
                _selectedPrim.Selected = 0d;
            foreach (IButton b in _stateButtons)
                b.SetVisualState(_control.Fade, 0d);
        }

        public void SetState(string state, string name, UUID id) {
            _state = state;
        }

        public string GetState(string name, UUID id) {
            return _state;
        }

        public bool GetToggleState(string toggle, UUID id) {
            return _toggles.ContainsKey(toggle) ? _toggles[toggle].IsOn : false;
        }

        public void AddToggle(UUID id, Toggle toggle) {
            _toggles[toggle.Name] = toggle;
        }

        public void AddStateButton(UUID id, IButton button) {
            _stateButtons.Add(button);
        }

        #endregion
    }
}
