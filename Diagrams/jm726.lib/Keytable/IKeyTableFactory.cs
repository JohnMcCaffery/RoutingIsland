using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Diagrams.Common.interfaces.keytable {
    public interface IKeyTableFactory {
        /// <summary>
        /// Make a new KeyTable
        /// </summary>
        /// <typeparam name="TValue">The type of value which the key table manages</typeparam>
        /// <returns>The new key table</returns>
        IKeyTable<TValue> MakeKeyTable<TValue>();
    }
}
