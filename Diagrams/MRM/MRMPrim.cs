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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using common.framework.interfaces.basic;
using OpenSim.Region.OptionalModules.Scripting.Minimodule;
using MRMPrimType=OpenSim.Region.OptionalModules.Scripting.Minimodule.Object.PrimType;
using MRMTouchEventArgs=OpenSim.Region.OptionalModules.Scripting.Minimodule.TouchEventArgs;
using OpenMetaverse;
using common.interfaces.entities;
using System.Drawing;
using JM726.Lib.Static;

namespace Diagrams.MRM {
    public class MRMPrim : IPrim {
        public IPrim[] Children {
            get {
                return _obj.Children.Select(child => new MRMPrim(child, _factory)).ToArray();
            }
        }

        private readonly MRMPrimFactory _factory;

        private IObject _obj;
        private UUID _id;
        private Vector3 _pos;
        private Vector3 _scale;
        private PrimType _shape;
        private Color _colour;
        private Quaternion _rot;
        private string _name;
        private double _glow;
        private bool _exists;
        private bool _editable;
        private bool _child;

        private Action _updateListener;

        #region WorldMove event

        private List<EntityMovedDelegate> _movedDelegates = new List<EntityMovedDelegate>();
        private event EntityMovedDelegate _OnWorldMoved;
        public event EntityMovedDelegate OnWorldMoved {
            add {
                _movedDelegates.Add(value);
                _OnWorldMoved += value;
            }
            remove {
                _movedDelegates.Remove(value);
                _OnWorldMoved -= value;
            }
        }
        #endregion

        #region WorldTouch event

        private List<EntityTouchedDelegate> _touchedDelegates = new List<EntityTouchedDelegate>();
        private event EntityTouchedDelegate _OnWorldTouch;
        public event EntityTouchedDelegate OnWorldTouch {
            add {
                if (_OnWorldTouch == null)
                    _obj.OnTouch += TriggerTouched;
                _touchedDelegates.Add(value);
                _OnWorldTouch += value;
            }
            remove {
                _OnWorldTouch -= value;
                _touchedDelegates.Remove(value);
                if (_OnWorldTouch == null)
                    _obj.OnTouch -= TriggerTouched;
            }
        }
        private void TriggerTouched(IObject sender, MRMTouchEventArgs args) {
            if (_OnWorldTouch != null) {
                TouchEventArgs e = new TouchEventArgs();
                e.AvatarName = args.Avatar.Name;
                e.AvatarID = args.Avatar.GlobalID;
                e.AvatarPosition = args.Avatar.WorldPosition;
                e.TouchPosition = args.TouchPosition;
                //e.TouchBiNormal = args.TouchBiNormal;
                //e.TouchMaterialIndex = args.TouchMaterialIndex;
                //e.TouchNormal = args.TouchNormal;
                //e.TouchST = args.TouchST;
                //e.TouchUV = args.TouchUV;
                //e.LinkNumber = args.LinkNumber;

                _OnWorldTouch(sender.GlobalID, e);
            }
        }

        #endregion

        #region WorldDelete event

        private List<EntityDeletedDelegate> _deletedDelegates = new List<EntityDeletedDelegate>();
        private event EntityDeletedDelegate _OnWorldDelete;
        public event EntityDeletedDelegate OnWorldDelete {
            add {
                _deletedDelegates.Add(value);
                _OnWorldDelete += value;
            }
            remove {
                _deletedDelegates.Remove(value);
                _OnWorldDelete -= value;
            }
        }

        #endregion

        public MRMPrim(UUID id, MRMPrimFactory factory) {
            _factory = factory;
            _id = id;
            SetUpExistingObj(_factory.RegisterPrim(this, id), factory);
        }

        public MRMPrim(IObject obj, MRMPrimFactory factory) {
            _factory = factory;
            _id = obj.GlobalID;
            _factory.RegisterPrim(this);
            SetUpExistingObj(obj, factory);
        }

        private void SetUpExistingObj(IObject obj, MRMPrimFactory factory) {
            _obj = obj;
            _exists = true;
            _editable = true;
            _name = _obj.Name;
            _pos =  _obj.WorldPosition;
            _child = !ID.Equals(obj.Root.GlobalID);
            bool attachment = _obj.IsAttachment;

            Vector3 shift = new Vector3();
            if (attachment && !_child) {
                //ATTACH_HUD_CENTER_2	    31	 HUD Center 2
                //ATTACH_HUD_TOP_RIGHT	    32	 HUD Top Right
                //ATTACH_HUD_TOP_CENTER	    33	 HUD Top
                //ATTACH_HUD_TOP_LEFT	    34	 HUD Top Left
                //ATTACH_HUD_CENTER_1	    35	 HUD Center
                //ATTACH_HUD_BOTTOM_LEFT	36	 HUD Bottom Left
                //ATTACH_HUD_BOTTOM	        37	 HUD Bottom
                //ATTACH_HUD_BOTTOM_RIGHT	38	 HUD Bottom Right
                switch (factory.World.Avatars.First(avatar => _obj.OwnerId.Equals(avatar.GlobalID)).Attachments.First(attach => attach.Asset.GlobalID.Equals(_obj.GlobalID)).Location) {
                    case 31: shift = new Vector3(0f, .5f, .5f); break;
                    case 32: shift = new Vector3(0f, 0f, 1f); break;
                    case 33: shift = new Vector3(0f, .5f, 1f); break;
                    case 34: shift = new Vector3(0f, 1f, 1f); break;
                    case 35: shift = new Vector3(0f, .5f, .5f); break;
                    case 36: shift = new Vector3(0f, 1f, 0f); break;
                    case 37: shift = new Vector3(0f, .5f, 0f); break;
                    //case 38: shift = new Vector3(0f, 0f, 0f); break;
                }
            }
            _factory.Update += () => {
                if (InWorld) {
                    if (!_editable) {
                        _name = Name;
                        _pos = Pos;
                        _scale = Scale;
                        _shape = Shape;
                        _colour = Colour;
                    }
                    if (attachment && !_child) {
                        Vector3 scale = _obj.Scale;
                        if (scale.Y < 1 && scale.Z < 1) {
                            Vector3 local = _obj.OffsetPosition + shift;
                            Vector3 newLocal = local;

                            float y = scale.Y / 2;
                            float z = scale.Z / 2;

                            //TODO Look into using texture stretch data to figure out screen ratio
                            //if (local.Y + y > .5f)
                            //    newLocal.Y = .5f - y;
                            //else
                            if (local.Y - y < 0f)
                                newLocal.Y = y;

                            if (local.Z + z > 1f)
                                newLocal.Z = 1 - z;
                            else if (local.Z - z < 0f)
                                newLocal.Z = z;

                            //TODO Look into using texture stretch data to figure out screen ratio
                            if (local != newLocal)
                                _obj.WorldPosition = newLocal - shift;
                        }
                    }
                }
            };

            _updateListener = Update;
            factory.Update += _updateListener;
        }

        public MRMPrim(MRMPrimFactory factory, string name, Vector3 position, Color colour = default(Color), Vector3 scale = default(Vector3), PrimType shape = PrimType.Unknown, Quaternion rotation = default(Quaternion)) {
            _exists = true;
            _editable = true;
            _factory = factory;
            _name = name;
            _obj = factory.RegisterPrim(this, name, position);
            _id = _obj.GlobalID;
            _child = !ID.Equals(_obj.Root.GlobalID);

            Glow = 0d;
            Name = _name;
            Colour = !colour.Equals(default(Color)) ? colour : Color.White;
            Shape = !shape.Equals(PrimType.Unknown) ? shape : PrimType.Box;
            Rotation = !rotation.Equals(default(Quaternion)) ? rotation : Quaternion.Identity;
            Scale = !scale.Equals(default(Vector3)) ? scale : new Vector3(.5f, .5f, .5f);

            _updateListener = Update;
            factory.Update += _updateListener;
        }

        /// <summary>
        /// Updates the primitive. Aims to run as fast as possible with as few calls to the proxy as possible.
        /// 
        /// If the primitive is editable but has been deleted 1 call is made to the proxy (to find out whether it has been deleted) and the delete event is triggered.
        /// If the primitive is editable and has not been deleted 2 calls are made to the proxy (to find out whether it has been deleted and to check if it has moved).
        /// If the primitive is not editable but has been deleted 1 calls is made to the proxy to find this out. Then a series more calls are made to create and initialise a new proxy.
        /// If the primitive is not editable and has not been deleted 1 + 5 + 2X + 1 calls are made. 1 to find out the prim has not been deleted. 5 calls to reset Name, Position, Scale, Rotation and Shape. 
        ///     2X + 1 calls to update the colour and glow of every side (where there are x sides)
        /// </summary>
        private void Update() {
            if (_editable && InWorld) 
                _pos = Pos;
            else if (!_editable && !_obj.Exists)
                _exists = InWorld;
            else if (!_editable) {
                try {
                    //If the prim is not editable and still in world its fields need to be checked to make sure they haven't changed.
                    _obj.Name = _name;
                    _obj.WorldPosition = _pos;
                    _obj.Scale = _scale;
                    _obj.WorldRotation = _rot.Equals(default(Quaternion)) ? Quaternion.Identity : _rot;
                    SetInWorldShape(_shape);
                    foreach (IObjectMaterial mat in _obj.Materials) {
                        mat.Color = _colour;
                        mat.Bloom = _glow;
                    }
                } catch (Exception e) {
                    Console.WriteLine("Problem updating " + _name + ". " + (InWorld ? "No longer" : "Still") +  " in world.\n" + e);
                }
            }
        }

        private bool _deleteTrigger = false;
        public bool InWorld {
            get {
                //If the prim is already known not to exist
                if (!_exists || _obj == null)
                    return false;
                //If this is the first time the prim has been registered as not existing in world.
                if (!_obj.Exists) {
                    if (_editable) {
                        //If the prim is world editable then trigger the event and mark it as not existing
                        if (_OnWorldDelete != null && !_deleteTrigger) {
                            _deleteTrigger = true;
                            _OnWorldDelete(ID);
                            _deleteTrigger = true;
                        }
                        _exists = false;
                    } else
                        RenewPrimitive();
                }
                return _exists; 
            }
        }

        /// <summary>
        /// Whether this primitive is attached to the user.
        /// </summary>
        public bool IsAttachment {
            get { return InWorld && _obj.IsAttachment; }
        }

        public UUID ID {
            get { return _id; }
        }

        public UUID Creator {
            get {
                if (!InWorld)
                    return UUID.Zero;
                return _obj.CreatorId;
            }
        }

        public UUID Owner {
            get {
                if (!InWorld)
                    return UUID.Zero;
                return _obj.OwnerId;
            }
        }

        public string Name {
            get {
                if (!_editable) {
                    Name = _name;
                    return _name;
                }
                return _name;
            }
            set {
                _name = value;
                if (InWorld)
                    _obj.Name = value;
            }
        }

        public Vector3 Pos {
            get {
                if (!InWorld)
                    return default(Vector3);
                
                if (!_pos.Equals(_obj.WorldPosition)) {
                    if (_editable) {
                        Vector3 oldPos = _pos;
                        _pos = _obj.WorldPosition;
                        if (_OnWorldMoved != null)
                            _OnWorldMoved(ID, oldPos, _pos);
                    } else
                        _obj.WorldPosition = _pos;
                }
                return _pos;
            }
            set {
                if (_pos.Equals(value))
                    return;
                _pos = value;
                if (InWorld)
                    _obj.WorldPosition = value;
            }
        }

        public Vector3 LocalPos {
            get {
                if (!InWorld)
                    return default(Vector3);
                return _obj.OffsetPosition;
            }
            set {
                if (InWorld) 
                    _obj.OffsetPosition = value;
            }
        }

        public uint LocalID {
            get {
                if (!InWorld)
                    return default(uint);
                return _obj.LocalID;
            }
        }

        public Color Colour {
            get {
                if (!_editable) {
                    Colour = _colour;
                    return _colour;
                }
                return !InWorld ? default(Color) : _obj.Materials[0].Color;
            }
            set {
                if (!_editable)
                    _colour = value;
                if (InWorld)
                    foreach (IObjectMaterial mat in _obj.Materials)
                        mat.Color = value;
            }
        }

        private Bitmap _texture;

        /// <summary>
        /// The texture which is applied to all faces of the primitive;
        /// </summary>
        public Bitmap Texture {
            get {
                if (!_editable) {
                    Colour = _colour;
                    return _texture;
                }
                return !InWorld ? null : _factory.GetTexture(_obj.Materials[0].Texture);
            }
            set {
                if (!_editable)
                    _texture = value;
                if (InWorld) {
                    UUID tex = _factory.MakeTexture(value);
                    foreach (IObjectMaterial mat in _obj.Materials)
                        mat.Texture = tex;
                }
            }
        }

        public Vector3 Scale {
            get {
                if (!_editable) {
                    Scale = _scale;
                    return _scale;
                }
                return !InWorld ? default(Vector3) : _obj.Scale;
            }
            set {
                if (!_editable)
                    _scale = value;
                if (InWorld)
                    _obj.Scale = value;
            }
        }

        public PrimType Shape {
            get {
                if (!_editable) {
                    Shape = _shape;
                    return _shape;
                }
                if (!InWorld)
                    return PrimType.Unknown;
                switch (_obj.Shape.PrimType) {
                    case MRMPrimType.Box: return PrimType.Box; 
                    case MRMPrimType.Cylinder: return PrimType.Cylinder; 
                    case MRMPrimType.Prism: return PrimType.Prism; 
                    case MRMPrimType.Ring: return PrimType.Ring; 
                    case MRMPrimType.Sculpt: return PrimType.Sculpt; 
                    case MRMPrimType.Sphere: return PrimType.Sphere; 
                    case MRMPrimType.Torus: return PrimType.Torus; 
                    case MRMPrimType.Tube: return PrimType.Tube;
                    default: return PrimType.Unknown;
                }
            }
            set {
                if (!_editable)
                    _shape = value;
                if (InWorld)
                    SetInWorldShape(value);
            }
        }

        public Quaternion Rotation {
            get {
                if (!_editable) {
                    Rotation = _rot;
                    return _rot;
                }
                return !InWorld ? default(Quaternion) : _obj.WorldRotation;
            }
            set {
                if (!_editable)
                    _rot = value;
                if (InWorld)
                    _obj.WorldRotation = value.Equals(default(Quaternion)) ? Quaternion.Identity : value;
            }
        }

        public double Glow {
            get {
                if (!_editable) {
                    foreach (IObjectMaterial mat in _obj.Materials)
                        mat.Bloom = _glow;
                        Glow = _glow;
                    return _glow;
                }
                return !InWorld ? 0d : _obj.Materials[0].Bloom;
            }
            set {
                if (!_editable)
                    _glow = value;
                if (InWorld)
                    foreach (IObjectMaterial mat in _obj.Materials)
                        mat.Bloom = value;
            }
        }

        public string Description {
            get {
                return InWorld ? _obj.Description : null;
            }
            set {
                if (InWorld)
                    _obj.Description = value;
            }
        }

        public string TouchText {
            get {
                return InWorld ? _obj.TouchText : null;
            }
            set {
                if (InWorld)
                    _obj.TouchText = value;
            }
        }

        public bool Editable {
            get {
                return _editable;
            }
            set {
                if (!value) {
                    //If making this object uneditable then get 
                    //all the current values from the in world prim
                    _editable = true;
                    _name = Name;
                    _pos = Pos;
                    _scale = Scale;
                    _shape = Shape;
                    _rot = Rotation;
                    _colour = Colour;
                    _glow = Glow;
                }
                _editable = value;
            }
        }

        public void Say(string msg) {
            if (InWorld)
                _obj.Say(msg);
        }

        public void Say(int channel, string msg) {
            if (InWorld)
                _obj.Say(msg, channel);
        }

        public bool Destroy() {
            Scale = Vector3.Zero;
            _exists = false;
            _editable = true;
            _factory.Update -= _updateListener;
            foreach (EntityDeletedDelegate del in _deletedDelegates.ToArray())
                _OnWorldDelete -= del;
            foreach (EntityTouchedDelegate touched in _touchedDelegates.ToArray())
                _OnWorldTouch -= touched;
            return _factory.RemovePrim(_id);
        }

        private void RenewPrimitive() {
            //If the prim is not editable create a new primitive in world and set it up to look like the old primitive
            _obj = _factory.RenewObject(_id, _pos);
            if (_obj == null)
                return;
            Name = _name;
            Shape = _shape;
            Colour = _colour;
            Rotation = _rot;
            Glow = _glow;
            Scale = _scale;
            if (_OnWorldTouch != null)
                _obj.OnTouch += TriggerTouched;
        }

        private void SetInWorldShape(PrimType shape) {
            switch (shape) {
                case PrimType.Box: _obj.Shape.PrimType = MRMPrimType.Box; break;
                case PrimType.Cylinder: _obj.Shape.PrimType = MRMPrimType.Cylinder; break;
                case PrimType.Prism: _obj.Shape.PrimType = MRMPrimType.Prism; break;
                case PrimType.Ring: _obj.Shape.PrimType = MRMPrimType.Ring; break;
                case PrimType.Sculpt: _obj.Shape.PrimType = MRMPrimType.Sculpt; break;
                case PrimType.Sphere: _obj.Shape.PrimType = MRMPrimType.Sphere; break;
                case PrimType.Torus: _obj.Shape.PrimType = MRMPrimType.Torus; break;
                case PrimType.Tube: _obj.Shape.PrimType = MRMPrimType.Tube; break;
            }
        }


        public void Dialogue(UUID avatar, string message, string[] buttons, int chatChannel) {
            if (InWorld)
                _obj.Dialog(avatar, message, buttons, chatChannel);
        }

        internal void RemoveListeners() {
            _factory.Update -= _updateListener;
            foreach (EntityTouchedDelegate touched in _touchedDelegates.ToArray())
                _OnWorldTouch -= touched;
        }
    }
}
