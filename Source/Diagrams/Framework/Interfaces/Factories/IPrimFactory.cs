﻿/*************************************************************************
Copyright (c) 2012 John McCaffery 

This file is part of Routing Project.

Routing Project is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Routing Project is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Routing Project.  If not, see <http://www.gnu.org/licenses/>.

**************************************************************************/

#region Namespace imports

using System;
using System.Collections.Generic;
using System.Drawing;
using Diagrams;
using OpenMetaverse;
using common.framework.interfaces.basic;
using Diagrams.Control.impl.Buttons;
using Diagrams.MRM.Controls.Buttons;
using Diagrams.MRM.Controls;

#endregion

namespace common.interfaces.entities {
    public delegate void ButtonRegisteredDelegate (UUID button, string name);
    /// <summary>
    /// Interface for singleton object used to interact with the virtual world. Can create primitives and listen for events. Can also look up primitives based on either ID of name.
    /// This interface is the one the system sees. It doesn't expose any details of the virtual world where the primitives exist.
    /// </summary>
    public interface IPrimFactory {
        /// <summary>
        /// Get a primitive based on its ID.
        /// If the given ID is not known will throw an exception.
        /// </summary>
        /// <param name="senderID">The ID of the primitive to get.</param>
        /// <returns>The primitive.</returns>
        IPrim this[UUID id] { get; }

        /// <summary>
        /// Get a primitive based on its name.
        /// If the given name is not known or there are more than one primitives with that name will throw an exception.
        /// </summary>
        /// <param name="name">The name of the primitive to get.</param>
        IPrim this[String name] { get; }

        /// <summary>
        /// The prim the script is running in.
        /// </summary>
        IPrim Host { get; }

        /// <summary>
        /// The name of the owner of the script.
        /// </summary>
        string Owner { get; }

        /// <summary>
        ///   Subscribe to this event to be notified of any chat messages not generated by one of the primitives managed by the system. 
        ///   All avatar chat messages will generate events. Any chat message generated by a primitive which was created or tracked using this class will be ignored.
        /// </summary>
        /// <summary>
        ///   Subscribe to this event to be notified of any chat messages not generated by one of the primitives managed by the system. 
        ///   All avatar chat messages will generate events. Any chat message generated by a primitive which was created or tracked using this class will be ignored.
        /// </summary>
        event ChatDelegate OnChat;
        
        event ButtonRegisteredDelegate OnButtonRegistered;

        IEnumerable<IPrim> AllPrims { get; }

        /// <summary>
        /// Create a primitive that does not yet exist in world
        /// </summary>
        /// <param name="name">The _name of the primitive</param>
        /// <param name="creator">The _name of the _creator of the primitive</param>
        /// <param name="listen">Whether to listen for touch events on this primitive</param>
        /// <param name="position">Where in world the primitive is to be created</param>
        /// <param name="scale">What size the primitive is to be, if set to default(Vector3) no scale should be set</param>
        /// <param name="colour">What colour the primitive is to be, if set to default(Colour) no colour should be set</param>
        /// <param name="shape">What shape the primitive is to be, if set to PrimType.Unknown no shape should be set</param>
        /// <returns>A new Prim object with the values specified</returns>
        IPrim MakePrim(string name, Vector3 position, Color colour = default(Color), Vector3 scale = default(Vector3), PrimType shape = PrimType.Unknown, Quaternion rotation = default(Quaternion));

        /// <summary>
        /// Shutdown, mainly shuts down the remove thread
        /// </summary>
        void Shutdown();

        /// <summary>
        /// Remove all Objects that this factory has created
        /// </summary>
        void ClearCreated();

        /// <summary>
        /// Check whether a primitive exists.
        /// </summary>
        /// <param name="senderID">The ID of the primitive to check for.</param>
        bool PrimExists(UUID id);

        /// <summary>
        /// Check whether a primitive exists.
        /// </summary>
        /// <param name="name">The name of the primitive to check for.</param>
        bool PrimExists(string name);

        /// <summary>
        /// Get a list of all the primitives with a given name.
        /// </summary>
        /// <param name="name">The name to search for.</param>
        List<IPrim> GetPrimsWithName(string name);

        /// <summary>
        /// Add a chat listener for a specific channel.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="chatListener"></param>
        void AddChannelListener(int channel, ChatDelegate chatListener);
        
        /// <summary>
        /// Remove a chat listener for a specific channel.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="chatListener"></param>
        void RemoveChannelListener(int channel, ChatDelegate chatListener);

        /// <summary>
        /// Register a prim as the root of a link set where all children are to be listened to as buttons.
        /// </summary>
        /// <param name="rootID">The ID of the root prim of the link set.</param>
        void AddLinkSetRoot(UUID rootID);

        /// <summary>
        /// Make a button tied to a specific name. Any primitives with the given name and the following script embedded in them will trigger this button when touched.
        /// </summary>
        /// <param name="name">The name of the button.</param>
        /// <param name="permissions">The permission set for the button. The button will only trigger and event if the permission set authorizes it.</param>
        /// <returns>A button object that allows listeners to be attached to the button.</returns>
        IButton MakeButton(string name, IPermissions permissions);

        /// <summary>
        /// Make a button specifying the prim to be listened to as the button.
        /// </summary>
        /// <param name="name">The name of the button.</param>
        /// <param name="permissions">The permission set for the button. The button will only trigger and event if the permission set authorizes it.</param>
        /// <param name="id">The in world primitives which represents the button.</param>
        /// <returns>A button object that allows listeners to be attached to the button.</returns>
        IButton MakeButton(string name, IPermissions permissions, params UUID[] ids);
    }
}