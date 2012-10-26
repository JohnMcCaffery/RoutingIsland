using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace jm726.lib.wrapper {
    public interface IWrapper<out TToWrap> where TToWrap : class {
        /// <summary>
        /// The instance to interact with in order for the interactions to be spied.
        /// </summary>
        TToWrap Instance { get; }
        /// <summary>
        /// Whether or not to generate events whenever an event is trigged from the instance being spied on.
        /// </summary>
        bool Listen { get; set; }
        /// <summary>
        /// The type of the interface being wrapped.
        /// </summary>
        Type WrappedInterface { get; }
    }
}
