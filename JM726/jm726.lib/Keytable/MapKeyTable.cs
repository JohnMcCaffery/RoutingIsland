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
using Diagrams.Common;
using OpenMetaverse;
using System.Collections;

namespace Diagrams {
    public class MapKeyTable<TValue> : IKeyTable<TValue> {
        private Dictionary<UUID, TValue> _map;

        public MapKeyTable() {
            _map = new Dictionary<UUID, TValue>();
        }

        private MapKeyTable(Dictionary<UUID, TValue> toCopy) {
            _map = new Dictionary<UUID, TValue>(toCopy);
        }

        public int Count {
            get { return _map.Count; }
        }

        public TValue this[UUID id] {
            get { return _map[id]; }
            set { _map[id] = value; }
        }

        public void Add(UUID key, TValue value) {
            lock (_map)
                _map.Add(key, value);
        }

        public void Clear() {
            lock (_map)
                _map.Clear();
        }

        public bool ContainsKey(UUID key) {
            return _map.ContainsKey(key);
        }

        public IKeyTable<TValue> Copy() {
            lock (_map)
                return new MapKeyTable<TValue>(_map);
        }

        public void Remove(UUID key) {
            lock (_map)
                _map.Remove(key);
        }

        public IEnumerator<TValue> GetEnumerator() {
            return _map.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return _map.GetEnumerator();
        }
    }
}
