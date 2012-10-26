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
using Diagrams.Control.impl.Util;
using Diagrams.Control.impl.Controls.Dialogs;
using Diagrams.Control.impl.Module;
using common.framework.interfaces.basic;
using common.interfaces.entities;
using Diagrams.MRM.Controls.Buttons;

namespace Diagrams.Control.impl.Buttons.ControlButtons {
    public class ChangeAlg : ControlButton {
        private readonly Dialog _dialog;

        private readonly IControlUtil _control;

        public ChangeAlg(IButton button, IControlUtil control, IPrimFactory factory) : base (button) {
            _control = control;
            _dialog = new Dialog(button.Prims.Count() > 0 ? button.Prims.First() : factory.Host, factory, control.Model.Algorithms.Concat(new String[] { Dialog.CANCEL }).ToArray());
            _dialog.ResponseReceived += (name, id, text, chat) => {
                if (!text.Equals(Dialog.CANCEL))
                    _control.Model.SetAlgorithm(text);
            };
        }
    
        protected override  void Touched(UUID entity, TouchEventArgs args) {
            _dialog.Show(args.AvatarName, args.AvatarID, "Current Algorithm: " + _control.Model.Algorithm + ".\nWhich algorithm would you like to use?");
        }
    }
}
