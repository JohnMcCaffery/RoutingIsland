using System;
using System.Collections.Generic;
using common;
using System.Collections;
using OpenMetaverse;
namespace Diagrams.Common {
    public interface IKeyTable<TValue> : IEnumerable<TValue> {
        /// <summary>
        ///   Get the number of Objects in the collection
        /// </summary>
        int Count { get; }

        /// <summary>
        ///   Get an enumerable Object for the keys so the keys can be iterated through using a foreach loop
        /// </summary>
        //List<UUID> Keys { get; }

        /// <summary>
        ///   Index into the map
        /// </summary>
        /// <param name = "id"></param>
        /// <returns></returns>
        TValue this[UUID id] { get; set; }

        /// <summary>
        /// Add an Object to the map
        /// </summary>
        /// <param name="key">The key to map the Object to</param>
        /// <param name="value">The Object to map</param>
        void Add(UUID key, TValue value);
        
        /// <summary>
        /// Remove all Values from the table
        /// </summary>
        void Clear();

        /// <summary>
        /// Check if a specified key is contained within the collection
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool ContainsKey(UUID key);

        /// <summary>
        /// Return a copy of the key table that can be modified without modifying this key table.
        /// </summary>
        /// <returns>A copy of this key table.</returns>
        IKeyTable<TValue> Copy();

        /// <summary>
        /// Remove an Object from the map
        /// </summary>
        /// <param name="key"></param>
        void Remove(UUID key);
    }
}
