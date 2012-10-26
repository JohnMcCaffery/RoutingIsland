﻿/*************************************************************************
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
using Diagrams.Control.impl.Controls.Dialogs;
using Diagrams.Control.impl.Module;
using common.framework.interfaces.basic;
using common.interfaces.entities;
using Nini.Config;
using Diagrams.Control.Impl.Module;
using OpenMetaverse;
using Diagrams.MRM.Controls.Buttons;

namespace Diagrams.Control.impl.Buttons.ControlButtons {
    public class Record : ControlToggle {
        private SaveDialog _save;

        private IControlUtil _control;

        public Record(IToggle toggle, SandboxControl control, IPrim hostPrim, IPrimFactory factory, IConfig controlConfig)
            : base(toggle) {

            _control = control;
            _save = new SaveDialog(hostPrim, factory, "sequence", control.DefaultRecordingName, name => control.Record.GetFolder(name));
            _save.OnSave += (name, id, file) => control.Record.SaveRecording(name, file);
        }

        protected override void Touched(UUID entity, TouchEventArgs args) {
        }

        protected override void ToggledOn(object sender, ToggleEventArgs args) {
            _control.Record.StartRecording();
        }

        protected override void ToggledOff(object sender, ToggleEventArgs args) {
            _control.Record.StopRecording();
            _save.Show(args.Name, args.ID);
        }

        protected override void Toggled(object sender, ToggleEventArgs args) {
        }
    }
}