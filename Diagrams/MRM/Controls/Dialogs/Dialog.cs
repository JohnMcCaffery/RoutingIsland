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
using common.framework.interfaces.basic;
using OpenMetaverse;
using common.interfaces.entities;

namespace Diagrams.Control.impl.Controls.Dialogs {
    public class Dialog {
        public const string CANCEL = "Cancel";
        public const string OK = "Ok";

        private static int _chatChannel = 46;

        public static void SetChatChannel(int channel) {
            _chatChannel = channel;
        }

        public static int ChatChannel {
            get { return _chatChannel; }
        }

        private static readonly Random _random = new Random();

        public event Action<string, UUID, string, string> ResponseReceived;

        private Action<string, UUID, string, string> _listener;

        private readonly IPrim _prim;

        private readonly IPrimFactory _primFactory;

        private readonly string[] _buttons = null;

        private Dictionary<UUID, string> _receivedText;

        public Dialog(IPrim prim, IPrimFactory factory) {
            _prim = prim;
            _primFactory = factory;
            _receivedText = new Dictionary<UUID, string>();
            _primFactory.AddChannelListener(ChatChannel, (name, id, text, channel) => {
                if (_receivedText.ContainsKey(id))
                    _receivedText[id] = text;
            });
        }

        public Dialog(IPrim prim, IPrimFactory factory, params string[] buttons) : this (prim, factory) {
            _buttons = buttons;
        }


        /// <param name="user">The user to display the dialog for.</param>
        /// <param name="id">The ID of the user to display the dialog for.</param>
        /// <param name="entity">The entity to display the dialog from.</param>
        public void Show(string user, UUID id, string msg) {
            if (_buttons == null)
                throw new Exception("Unable to show dialog. No button array specified.");
            Show(user, id, msg, _buttons);
        }


        /// <param name="user">The user to display the dialog for.</param>
        /// <param name="id">The ID of the user to display the dialog for.</param>
        /// <param name="entity">The entity to display the dialog from.</param>
        public void Show(string user, UUID id, string msg, string[] buttons) {
            if (!_receivedText.ContainsKey(id))
                _receivedText.Add(id, null);
            else
                _receivedText[id] = null;
            
            int chan = _random.Next(int.MinValue, -1);
            _primFactory.AddChannelListener(chan, ButtonPressed);
            _prim.Dialogue(id, msg, buttons, chan);
        }

        protected void ButtonPressed(string name, UUID id, string text, int channel) {
            string receivedText = _receivedText[id];
            _receivedText.Remove(id);
            if (ResponseReceived != null)
                ResponseReceived(name, id, text, receivedText);
            _primFactory.RemoveChannelListener(channel, ButtonPressed);
        }
    }
}
