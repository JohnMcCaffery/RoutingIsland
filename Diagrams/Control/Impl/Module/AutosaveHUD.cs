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
using Diagrams.Common.interfaces.keytable;
using common.Queue;
using common.interfaces.entities;
using Nini.Config;
using System.IO;

namespace Diagrams.Control.Impl.Module {
    public class AutosaveHUD : FullHudControl {
        private const string AUTOSAVE_NAME = "Autosave.xml";
        private readonly string _autosaveFile;
        public AutosaveHUD(IKeyTableFactory tableFactory, IAsynchQueueFactory queueFactory, IPrimFactory primFactory, IModel model, IConfigSource config)
            : base(tableFactory, queueFactory, primFactory, model, config) {

            _autosaveFile = Path.Combine(Topology.GetFolder(Factory.Owner), AUTOSAVE_NAME);
            if (File.Exists(_autosaveFile))
                Topology.LoadTopology(Factory.Owner, Factory.Host.Owner, _autosaveFile);
        }

        public override void Stop() {
            Topology.SaveTopology(Factory.Owner, Factory.Host.Owner, _autosaveFile);
            base.Stop();
        }
    }
}
