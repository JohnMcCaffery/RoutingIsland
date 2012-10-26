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
using common.framework.interfaces.basic;
using common.interfaces.entities;
using System.IO;

namespace Diagrams.Control.impl.Controls.Dialogs {
    public class SaveDialog {
        public const string SAVE = "Save";
        public const string SAVE_AS = "Save As";
        
        private readonly string _defaultName;
        private readonly string _toSave;

        private Dialog dialog;

        public event Action<string, UUID, string> OnSave;

        private Func<string, string> _getFolder;

        public SaveDialog(IPrim prim, IPrimFactory factory, string toSave, string defaultName, Func<string, string> getFolder) {
            _defaultName = defaultName;
            _toSave = toSave;
            _getFolder = getFolder;
            dialog = new Dialog(prim, factory, SAVE, SAVE_AS, Dialog.CANCEL);
            dialog.ResponseReceived += (name, id, pressed, chatted) => {
                switch (pressed) {
                    case
                        SAVE:
                        if (OnSave != null)
                            OnSave(name, id, GetDefaultName(name));
                        break;
                    case SAVE_AS:
                        if (chatted == null)
                            Show(name, id, true);
                        else if (OnSave != null)
                            OnSave(name, id, chatted);
                        break;
                    case Dialog.CANCEL: break;
                }
            };
        }

        private string GetDefaultName(string user) {
            string folder = _getFolder(user);
            int append = Directory.Exists(folder) ? Directory.GetFiles(folder).Count(file => Path.GetFileName(file).StartsWith(_defaultName)) : 0;
            return _defaultName + (append > 0 ? "_" + append : "");
        }

        public void Show(string user, UUID id) {
            Show(user, id, false);
        }

        private void Show(string user, UUID id, bool repeat) {
            string msg = (repeat ? "No file entered.\n" : "") +
                "Press 'Save' to save this " + _toSave + " as '" + GetDefaultName(user) + "'.\n" +
                "Enter '/" + Dialog.ChatChannel + " FILENAME' into the chat console then press 'SaveAs' to save this " + _toSave + " as FILENAME. \n" +
                "Press 'Cancel' to cancel.";
            dialog.Show(user, id, msg);
        }
    }
}
