using System;
using common;
using log4net;

namespace Diagrams {
    // TODO IMPLEMENTED
    public abstract class Process {
        public static Process CreateProcess<TItem>(string name, Action<TItem> processor, TItem item) {
            return new GenericProcess<TItem>(name, item, processor);
        }
        public static Process CreateProcess(string name, Action processor) {
            return new GenericProcess<object>(name, processor);
        }

        public abstract void Run();

        #region Nested type: GenericProcess

        private class GenericProcess<TItem> : Process {
            private readonly TItem _item;
            private readonly string _name;
            private readonly Action<TItem> _processor;
            private readonly Action _work;
            private readonly ILog logger = LogManager.GetLogger(typeof (TItem).FullName);

            public GenericProcess(string name, TItem item, Action<TItem> processor) {
                if (item == null)
                    throw new Exception("Could not queue process, item cannot be null");
                _name = name;
                _item = item;
                _processor = processor;
            }

            public GenericProcess(string name, Action processor) {
                _name = name;
                _work = processor;
            }

            public override void Run() {
                try {
                    if (_work == null)
                        _processor(_item);
                    else
                        _work();
                }
                catch (Exception e) {
                    logger.Debug("Problem running " + _name + " on " + _item, e);
                }
            }
        }

        #endregion
    }
}