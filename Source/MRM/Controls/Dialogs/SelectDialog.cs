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
    public class SelectDialog {
        private const string MORE = "More";

        private const int MAX_FILES_PER_DIALOG = 10;

        private readonly Dictionary<UUID, int> _fileSubsets;

        private readonly string _sharedFolder;

        private readonly Func<string, string> _getFolder;

        private Dialog _dialog;

        public event Action<string, UUID, string> OnSelect;

        public SelectDialog(IPrim prim, IPrimFactory factory, Func<string, string> getFolder, string sharedFolder) {
            _getFolder = getFolder;
            _sharedFolder = sharedFolder;
            _fileSubsets = new Dictionary<UUID, int>();
            _dialog = new Dialog(prim, factory);
            _dialog.ResponseReceived += (name, id, pressed, chatted) => {
                switch (pressed) {
                    case
                        MORE: 
                        ShowNextSet(name, id);
                        break;
                    case Dialog.CANCEL: 
                        _fileSubsets.Remove(id);
                        break;
                    default: 
                        _fileSubsets.Remove(id);
                        if (OnSelect != null) 
                            OnSelect(name, id, pressed);
                        break;
                }
            };
        }

        private string[] GetFileButtons(string name, int fileSubset) {
            string folder = _getFolder(name);
            if (!Directory.Exists(folder) && (_sharedFolder == null || !Directory.Exists(_sharedFolder)))
                return new string[] { Dialog.CANCEL };
            IEnumerable<string> files = new string[0];
            if (Directory.Exists(folder))
                files = Directory.GetFiles(folder).Select(file => Path.GetFileName(file)).Where(file => file.EndsWith(".xml"));
            if (_sharedFolder != null && Directory.Exists(_sharedFolder)) 
                files = files.Concat(Directory.GetFiles(_sharedFolder).Select(file => Path.GetFileName(file))).Where(file => file.EndsWith(".xml"));
            int numButtons = files.Count();
            int skip = MAX_FILES_PER_DIALOG * fileSubset;
            int remainingFiles = numButtons - skip;
            int take = remainingFiles / MAX_FILES_PER_DIALOG > 0 ? MAX_FILES_PER_DIALOG : remainingFiles;
            if (remainingFiles == MAX_FILES_PER_DIALOG + 1)
                take++;
            if (take != numButtons) {
                files = files.Skip(skip).Take(take);
                if (take == MAX_FILES_PER_DIALOG && numButtons != skip + take)
                    files = files.Concat(new string[] { MORE });
            }
            files = files.Concat(new string[] { Dialog.CANCEL });
            return files.ToArray();
        }

        public void Show(string name, UUID id) {
            if (!_fileSubsets.ContainsKey(id))
                _fileSubsets.Add(id, 0);
            _dialog.Show(name, id, "Select a file.", GetFileButtons(name, 0));
        }

        private void ShowNextSet(string name, UUID id) {
            _fileSubsets[id]++;
            _dialog.Show(name, id, "Select a file.", GetFileButtons(name, _fileSubsets[id]));
        }
    }
}
