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
using Diagrams.Control.impl.Controls.Dialogs;
using Diagrams.Control.Impl.Module;
using OpenMetaverse;
using Diagrams.Control.impl.Module;
using common.framework.interfaces.basic;
using common.interfaces.entities;
using Nini.Config;
using System.IO;
using Diagrams.MRM.Controls.Buttons;

namespace Diagrams.Control.impl.Buttons.ControlButtons {
    public class Open : ControlButton {
        private const string SEQUENCE = "Sequence";

        private const string TOPOLOGY = "Topology";

        private readonly Dialog _dialog;

        public Open(IButton button, IControlUtil control, IPrimFactory factory)
            : base(button) {

            IPrim prim = button.Prims.Count() > 0 ? button.Prims.First() : factory.Host;
            _dialog = new Dialog(prim, factory, SEQUENCE, TOPOLOGY, Dialog.CANCEL);
            SelectDialog openSequenceDialog = new SelectDialog(prim, factory, name => control.Record.GetUserFolder(name), control.Record.SharedFolder);
            SelectDialog openTopologyDialog = new SelectDialog(prim, factory, name => control.Topology.GetUserFolder(name), control.Topology.SharedFolder);
 
            _dialog.ResponseReceived += (name, id, text, chat) => {
                if (text.Equals(Dialog.CANCEL))
                    return;
                switch (text) {
                    case SEQUENCE: openSequenceDialog.Show(name, id); break;
                    case TOPOLOGY: openTopologyDialog.Show(name, id); break;
                }
            };
            openSequenceDialog.OnSelect += (name, id, file) => control.Record.PlayRecording(name, id, file);
            openTopologyDialog.OnSelect += (name, id, file) => control.Topology.LoadTopology(name, id, file);
        }

        protected override void Touched(UUID entity, TouchEventArgs args) {
            _dialog.Show(args.AvatarName, args.AvatarID, "Do you want to open a topology or a sequence?");
        }
    }
}
