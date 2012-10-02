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

#region Namespace imports

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenMetaverse;
using System.Runtime.Serialization;

#endregion

namespace common.framework.impl.util {
    /// <summary>
    ///   Implementation of the Parameters interface
    /// </summary>
    [DataContract]
    public class Parameters : EventArgs, IEnumerable<KeyValuePair<string, object>> {
        #region Public Static Fields
        /// <summary>
        /// Type of a bool. Stored for convenience.
        /// </summary>
        public static Type BoolType = typeof(bool);
        /// <summary>
        /// Type of a float. Stored for convenience.
        /// </summary>
        public static Type FloatType = typeof(float);
        /// <summary>
        /// Type of a double. Stored for convenience.
        /// </summary>
        public static Type DoubleType = typeof(double);
        /// <summary>
        /// Type of a int. Stored for convenience.
        /// </summary>
        public static Type IntType = typeof(int);
        /// <summary>
        /// Type of a long. Stored for convenience.
        /// </summary>
        public static Type LongType = typeof(long);
        /// <summary>
        /// Type of a string. Stored for convenience.
        /// </summary>
        public static Type StringType = typeof(string);
        /// <summary>
        /// Type of a Color. Stored for convenience.
        /// </summary>
        public static Type ColourType = typeof(Color);
        /// <summary>
        /// Type of a UUID. Stored for convenience.
        /// </summary>
        public static Type IdType = typeof(UUID);
        /// <summary>
        /// Type of a Vector3. Stored for convenience.
        /// </summary>
        public static Type VectorType = typeof(Vector3);
        /// <summary>
        /// Type of a Parameters. Stored for convenience.
        /// </summary>
        public static Type ParameterType = typeof(Parameters);

        #endregion

        #region Private Fields

        /// <summary>
        /// Dictionary storing the parameters.
        /// </summary>
        private Dictionary<string, Object> _parameters;

        #endregion

        #region Constructors

        /// <summary>
        ///   Initialise with no parameters.
        /// </summary>
        public Parameters() {
            _parameters = new Dictionary<string, object>();
        }

        /// <summary>
        /// Initialise with starting parameters set.
        /// 
        /// Parameters must be key value pairs. Keys must be strings and unique.
        /// 
        /// A valid call would be 'new Parameters("Key", new Object())' or 'new Parameters("Key1", new Object(), "Key2", new Object())'
        /// 
        /// This is a very lazy way of programming and should not really be here. It exists purely for convenience
        /// </summary>
        /// <param name="parameters">Varargs parameter. Expected to be a multiple of two.</param>
        public Parameters(params object[] parameters) : this() {
            if (parameters == null) return;
            if (parameters.Length % 2 == 1)
                throw new Exception("Parameters must be added in key, value pairs. Keys must be strings.");

            for (int i = 0; i < parameters.Length; i += 2)
                if (!(parameters[i] is string))
                    throw new Exception("Parameters must be added in key, value pairs. Keys must be strings.");
                else if (_parameters.ContainsKey(parameters[i] as string))
                    throw new Exception("Keys must be unique.");
                else
                    _parameters.Add(parameters[i] as string, parameters[i+1]);
        }

        #endregion

        #region Parameters Properties

        [DataMember]
        public KeyValuePair<string, object>[] Data {
            get {
                return _parameters.ToArray();
            }
            set {
                _parameters = new Dictionary<string, object>();
                //_parameters = new Dictionary<string, object>();
                foreach (KeyValuePair<string, object> pair in value)
                    _parameters.Add(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// Count the number of parameters stored.
        /// </summary>
        public int Count {
            get { return _parameters.Count; }
        }

        #endregion
        
        #region Parameters Methods

        public void Append(Parameters parameters) {
            foreach (KeyValuePair<string, object> pair in parameters.Data)
                Set<object>(pair.Key, pair.Value);
        }

        /// <summary>
        /// Add a new parameter to the collection. If the parameter already existed it is overwritten.
        /// </summary>
        /// <typeparam name="TParamType">The type of the parameter being added.</typeparam>
        /// <param name="key">The key to store param under.</param>
        /// <param name="param">The parameter to add.</param>
        public void Set<TParamType>(String key, TParamType param) {
            if (key == null)
                throw new Exception("Key cannot be null");
            if (param == null)
                throw new Exception("Parameter tied to " + key + " cannot be null");

            if (_parameters.ContainsKey(key))
                _parameters[key] = param;
            else
                _parameters.Add(key, param);
        }

        /// <summary>
        /// Get a parameter specifying the type to return.
        /// 
        /// Will throw an exception of the parameter does not exist or is the wrong type.
        /// Bools are a special case. If a parameter is expected to be a bool but does not exist it will return as false.
        /// </summary>
        /// <param name="key">The method of the parameter.</param>
        public TParameter Get<TParameter>(string key) {
            if (typeof(TParameter).Equals(typeof(bool)))
                return (TParameter) GetBool(key);
            if (!_parameters.ContainsKey(key))
                throw new Exception(key + " is not a valid key.");
            if (!(_parameters[key] is TParameter))
                throw new Exception(key + " is not of type " + typeof(TParameter).Name + ".");
            return (TParameter)_parameters[key];
        }

        /// <summary>
        /// Bools are a special case.
        /// </summary>
        /// <typeparam name="TParameter"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        private object GetBool(string key) {
            if (!_parameters.ContainsKey(key))
                return false;
            if (!(_parameters[key] is bool))
                throw new Exception(key + " is not of type bool.");
            return _parameters[key];
        }

        /// <summary>
        /// Check the that a parameter of the specified type exists.
        /// </summary>
        /// <typeparam name="TCheck">The type to check whether the parameter is.</typeparam>
        /// <param name="key">The parameter to check.</param>
        /// <returns></returns>
        public bool HasParameter<TCheck>(string key) {
            return _parameters.ContainsKey(key) && _parameters[key] is TCheck;
        }

        #endregion

        #region IEnumerable Methods

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
            return _parameters.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        #endregion

        #region Object Methods

        /// <inheritdoc />
        public override bool Equals(object obj) {
            if (!(obj is Parameters)) return false;
            var parameters = obj as Parameters;

            return parameters.All(pair => _parameters.ContainsKey(pair.Key) && _parameters[pair.Key].Equals(pair.Value));
        }

        #endregion
    }
}