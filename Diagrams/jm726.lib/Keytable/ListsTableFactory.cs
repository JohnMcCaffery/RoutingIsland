using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Diagrams.Common.interfaces.keytable;
using common;

namespace Diagrams.Common.keytable {
    public class ListsTableFactory : IKeyTableFactory {
        public IKeyTable<TValue> MakeKeyTable<TValue>() {
            return new ListsKeyTable<TValue>();
        }
    }
}
