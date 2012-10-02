//MRM:C#
//@DEPENDS:SH.Scripts.dll  

using OpenSim.Region.OptionalModules.Scripting.Minimodule;
using scripts;

namespace OpenSim {
    class MiniModule : MRMBase {
        private EntryPoint ep;

        public override void Start() {
            ep = new EntryPoint("C:/Users/user/University/SH Project/Project/Diss/Release/ConfigFolder/Default.config");
            ep.Start(Host, World);
        }

        public override void Stop() {
            ep.Stop();
        }
        /*
        public override void Stop() {
            DB.Print("MiniModule stopping");
            ep.Stop(Host);
        }*/

    }
}