/*************************************************************************
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

using System;
using System.Reflection;
using OpenMetaverse;
using common.model.framework.interfaces;
using common.framework.impl.util;

namespace Diagrams {
    public class TouchEventArgs : EventArgs {
        public string AvatarName;
        public UUID AvatarID;
        public Vector3 AvatarPosition;
        public Vector3 TouchPosition;

        //public Vector3 TouchBiNormal;
        //public Vector3 TouchNormal;

        //public Vector2 TouchUV;
        //public Vector2 TouchST;

        //public int TouchMaterialIndex;
        //public uint LinkNumber;
    }

    /// <summary>
    ///   Delegate for received packets.
    /// </summary>
    /// <param method = "at">The entity the packet is being delivered a.t</param>
    /// <param method = "packet">The packet being delivered.</param>
    public delegate void PacketReceivedDelegate(UUID at, IPacket packet);

    /// <summary>
    /// Delegate for changes to a _link.
    /// </summary>
    /// <param name="link">The _link that changed.</param>
    /// <param name="weight">The new weight of the link.</param>
    public delegate void WeightChangedDelegate(UUID link, float weight);

    /// <summary>
    /// Delegate for when a entity is moved.
    /// </summary>
    /// <param name="entity">The ID of the entity that was moved.</param>
    /// <param name="toucherID">The position that the entity used to be at.</param>
    public delegate void EntityMovedDelegate(UUID entity, Vector3 oldPosition, Vector3 newPosition);

    /// <summary>
    /// Triggered when a entity is moved.
    /// </summary>
    /// <param name="entity">The ID of the entity that was moved.</param>
    /// <param name="toucherID">The ID of the user that touched the entity.</param>
    /// <param name="toucherName">The method of the user that touched the entity.</param>
    /// <param name="toucherPos">The position of the toucher in the world.</param>
    /// <param name="touchPos">Where the entity was touched.</param>
    public delegate void EntityTouchedDelegate(UUID source, TouchEventArgs args);

    /// <summary>
    /// Delegate for when a entity is moved.
    /// </summary>
    /// <param name="entity">The ID of the entity that was moved.</param>
    public delegate void EntityDeletedDelegate(UUID entity);

    /// <summary>
    /// Delegate for when a user chats.
    /// </summary>
    /// <param name="chatter">The user who chatted the message.</param>
    /// <param name="chatterID">The ID of the user who chatted the message.</param>
    /// <param name="text">The text that was chatted.</param>
    public delegate void ChatDelegate(String chatter, UUID chatterID, String text, int channel);
}