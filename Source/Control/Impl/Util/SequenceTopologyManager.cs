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
using System.Xml;
using common.framework.interfaces.entities;
using common.framework.impl.util;
using OpenMetaverse;
using Diagrams.Control.Impl.Module;
using Nini.Config;
using System.Drawing;
using Diagrams.Control.impl.Entities;

namespace Diagrams.Control.impl.Util {
    public class SequenceTopologyManager : TopologyManager {
        private Control.Impl.Module.Control _control;

        public SequenceTopologyManager(IConfig config, Control.Impl.Module.Control control)
            : base(config, control) {

            _control = control;
        }

        protected override INode LoadNode(string name, XmlNode xmlNode, Parameters parameters, Vector3 position, Color colour, float scale, string user, UUID userID) {
            if (parameters == null)
                parameters = new Parameters();
            parameters.Set<bool>("IsEP", xmlNode.LocalName.Equals("EP"));
            parameters.Set<string>("Owner", user);
            parameters.Set<UUID>("OwnerID", userID);
            return _control.AddNode(name, parameters, position, colour);
        }
    }
}
