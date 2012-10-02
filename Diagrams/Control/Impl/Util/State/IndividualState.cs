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
using Diagrams.Control.impl.Entities;
using common.framework.interfaces.entities;
using Diagrams.Control.Impl.Module;
using Diagrams.Common.interfaces.keytable;
using Diagrams.MRM.Controls.Buttons;
using Diagrams.Common;

namespace Diagrams.Control.Impl.Util {
    public class IndividualState : IState {
        private readonly Module.Control _control;

        private readonly Dictionary<string, string> _state;
        private readonly Dictionary<string, IControlEntity> _selectedEntities;
        private readonly IKeyTable<Dictionary<string, Toggle>> _toggles;
        private readonly IKeyTable<List<IButton>> _stateButtons;

        public IndividualState(Module.Control control, IKeyTableFactory tableFactory) {
            _control = control;

            _state = new Dictionary<string, string>();
            _selectedEntities = new Dictionary<string, IControlEntity>();
            _toggles = tableFactory.MakeKeyTable<Dictionary<string, Toggle>>();
            _stateButtons = tableFactory.MakeKeyTable<List<IButton>>();
        }

        public void SetSelectedPrim(UUID prim, string name, UUID id) {
            if ((!_control.IsNode(prim) && !_control.IsLink(prim)))
                return;
            IControlEntity entity;
            if (_control.IsNode(prim))
                entity = _control.GetNode(prim);
            else
                entity = _control.GetLink(prim);
            if (!entity.Authorize(name, id))
                return;

            entity.Selected = _control.ToggleGlow;
            if (!_selectedEntities.ContainsKey(name))
                _selectedEntities.Add(name, entity);
            else
                _selectedEntities[name] = entity;
        }

        public virtual ILogicEntity GetSelectedEntity(string name, UUID id) {
            return _selectedEntities.ContainsKey(name) ? _selectedEntities[name] : null;
        }

        public event OnStateChangeDelegate OnStateChange;

        public virtual void ResetState(string name, UUID id) {
            SetState(Module.Control.NothingSelected, name, id);

            ILogicEntity selectedPrim = GetSelectedEntity(name, id);
            if (selectedPrim != null)
                selectedPrim.Selected = 0d;
            if (_stateButtons.ContainsKey(id))
                foreach (IButton b in _stateButtons[id])
                    b.SetVisualState(_control.Fade, 0d);
        }

        public virtual void SetState(string state, string name, UUID id) {
            string oldState = _state.ContainsKey(name) ? _state[name] : null;
            if (!_state.ContainsKey(name))
                _state.Add(name, Module.Control.NothingSelected);
            _state[name] = state;
            if (OnStateChange != null)
                OnStateChange(this, new StateChangeEventArgs(oldState, state));
        }

        public virtual string GetState(string name, UUID id) {
            if (!_state.ContainsKey(name))
                _state.Add(name, Module.Control.NothingSelected);
            return _state[name];
        }

        public virtual bool GetToggleState(string toggle, UUID id) {
            if (_toggles.ContainsKey(id) && _toggles[id].ContainsKey(toggle))
                return _toggles[id][toggle].IsOn;
            return false;
        }

        public virtual void AddToggle(UUID id, Toggle toggle) {
            if (!_toggles.ContainsKey(id))
                _toggles.Add(id, new Dictionary<string, Toggle>());

            if (!_toggles[id].ContainsKey(toggle.Name))
                _toggles[id].Add(toggle.Name, toggle);
            else
                _toggles[id][toggle.Name] = toggle;
        }

        public virtual void AddStateButton(UUID id, IButton button) {
            if (!_stateButtons.ContainsKey(id))
                _stateButtons.Add(id, new List<IButton>());
            _stateButtons[id].Add(button);
        }
    }
}
