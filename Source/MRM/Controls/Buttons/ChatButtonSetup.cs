using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using common.interfaces.entities;
using Diagrams.Common;
using common.framework.interfaces.basic;
using System.Threading;
using JM726.Lib.Static;

namespace Diagrams.Control.impl.Controls.Buttons {
    public class ChatButtonSetupSingle : IChatButtonSetup {
        private const int INIT_CHANNEL = -50;
        private const string INIT = "INIT";
        private int _channel;
        private HashSet<string> _boundButtons, _requestedButtons;
        private UUID _owner;
        private IPrim _host;
        private bool _requesting;

        public IPrim HostPrim {
            get { return _host; }
        }

        public ChatButtonSetupSingle(IPrimFactory factory, IPrim host) {
            _host = host;
            _owner = host.Owner;
            _cont = true;

            _boundButtons = new HashSet<string>();
            _requestedButtons = new HashSet<string>();

            Random r = new Random();
            _channel = r.Next(int.MinValue, -1);

            factory.AddChannelListener(_channel, (button, id, text, channel) => {
                string[] msg = text.Split(',');
                UUID owner;
                if (msg.Length == 2 && msg[0].Equals(INIT) && UUID.TryParse(msg[1], out owner) && owner.Equals(_owner)) {
                    _boundButtons.Add(button);
                    if (_requestedButtons.Contains(button)) {
                        _requestedButtons.Remove(button);
                        if (ButtonRegistered != null)
                            ButtonRegistered(button, _owner);
                    }
                }
            });

            NotifyChannel();
        }

        private void NotifyChannel() {
            Thread t = new Thread(() => {
                _host.Say(INIT_CHANNEL, string.Format("{0},{1}", INIT, _channel));
                _requesting = true;
                JM726.Lib.Static.Util.Wait(500);
                _requesting = false;
                if (_requestedButtons.Count > 0) 
                    NotifyChannel();
            });
            t.Name = "Button request thread.";
            if (_cont)
                t.Start();
        }

        public int Channel {
            get {
                return _channel;
            }
        }

        public UUID RequestButton(string button) {
            if (_boundButtons.Contains(button))
                return _owner;
            else {
                _requestedButtons.Add(button);
                if (!_requesting)
                    NotifyChannel();
            }
            return UUID.Zero;
        }

        private bool _cont;

        public void Stop() {
            _cont = false;
        }

        public event Action<string, UUID> ButtonRegistered;
    }

    class ChatButtonSetupMultiple : IChatButtonSetup {
        public int Channel {
            get {
                throw new System.NotImplementedException();
            }
            set {
            }
        }

        #region IChatButtonSetup Members


        public UUID RequestButton(string name) {
            throw new NotImplementedException();
        }

        public event Action<string, UUID> ButtonRegistered;

        #endregion

        #region IChatButtonSetup Members


        public void Stop() {
            throw new NotImplementedException();
        }

        #endregion

        #region IChatButtonSetup Members


        public IPrim HostPrim {
            get { throw new NotImplementedException(); }
        }

        #endregion
    }
}
