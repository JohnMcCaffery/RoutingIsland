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
            _map.Add(key, value);
        }

        public void Clear() {
            _map.Clear();
        }

        public bool ContainsKey(UUID key) {
            return _map.ContainsKey(key);
        }

        public IKeyTable<TValue> Copy() {
            return new MapKeyTable<TValue>(_map);
        }

        public void Remove(UUID key) {
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
