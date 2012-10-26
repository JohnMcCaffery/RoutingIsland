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

//MRM:C#
using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Region.OptionalModules.Scripting.Minimodule;

namespace Diagrams.MRM {
    public class SingleTouchScript : MRMBase {
        private const string SELECT = "SELECT";

        private string _name;
        private readonly OnChatDelegate _chatListener;

        public SingleTouchScript() {
            _chatListener = Listen;
        }

        public override void Start(string[] args) {
            World.OnChat += _chatListener;
            _name = Host.Object.Name;
        }

        private void Listen(IWorld source, ChatEventArgs args) {
            if (!args.Text.StartsWith(SELECT) || !args.Sender.Equals(Host.Object.Root))
                return;

            string[] msg = args.Text.Split(',');

            if (msg.Length == 3 && msg[1].Equals(_name)) {
                float glow;
                if (float.TryParse(msg[2], out glow))
                    foreach (IObjectMaterial mat in Host.Object.Materials)
                        //mat.Color = glow > 0 ? Color.White : Color.DarkGray;
                        mat.Bright = glow > 0;
            }
        }

        public override void Stop() {
            World.OnChat -= _chatListener;
        }
    }
}