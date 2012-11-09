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
using System.IO;
using common.framework.interfaces.basic;
using Nini.Config;

namespace Diagrams.Control.impl.Util {
    public abstract class FileWorker {
        protected readonly string _userFolder;
        protected readonly string _sharedFolder;
        
        protected FileWorker(IConfig controlConfig) {
            _userFolder = controlConfig.Get("UserFolder", ".");
            _sharedFolder = controlConfig.Get("SharedFolder", null);
            if (_sharedFolder != null)
                _sharedFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _sharedFolder);
        }

        protected string GetFile(string folder, string file) {
            if (Path.IsPathRooted(file)) 
                throw new Exception("Unable to get topology at '" + file + "'. Filename must be a relative path.");

            if (!File.Exists(folder))
                Directory.CreateDirectory(folder);
            file = Path.Combine(folder, Path.GetFileName(file));
            if (!Path.GetExtension(file).ToUpper().Equals(".XML"))
                file += ".xml";
            return file;
        }

        public virtual string SharedFolder {
            get { return _sharedFolder; }
        }

        public virtual string GetUserFolder(string name) {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _userFolder);
            folder = Path.Combine(folder, name);
            if (!File.Exists(folder))
                Directory.CreateDirectory(folder);
            return folder;
        }
    }
}
