#region Namespace imports

using System.Collections;
using System.Collections.Generic;
using OpenMetaverse;
using Diagrams.Common;
using common.framework.interfaces.entities;

#endregion

namespace common {
    /// <summary>
    ///   Wrapper class to provide hash set functionality where the key set is a Key value and the values are some generically specified type
    /// 
    ///   Hash table is used because hashing provides fast lookup. Linked lists are used for fast iteration. This class is used for two 
    ///   things. Firstly indexing keys to Objects. The initialisation, model and view layers all index a series of Objects by their key.
    ///   To do this they use one of these Objects. These Objects allow fast iteration through them by storing keys and values as linked list.
    ///   They also allow fast lookup through the hash table. Thus this class is optimized for both the operations it has to perform frequently.
    ///   Because of the relatively small number of change operations to that will be applied to these collections and the small amount of data
    ///   they will store the fact that updates are slow and data is duplicated is not a problem.
    /// </summary>
    /// <typeparam name = "TValue">The type of the values the hash set stores</typeparam>
    public class ListsKeyTable<TValue> : /*List<TValue>*/ IKeyTable<TValue> {
        private readonly List<UUID> _keys;
        private readonly Hashtable _map;
        private readonly List<TValue> _values;

        /// <summary>
        ///   Initialise the private fields which store the data
        /// </summary>
        public ListsKeyTable() {
            _keys = new List<UUID>();
            _values = new List<TValue>();
            _map = new Hashtable(100, .5f);
        }

        /// <summary>
        ///   Index into the map
        /// </summary>
        /// <param name = "index"></param>
        /// <returns></returns>
        public TValue this[UUID index] {
            get { return (TValue) _map[index]; }
            set { _map[index] = value; }
        }

        /// <summary>
        ///   Get an enumerable Object for the keys so the keys can be iterated through using a foreach loop
        /// </summary>
        public List<UUID> Keys {
            get { return _keys; }
        }

        /// <summary>
        ///   Get the number of Objects in the collection
        /// </summary>
        public int Count {
            get { return _map.Count; }
        }    

        /// <summary>
        /// Remove all Values from the table
        /// </summary>
        public void Clear() {
            lock (this) {
                _keys.Clear();
                _map.Clear();
                _values.Clear();
            }
        }

        /// <summary>
        ///   Check if a specified key is contained within the collection
        /// </summary>
        /// <param name = "key"></param>
        /// <returns></returns>
        public bool ContainsKey(UUID key) {
            return _map.ContainsKey(key);
        }

        /// <summary>
        ///   Add an Object to the map
        /// </summary>
        /// <param name = "key">The key to map the Object to</param>
        /// <param name = "value">The Object to map</param>
        public void Add(UUID key, TValue value) {
            lock (this) {
                _values.Add(value);
                _keys.Add(key);
                _map.Add(key, value);
            }
        }

        /// <summary>
        ///   Remove an Object from the map
        /// </summary>
        /// <param name = "key"></param>
        public void Remove(UUID key) {
            lock (this) {
                _values.Remove((TValue)_map[key]);
                _keys.Remove(key);
                _map.Remove(key);
            }
        }

        /// <summary>
        /// Return a copy of the key table that can be modified without modifying this key table.
        /// </summary>
        /// <returns>A copy of this key table.</returns>
        public IKeyTable<TValue> Copy() {
            lock (this) {
                var x = new ListsKeyTable<TValue>();
                foreach (UUID key in Keys)
                    x.Add(key, this[key]);
                return x;
            }
        }

        /// <inheritdoc />
        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() {
            return _values.GetEnumerator();
        }

        /// <inheritdoc />
        public IEnumerator GetEnumerator() {
            return _values.GetEnumerator();
        }
    }
}