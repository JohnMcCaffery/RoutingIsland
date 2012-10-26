////MRM:C#

//using OpenSim.Region.OptionalModules.Scripting.Minimodule;
//using System;

//namespace OpenSim {
//    class MiniModule : MRMBase {

//        public override void Start() {
//            IObject[] toRemove = new IObject[World.Objects.Count];
//            int i = 0;
//            foreach (IObject obj in World.Objects) {
//                if (obj != null && obj.Exists && obj.Name != null) {
//                    string name = obj.Name;
//                    if (name.StartsWith("Router") || name.StartsWith("End Point") || name.StartsWith("Link between ") || name.StartsWith("Packet "))
//                        toRemove[i++] = obj;
//                }
//            }
//            foreach (IObject obj in toRemove) {
//                if (obj != null) {
//                    Console.WriteLine("Removing " + obj.Name);
//                    World.Objects.Remove(obj);
//                }
//            }
//        }

//        public override void Stop() {

//        }
//    }
//}