//MRM:C#
//@DEPENDS:SH.Scripts.dll  

using OpenSim.Region.OptionalModules.Scripting.Minimodule;
using scripts;
using System;

namespace OpenSim {
    class MiniModule : MRMBase {
        private EntryPoint ep;

        public override void Start() {
            ep = new EntryPoint("C:/Users/jm726/Documents/Public/Routing Project/ConfigFolder/Util.config");
            ep.Start(Host, World);
        }

        public override void Stop() {
            ep.Stop();
        }
    }
}