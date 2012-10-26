using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using OpenSim.Region.OptionalModules.Scripting.Minimodule;

namespace scripts {
    /// <summary>
    /// Delegate which can be used to add listeners to the world
    /// </summary>
    /// <param name="target">The target Object to add a listener</param>
    public delegate void ListenerAdderDelegate(UUID target);

    /// <summary>
    /// Delegate method used to flag that the system needs to be shut down
    /// </summary>
    /// <param name="shutdownReason">The reason why the system needed to be shut down</param>
    public delegate void ShutdownDelegate(string shutdownReason);

    /// <summary>
    /// Interface defining a class which will act as a system which is to be run from inside opensim.
    /// 
    /// The user specifies a configuration file which points to an implementation of this interface. 
    /// That implementation is then instantiated in its own application domain from within an MRM script.
    /// 
    /// The classes within this (the scripts) assembly are the classes which handle this first stage. This interface
    /// is the interface by which the scripts assembly interacts with whatever system it is being used to run.
    /// </summary>
    public interface ISystem {
        /// <summary>
        /// Start the system
        /// </summary>
        /// <param name="host">The host which hosts the primitive in which the script is running</param>
        /// <param name="world">The world object which is the key to the in world scenegraph</param>
        /// <param name="listenerAdder">Delegate used to add listeners to primitives within opensim</param>
        /// <param name="shutdown">Delegate used to flag that the system needs to be shutdown</param>
        void start(IHost host, IWorld world, ListenerAdderDelegate listenerAdder, ShutdownDelegate shutdown);

        /// <summary>
        /// Clear away any primitives the system created and does not want to keep
        /// </summary>
        void clear(UUID id, string sender);

        /// <summary>
        /// Stop the whole system, closing down any threads that are running.
        /// Normally triggered by Opensims internal shutdown mechanism
        /// </summary>
        void stop();

        /// <summary>
        /// What to do on a touch event
        /// </summary>
        /// <param name="touchee">The Object that was touched</param>
        /// <param name="args">The Object that did the touching</param>
        void touched(UUID touchee, TouchEventArgs args);

        /// <summary>
        /// Chat event received
        /// </summary>
        /// <param name="sender">The Object (prim or entity) that chatted</param>
        /// <param name="text">The text they chatted</param>
        void chat(IEntity sender, string text);

        /// <summary>
        /// Chat event received
        /// </summary>
        /// <param name="world">Method used to update the world proxy object</param>
        void updateWorld(IWorld world);
    }
}
