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
using OpenSim.Region.OptionalModules.Scripting.Minimodule;
using MRMChatEventArgs=OpenSim.Region.OptionalModules.Scripting.Minimodule.ChatEventArgs;
using OpenMetaverse;
using System.IO;

namespace OpenSim {
    public class MiniModule : MRMBase {
        private const int CHAN = 50;
        private const string PING = "Ping";
        private const string PING_ACK = "Pong";

        public override void Start(string[] args) {
            //World.Avatars[0].WorldPosition += ;
            World.OnChat += Listen;

            Host.Object.Say(PING_ACK, CHAN);
        }

        private void Listen(IWorld source, MRMChatEventArgs args) {
            if (args.Channel != 0)
                Host.Object.Say(args.Text + " @ " + args.Channel);
            if (args.Channel == CHAN && args.Text.Equals(PING))
                Host.Object.Say(PING_ACK, CHAN);
        }

        public override void Stop() {

        }
    }
}