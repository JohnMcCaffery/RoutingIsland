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
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using log4net;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Text;
using System.IO;
using common;
using common.interfaces.entities;
using OpenMetaverse;
using jm726.lib.wrapper.logger;
using jm726.lib.Serialization;
using Diagrams.Common;
using System.Drawing;

namespace StAndrews.NeworkIsland.Framework.Util.Logger.Opensim {

    /// <summary>
    /// TODO IMPLEMENTED
    /// Class which is used to get logger objects which have been configured to work with opensim types UUID, Vector3 and the utility type Parameters.
    /// Simply a wrapper for StaticLogger which registers 3 delegates for the above mentioned types then creates a new XmlLogWriter.
    /// </summary>
    public class OpenSimLogWriter<TToLog> : XmlLogWriter<TToLog> where TToLog : class {
        private IKeyTable<string> _factory;
        private static readonly Type UUIDType = typeof(UUID);
        private readonly Vector3 _hostPos;

        public OpenSimLogWriter(IKeyTable<string> factory, TToLog instance, Vector3 hostPos, bool ignoreReturn = false, bool recursive = true)
            : base(instance, ignoreReturn, recursive) {
            _factory = factory;
            _hostPos = hostPos;
        }

        protected override XmlNode SwitchArgument(ParameterInfo param, object arg) {
            if (arg is Vector3) {
                XmlNode vectorNode = ObjectSerializer.SerializeObject(((Vector3)arg) - _hostPos, Log, param.Name);
                XmlAttribute vectorAttr = Log.CreateAttribute("IsRelativeVector");
                vectorAttr.Value = "True";
                vectorNode.Attributes.Append(vectorAttr);
                return vectorNode;
            } else if ((arg is UUID) && _factory.ContainsKey((UUID)arg)) {
                XmlNode idNode = ObjectSerializer.SerializeObject(_factory[(UUID)arg], Log, param.Name);
                XmlAttribute idAttr = Log.CreateAttribute("IsID");
                idAttr.Value = "True";
                idNode.Attributes.Append(idAttr);
                return idNode;
            } else if (arg.GetType().IsArray && arg.GetType().GetElementType().Equals(typeof(UUID))) {
                object[] ids = ((UUID[])arg).Select<UUID, object>(id => _factory.ContainsKey(id) ? (object) _factory[id] : (object) id).ToArray();
                XmlNode idArrayNode = ObjectSerializer.SerializeObject(ids, Log, param.Name);
                XmlAttribute idAttr = Log.CreateAttribute("IsIDArray");
                idAttr.Value = "True";
                idArrayNode.Attributes.Append(idAttr);
                return idArrayNode;
            }
            return null;
        }
    }
}