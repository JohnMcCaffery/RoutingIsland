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
using Diagrams.Control.impl.Controls;
using System.IO;
using Diagrams.Control.impl.Module;
using common.framework.interfaces.basic;
using common.interfaces.entities;
using Nini.Config;
using Diagrams.Control.impl.Controls.Dialogs;
using OpenMetaverse;
using Diagrams.Control.Impl.Module;
using Diagrams.MRM.Controls.Buttons;

namespace Diagrams.Control.impl.Buttons.ControlButtons {
    class Save : ControlButton {
        private SaveDialog _save;

        public Save(IButton button, SandboxControl control, IPrimFactory factory, IConfig controlConfig)
            : base(button) {

            _save = new SaveDialog(button.Prims.Count() > 0 ? button.Prims.First() : factory.Host, factory, "topology", control.DefaultTopologyName, name => control.Topology.GetFolder(name));
            _save.OnSave += (name, id, file) => control.Topology.SaveTopology(name, id, file);
        }
            
        protected override void Touched(UUID entity, TouchEventArgs args) {
            _save.Show(args.AvatarName, args.AvatarID);
        }
    }
}
