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
using jm726.lib.Serialization;

namespace jm726.lib.wrapper.logger {
    public class MethodCall {
        public string Type { get { return _type; } }
        public string Name { get { return _name; } }
        public string FullName {
            get {
                if (_type.Equals("PropertyGet"))
                    return "get_" + _name;
                else if (_type.Equals("PropertySet"))
                    return "set_" + _name;
                else if (_type.Equals("ListenerAdd"))
                    return "add_" + _name;
                else if (_type.Equals("ListenerRemove"))
                    return "remove_" + _name;
                return _name;
            }
        }
        public string Hash { get { return _hash; } }
        public string Interface { get { return _interface; } }
        public object Return { get { return _return; } set { _return = value; } }
        public object[] Parameters { get { return _parameters; } }

        private string _type;
        private string _name;
        private string _hash;
        private string _interface;
        private object _return;
        private object[] _parameters;
        private MethodInfo _method;

        public MethodCall(string hash, MethodInfo method, object ret, object[] parameters) {
            if (hash == null)
                throw new NullReferenceException("Hash cannot be null.");
            SetType(method, parameters);

            _hash = hash;
            _return = ret;
        }

        public MethodCall(MethodInfo method, object instance, object[] parameters) {
            if (instance == null)
                throw new NullReferenceException("Instance cannot be null.");
            else if (!method.DeclaringType.IsInstanceOfType(instance))
                throw new ArgumentException("Instance must be of type " + method.DeclaringType.Name + ".");
            SetType(method, parameters);

            _hash = instance.GetHashCode().ToString();
            _return = method.Invoke(instance, parameters);
        }

        public MethodCall(XmlNode eventNode, params Assembly[] assemblies) : this (eventNode, (node, parameter) => parameter) { }

        public MethodCall(XmlNode eventNode, Func<XmlNode, object, object> switchParameter) {
            _type = eventNode.Name;
            XmlAttribute nameAttr = eventNode.Attributes["Name"];
            XmlAttribute interfaceAttr = eventNode.Attributes["Interface"];
            XmlAttribute hashAttr = eventNode.Attributes["Hash"];
            
            if (nameAttr == null)
                throw new InvalidOperationException("Unable to create " + _type + ". Name attribute not set.");
            if (interfaceAttr == null)
                throw new InvalidOperationException("Unable to create " + _type + " for " + nameAttr.Value + ". Interface attribute not set.");

            _name = nameAttr.Value;
            _interface = interfaceAttr.Value;
            _hash = hashAttr == null ? "0" : hashAttr.Value;

            if (eventNode.FirstChild == null) {
                _return = null;
                _parameters = new object[0];
                return;
            }

            XmlNode child = eventNode.FirstChild;
            if (child.Name.Equals("Parameters")) {
                _parameters = new object[child.ChildNodes.Count];

                for (int i = 0; i < _parameters.Length; i++) {
                    XmlNode paramNode = child.ChildNodes[i];
                    _parameters[i] = switchParameter(paramNode, ObjectSerializer.DeserializeObject(paramNode));
                }

                child = child.NextSibling;
            } else
                _parameters = new object[0];

            if (child != null && child.Name.Equals("Return"))
                _return = ObjectSerializer.DeserializeObject(child);
        }

        public MethodCall(string type, string name, string intface, string hash, object ret, object[] parameters) {
            if (parameters == null) parameters = new object[0];
            _type = type;
            _name = name;
            _hash = hash;
            _interface = intface;
            _return = ret;
            _parameters = parameters;
        }

        private void SetType(MethodInfo method, object[] parameters) {
            if (method == null)
                throw new NullReferenceException("Method cannot be null.");
            if (parameters == null) parameters = new object[0];

            ParameterInfo[] paramInfos = method.GetParameters();
            if (paramInfos.Length != parameters.Length)
                throw new ArgumentException("Incorrect number of parameters supplied. Expected " + method.GetParameters().Length + ", was " + parameters.Length + ".");
            for (int i = 0; i < paramInfos.Length; i++) {
                ParameterInfo paramInfo = paramInfos[i];
                object param = parameters[i];
                if (!paramInfo.ParameterType.IsInstanceOfType(param) && (paramInfo.ParameterType.IsValueType || param != null))
                    throw new ArgumentException("Parameter " + i + " (" + paramInfo.Name + ") is the wrong type. Expected " + paramInfo.ParameterType.Name + ", was " + (param == null ? "null" : param.GetType().Name) + ".");
            }

            _interface = method.DeclaringType.FullName;

            _parameters = parameters;
            _method = method;

            if (!method.IsSpecialName) {
                _type = "Method";
                _name = method.Name;
            } else if (method.Name.StartsWith("add_")) {
                _type = "ListenerAdd";
                _name = method.Name.Substring(4);
            } else if (method.Name.StartsWith("remove_")) {
                _type = "ListenerRemove";
                _name = method.Name.Substring(7);
            } else if (method.Name.StartsWith("get_")) {
                _type = "PropertyGet";
                _name = method.Name.Substring(4);
            } else if (method.Name.StartsWith("set_")) {
                _type = "PropertySet";
                _name = method.Name.Substring(4);
            }
        }

        public XmlNode Serialize(XmlDocument doc) {
            return Serialize(doc, (paramInfo, param) => null, false);
        }

        public XmlNode Serialize(XmlDocument doc, bool ignoreReturn) {
            return Serialize(doc, (paramInfo, param) => null, ignoreReturn);
        }

        public XmlNode Serialize(XmlDocument doc, Func<ParameterInfo, object, XmlNode> switchParameter) {
            return Serialize(doc, switchParameter, false);
        }

        public XmlNode Serialize(XmlDocument doc, Func<ParameterInfo, object, XmlNode> switchParameter, bool ignoreReturn) {
            XmlNode methodNode = doc.CreateElement(Type);

            XmlAttribute interfaceAttr = doc.CreateAttribute("Interface");
            XmlAttribute nameAttr = doc.CreateAttribute("Name");
            XmlAttribute hashAttr = doc.CreateAttribute("Hash");

            interfaceAttr.Value = Interface;
            nameAttr.Value = Name;
            hashAttr.Value = Hash;

            methodNode.Attributes.Append(interfaceAttr);
            methodNode.Attributes.Append(nameAttr);
            methodNode.Attributes.Append(hashAttr);

            if (Parameters != null && Parameters.Length > 0 && !_type.Equals("ListenerAdd") && !_type.Equals("ListenerRemove")) {
                XmlNode parametersNode = doc.CreateElement("Parameters");
                ParameterInfo[] pTypes = _method == null ? null : _method.GetParameters();
                for (int i = 0; i < Parameters.Length; i++)
                    if (pTypes == null)
                        parametersNode.AppendChild(ObjectSerializer.SerializeObject(Parameters[i], doc, "Parameter"));
                    else {
                        XmlNode paramNode = switchParameter(pTypes[i], _parameters[i]);
                        if (paramNode != null)
                            parametersNode.AppendChild(paramNode);
                        else
                            parametersNode.AppendChild(ObjectSerializer.SerializeObject(_parameters[i], doc, pTypes[i].Name));
                    }

                methodNode.AppendChild(parametersNode);
            }
            if (_return != null && !ignoreReturn) 
                methodNode.AppendChild(ObjectSerializer.SerializeObject(_return, doc, "Return"));

            return methodNode;
        }

        public override string ToString() {
            return Type + " " + Name;
        }
    }
}
