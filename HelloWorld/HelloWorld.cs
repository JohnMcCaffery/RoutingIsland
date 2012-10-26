//MRM:C#
//MRM:X -a XMRM/HelloWorld.dll -c Start.HelloWorld
using OpenSim.Region.OptionalModules.Scripting.Minimodule;
using System;

namespace Start {
    public class HelloWorld : MRMBase {
        public override void Start(string[] args) {
            Host.Object.Say("Hello World!");
        }

        public override void Stop() { }
    }
}
