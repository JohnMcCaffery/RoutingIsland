using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using common.framework.interfaces.basic;

namespace Diagrams.MRM.Controls.Buttons {
    public interface IChatButtonSetup {
        int Channel {
            get;
        }

        IPrim HostPrim { get; }

        UUID RequestButton(string name);

        event Action<string, UUID> ButtonRegistered;

        void Stop();
    }
}
