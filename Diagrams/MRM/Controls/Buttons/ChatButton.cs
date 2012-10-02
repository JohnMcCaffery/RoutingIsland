using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using common.interfaces.entities;
using OpenMetaverse;
using common.framework.interfaces.basic;
using Diagrams.MRM.Controls.Buttons;

namespace Diagrams.Control.impl.Buttons {
    public class ChatButton : IButton {
        internal const string SELECT = "SELECT";
        private const string TOUCH = "TOUCH";
        private readonly string _name;
        private readonly int _channel;
        private readonly MRMPrimFactory _factory;
        private UUID _owner;

        protected int Channel {
            get { return _channel; }
        }

        public IPrim Prim {
            get { return _factory.ButtonPrims.ContainsKey(Name) ? _factory.ButtonPrims[Name].FirstOrDefault(); }
        }


        private double _selected;
        public double Selected {
            get { return _factory[_factory.ButtonPrims[Name].First()].Glow; }
            set {
                _selected = value;
                if (_factory.ButtonPrims.ContainsKey(Name))
                    foreach (var prim in _factory.ButtonPrims[Name].Select<UUID, IPrim>(id => _factory[id]))
                        prim.Glow = value;
            }
        }

        public ChatButton(MRMPrimFactory factory, string name) {
            _name = name;
            _factory = factory;

        }

        #region IButton Members

        public event EntityTouchedDelegate OnTouched;

        public string Name {
            get { return _name; }
        }

        private void TriggerTouch(string text, UUID id) {
            string[] msg = text.Split(':');
            if (OnTouched != null && Name.Equals(_name) && msg.Length == 4) {
                TouchEventArgs args = new TouchEventArgs();
                args.AvatarID = UUID.Parse(msg[0];
                args.AvatarName = msg[1];
                args.AvatarPosition = Vector3.Parse(msg[2];
                args.TouchPosition = Vector3.Parse(msg[3];
                OnTouched(id, args);
            }
        }

        #endregion
    }
}
