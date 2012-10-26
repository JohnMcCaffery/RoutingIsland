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

#region Namespace imports

using System;
using System.Drawing;
using Diagrams;
using OpenMetaverse;
using common.framework.interfaces.entities;
using Diagrams.Framework.Interfaces.Entities.general;

#endregion

namespace common.framework.interfaces.basic {
    /// <summary>
    ///   Interface defining a primitive within the world, whatever world is being used for display.
    /// 
    ///   Wraps a series of useful functions to make it easy for the system to work with primitives.
    /// 
    ///   Mainly useful because it wraps functions for setting the colour and glow of a primitive. Setting these
    ///   involve iterating through every surface and setting material properties for that surface.
    /// 
    ///   This interface is just an abstraction to hide the details of how primitives in world are manipulated
    /// </summary>
    public interface IPrim : IEntity, IOwned {
        /// <summary>
        ///   The opensim primitive this primitive uses to model itself
        /// </summary>
        /// <summary>
        ///   Whether or not this primitive exists in world
        /// </summary>
        bool InWorld { get; }

        /// <summary>
        /// The ID of the primitative within the region.
        /// </summary>
        uint LocalID { get; }

        /// <summary>
        /// The texture which is applied to all faces of the primitive;
        /// </summary>
        Bitmap Texture { get; set; }

        /// <summary>
        /// Whether this primitive is attached to the user.
        /// </summary>
        bool IsAttachment { get; }

        /// <summary>
        ///   The position of the primitive in the world
        /// </summary>
        Vector3 Pos { get; set; }

        /// <summary>
        ///   The position of the primitive in the world
        /// </summary>
        Vector3 LocalPos { get; set; }
        
        /// <summary>
        ///   The colour of the prim in the world
        /// </summary>
        Color Colour { get; set; }

        /// <summary>
        ///   The scale of the primitive in the world
        /// </summary>
        Vector3 Scale { get; set; }

        /// <summary>
        ///   The shape of the primitive in the world
        /// </summary>
        PrimType Shape { get; set; }

        /// <summary>
        ///   The rotation of the primitive in the world
        /// </summary>
        Quaternion Rotation { get; set; }

        /// <summary>
        ///   The description of this Object in opensim
        /// </summary>
        String Description { get; set; }

        /// <summary>
        ///   The text which is displayed as the touch option
        /// </summary>
        String TouchText { get; set; }

        /// <summary>
        ///   Set whether this Object is glowing or not
        /// </summary>
        double Glow { get; set; }

        /// <summary>
        ///   Whether changes to this Object should be allowed
        /// </summary>
        bool Editable { get; set; }

        /// <summary>
        /// All the children of this object if is part of a linked set.
        /// </summary>
        IPrim[] Children { get; }

        /// <summary>
        /// The avatar who created this primitive.
        /// </summary>
        UUID Creator { get; }

        /// <summary>
        /// Triggered whenever this entity is moved.
        /// </summary>
        event EntityMovedDelegate OnWorldMoved;

        /// <summary>
        /// Triggered whenever this entity is touched.
        /// </summary>
        event EntityTouchedDelegate OnWorldTouch;

        /// <summary>
        /// Triggered whenever this entity is deleted in world.
        /// </summary>
        event EntityDeletedDelegate OnWorldDelete;
        
        /// <summary>
        ///   Put the msg in world said by the Object
        /// </summary>
        /// <param name = "msg"></param>
        void Say(string msg);

        /// <summary>
        ///   Put the msg in world said by the Object
        /// </summary>
        /// <param name = "msg"></param>
        void Say(int channel, string msg);

        /// <summary>
        /// Create a chat box asking the user for input.
        /// </summary>
        /// <param name="avatar"></param>
        /// <param name="message"></param>
        /// <param name="buttons"></param>
        /// <param name="chatChannel"></param>
        void Dialogue(UUID avatar, string message, string[] buttons, int chatChannel);

        /// <summary>
        ///   Remove the entity from the world
        /// </summary>
        bool Destroy();
    }
}