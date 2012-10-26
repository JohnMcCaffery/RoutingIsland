using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Diagrams.Common.interfaces.keytable;

namespace Diagrams.Common.keytable {
    public class MapTableFactory : IKeyTableFactory {
        public IKeyTable<TValue> MakeKeyTable<TValue>() {
            return new MapKeyTable<TValue>();
        }
    }
}
