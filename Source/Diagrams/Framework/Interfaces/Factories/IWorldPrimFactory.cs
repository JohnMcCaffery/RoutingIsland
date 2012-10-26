using System;
using OpenMetaverse;
using common.interfaces.entities;
using common.framework.interfaces.basic;

namespace Diagrams {
    /// <summary>
    /// A primitive factory which interacts with a virtual world. Gets objects from the world and then allows primitives to be registered with the world. Works in conjunction with IWorldPrims.
    /// </summary>
    /// <typeparam name="TObject">The type of object the virtual world provides to model an object in world.</typeparam>
    public interface IWorldPrimFactory<TObject> : common.interfaces.entities.IPrimFactory {

        /// <summary>
        /// Use this to register a virtual primitive with the world. This will cause the primitive to appear in world.
        /// <param name="prim">The primitive being registered.</param>
        /// 	<param name="updateListener">The method which can be called to update the primitive from its wrapped TObject.</param>
        /// 	<param name="updateObject">Update the Object which the prim wraps to interact with the VW.</param>
        /// 	<returns>A TObject which will be used by the new prim to interact with the world.</returns>
        /// </summary>
        TObject RegisterPrim(IPrim prim, Action updateListener, Action<TObject> updateObject);

        /// <summary>
        /// Use this to register a virtual primitive that that should already exist in world. Will throw an exception if the primitive does not actually exist in world.
        /// <param name="prim">The primitive being registered.</param>
        /// 	<param name="id">The ID of the IObject in world that the new prim is to wrap.</param>
        /// 	<param name="updateListener">The method which can be called to update the primitive from its wrapped TObject.</param>
        /// 	<param name="updateObject">Update the Object which the prim wraps to interact with the VW.</param>
        /// 	<returns>A TObject which will be used by the new prim to interact with the world.</returns>
        /// </summary>
        TObject RegisterPrim(IPrim prim, OpenMetaverse.UUID id, Action updateListener, Action<TObject> updateObject);

        /// <summary>
        /// Remove a prim from the virtual world. Deregisters its update listener.
        /// </summary>
        /// <param name="id">The ID of the primitive to remove.</param>
        /// <returns>True if the object was successfully removed. False otherwise</returns>
        bool RemovePrim(UUID id);

        /// <summary>
        /// Used if the in world object representing the prim has been removed but the prim is still required to be in world. Should create a new in world primitive and map it to the old primitive's senderID.
        /// </summary>
        TObject RenewObject(UUID id);
    }
}
