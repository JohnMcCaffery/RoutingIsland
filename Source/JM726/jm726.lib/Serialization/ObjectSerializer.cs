/*************************************************************************
Copyright (c) 2012 John McCaffery 

This file is part of JohnLib.

JohnLib is free software: you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

JohnLib is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License
along with JohnLib.  If not, see <http://www.gnu.org/licenses/>.

**************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Xml.Serialization;
using System.Xml;
using OpenMetaverse;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization;
using System.Collections;

namespace jm726.lib.Serialization {
    public static class ObjectSerializer {
        #region Static

        #region Fields

        private static NetDataContractSerializer serializer = new NetDataContractSerializer();

        /// <summary>
        /// Logger used to make logging calls.
        /// </summary>
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ObjectSerializer));

        #endregion

        #region Methods

        #region Public

        /// <summary>
        /// Serialize an object. The node returned will belong to the given document.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="doc">The document to create the node for.</param>
        /// <param name="name">The name of the XML node to return. By default is the name of the type of the object being serialized.</param>
        /// <returns>An XML node containing all the information necessary to recreate the object in a new process.</returns>
        public static XmlNode SerializeObject(object obj, XmlDocument doc, string name = null) {
            if (obj == null)
                return doc.CreateElement(name == null ? "Null" : name);

            name = GetName(obj.GetType(), name);

            XmlNode objectNode = doc.CreateElement(name);
            XmlNode objectStateNode = SerializeObjectState(obj, doc);

            objectNode.AppendChild(objectStateNode);
            
            return objectNode;
        }

        public static object DeserializeObject(XmlNode objNode) {
            if (objNode.ChildNodes.Count == 0)
                return null;

            XmlNode stateNode = objNode.ChildNodes[0];

            try {
                return serializer.Deserialize(new MemoryStream(ASCIIEncoding.Default.GetBytes(stateNode.OuterXml)));
            } catch (InvalidOperationException e) {
                throw new SerializationException("Unable to deserialize. Are you missing a required type?", e);
            } catch (SerializationException e) {
                throw new SerializationException("Unable to deserialize. Are you missing a required type?", e);
            }
        }

        #endregion

        #region Private

        #region Serialize

        private static string GetName(Type type, string originalName = null) {
            if (originalName != null) 
                return originalName;
            if (!type.IsGenericType) 
                return type.Name.Replace("[]", "_Collection");

            string name = type.Name;

            int indexOfGeneric = name.IndexOf('`');
            int indexOfBrackets = name.IndexOf("[]");

            if (indexOfGeneric > 0 && indexOfBrackets > 0)
                name = name.Substring(0, indexOfGeneric) + name.Substring(indexOfBrackets);
            else if (indexOfGeneric > 0)
                name = name.Substring(0, indexOfGeneric);

            return name.Replace("[]", "_Collection");
        }

        /// <summary>
        /// Serialize the state of an object. Turns any values stored in public fields or properties and any values held within this node if it is a collection into XML.
        /// </summary>
        /// 
        /// <param name="obj">The object being serialized.</param>
        /// <param name="doc">The document to create the node for.</param>
        /// <returns>An XML node which represents the entire current state of the object.</returns>
        private static XmlNode SerializeObjectState(object obj, XmlDocument doc) {
            string objectXML;
            StringBuilder sb = new StringBuilder();
            serializer.WriteObject(new XmlTextWriter(new StringWriter(sb)), obj);
            objectXML = sb.ToString();

            XmlDocument tmpDoc = new XmlDocument();
            tmpDoc.LoadXml(objectXML);

            return doc.ImportNode(tmpDoc.DocumentElement, true);
        }

        #endregion

        #region Deserialize

        #endregion

        #endregion

        #endregion

        #endregion
    }
}
