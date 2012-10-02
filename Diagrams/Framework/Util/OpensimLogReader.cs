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
using OpenMetaverse;
using common.framework.impl.util;
using common.interfaces.entities;
using System.Reflection;
using Diagrams;
using jm726.lib.wrapper.logger;
using Diagrams.Common;

namespace StAndrews.NeworkIsland.Framework.Util.Logger.Opensim {
    /// <summary>
    /// TODO IMPLEMENTED
    /// Class which is used to get logger objects which have been configured to work with opensim types UUID, Vector3 and the utility type Parameters.
    /// Simply a wrapper for StaticLogger which registers 3 delegates for the above mentioned types then creates a new XmlLogWriter.
    /// </summary>
    public class OpenSimLogReader : XmlLogReader {
        private Dictionary<string, UUID> _factory;
        private static readonly Type UUIDType = typeof(UUID);
        private readonly Vector3 _hostPos;

        public OpenSimLogReader(Dictionary<string, UUID> factory, Vector3 hostPos)
            : base() {
            _factory = factory;
            _hostPos = hostPos;
        }

        public OpenSimLogReader(Dictionary<string, UUID> factory, object instance, Vector3 hostPos)
            : base(instance) {
            _factory = factory;
            _hostPos = hostPos;
        }

        protected override object SwitchArgumentBack(XmlNode paramNode, object arg) {
            XmlAttribute idAttr = paramNode.Attributes["IsID"];
            if (idAttr != null && idAttr.Value.Equals("True"))
                return _factory[arg as string];

            XmlAttribute idArrayAttr = paramNode.Attributes["IsIDArray"];
            if (idArrayAttr != null && idArrayAttr.Value.Equals("True")) {
                object[] idArray = (object[])arg;
                return ((object[])arg).Select<object, UUID>(id => {
                    string idStr = id as string;
                    if (idStr != null && _factory.ContainsKey(idStr))
                        return _factory[idStr];
                    else if (id is UUID)
                        return (UUID)id;
                    return UUID.Zero;
                }).Where<UUID>(id => !id.Equals(UUID.Zero)).ToArray();
            }

            XmlAttribute vectorAttr = paramNode.Attributes["IsRelativeVector"];
            if (vectorAttr != null && vectorAttr.Value.Equals("True") && arg is Vector3) 
                return ((Vector3) arg) + _hostPos;
            return arg;
        }
    }
}
