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
using Diagrams.Control.impl.Controls.Dialogs;
using Diagrams.Control.impl.Module;
using common.framework.interfaces.basic;
using common.interfaces.entities;
using OpenMetaverse;
using Diagrams.MRM.Controls.Buttons;

namespace Diagrams.Control.impl.Buttons.ControlButtons {
    class Clear : ControlButton {
        private const string NO = "No";
        private const string CLEAR = "Clear";
        private const string YES = "Yes";

        private readonly Dialog _dialog;

        private readonly IControlUtil _control;

        public Clear(IButton button, IControlUtil control, IPrimFactory factory)
            : base(button) {
            _control = control;
            _dialog = new Dialog(button.Prims.Count() > 0 ? button.Prims.First() : factory.Host, factory, YES, NO);
            _dialog.ResponseReceived += (name, id, text, chat) => {
                if (text.Equals(YES))
                    _control.Clear(name, id);
            };
        }
    
        protected override void Touched(UUID entity, TouchEventArgs args) {
            _dialog.Show(args.AvatarName, args.AvatarID, "Are you sure you want to clear your entire topology?");
        }
    }
}
