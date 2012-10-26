using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using jm726.lib.wrapper;

namespace jm726.lib.wrapper.spy {
    public interface ISpy<out TToWrap> : IWrapper<TToWrap> where TToWrap : class {
        /// <summary>
        /// Triggered when an event listener is added to an event.
        /// </summary>
        event EventModifyDelegate OnEventAdd;

        /// <summary>
        /// Triggered when an event listener is removed from an event.
        /// </summary>
        event EventModifyDelegate OnEventRemove;

        /// <summary>
        /// Triggered whenever the instance being spied on triggers and event.
        /// </summary>
        event EventTriggeredDelegate OnEventTriggered;

        /// <summary>
        /// Triggered whenever a regular method is called.
        /// </summary>
        event MethodCallDelegate OnMethodCall;

        /// <summary>
        /// Triggered whenever any method is invoked (both special methods such as property getters and setters and regular methods).
        /// </summary>
        event MethodCallDelegate OnMethodEvent;

        /// <summary>
        /// Triggered whenever a properties getter is invoked.
        /// </summary>
        event PropertyInteractDelegate OnPropertyGet;

        /// <summary>
        /// Triggered whenever a properties setter is invoked.
        /// </summary>
        event PropertyInteractDelegate OnPropertySet;
    }
}
