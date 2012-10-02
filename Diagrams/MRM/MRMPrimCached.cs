using System;
using System.Drawing;
using OpenMetaverse;
using OpenSim.Region.OptionalModules.Scripting.Minimodule;
using common.framework.interfaces.entities;
using log4net;
using MRM;
using common.framework.interfaces.basic;
using System.Collections.Generic;
using System.Diagnostics;
using MRMPrimType=OpenSim.Region.OptionalModules.Scripting.Minimodule.Object.PrimType;

namespace Diagrams {
    /// <summary>
    /// Implementation of the IWorldPrim interface to work with MRMs. Wraps an IObject from the MRM API in order to work with the world. Registers and removes itself using an IWorldFactory<IObject>.
    /// </summary>
    public class MRMPrimCached : IPrim {
        private static ILog _logger = LogManager.GetLogger(typeof(MRMPrimCached));
        /// <summary>
        /// The wrapped IObject which represents the primitive in the virtual world. Values are taken from this and used to update the fields in the primitive.
        /// </summary>
        private IObject _obj;

        private UUID _id;
        private String _creator = "Default Creator";
        private String _name = "Unnamed Object";
        private String _description = "";
        private String _touchText = "";
        /// <summary>
        /// The colour of the node
        /// </summary>
        private Color _colour;
        private Color _defaultColour = Color.White;
        private double _glow;
        private Vector3 _pos;
        private Quaternion _rotation;
        private Vector3 _scale;
        private PrimType _shape;
        private bool _exists;

        private IMRMPrimFactory _factory;

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
        private void TriggerTouched(IObject sender, TouchEventArgs args) {
            _logger.Debug(Name + " triggering world touch from '" + args.Avatar.Name + "'.");
            _OnWorldTouch(sender.GlobalID, args.Avatar.GlobalID, args.Avatar.Name, args.Avatar.WorldPosition, args.TouchPosition);
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

        public MRMPrimCached(UUID id, IMRMPrimFactory primFactory) {
            _exists = true;
            _id = id;
            _obj = primFactory.RegisterPrim(this, id);
            _exists = _obj.Exists;
            _factory = primFactory;

            Editable = true;

            //Set up the primitive with the given values
            UpdateEditable();
        }

        /// <param name="primFactory">The factory that will be used to interact with the world.</param>
        public MRMPrimCached(IMRMPrimFactory primFactory, string name, Vector3 pos = default(Vector3), Color colour = default(Color), Vector3 scale = default(Vector3), PrimType shape = PrimType.Unknown, Quaternion rotation = default(Quaternion)) {
            _exists = true;
            _factory = primFactory;
            _name = name;
            _colour = colour.Equals(default(Color)) ? Color.White : colour;
            _defaultColour = colour.Equals(default(Color)) ? Color.White : colour;
            _pos = pos.Equals(default(Vector3)) ? new Vector3(125f, 125f, 25f) : pos;
            _scale = scale.Equals(default(Vector3)) ? new Vector3(.5f, .5f, .5f) : scale;
            _shape = shape == PrimType.Unknown ? PrimType.Box : shape;
            _rotation = rotation == default(Quaternion) ? Quaternion.Identity : rotation;

            _description = "";
            _touchText = "";
            _glow = 0d;

            //Create the primitive in world
            _obj = primFactory.RegisterPrim(this, Update);
            _id = _obj.GlobalID;

            Editable = true;

            //Set up the primitive with the given values
            if (!InWorld)
                throw new Exception("Primitive was not created correctly in world.");
            UpdateNotEditable();
        }

        private void Update() {
            if (Editable)
                UpdateEditable();
            else
                UpdateNotEditable();
        }

        private void UpdateObject(IObject obj) {
            _obj = obj;
            Update();
        }

        private bool CheckInWorld() {
            if (!InWorld) {
                _logger.Debug(Name + " triggering world delete.");
                if (_OnWorldDelete != null)
                    _OnWorldDelete(ID);
                _factory.RemovePrim(ID);
                return false;
            }
            return true;
        }

        private void UpdateEditable() {
            if (!CheckInWorld())
                return;
            Vector3 oldPos = _pos;
            _name = _obj.Name;
            try {
                _description = _obj.Description;
                _touchText = _obj.TouchText;
                _colour = _obj.Materials[0].Color;
                _glow = _obj.Materials[0].Bloom;
                _pos = _obj.WorldPosition;
                _rotation = _obj.WorldRotation;
                _scale = _obj.Scale;
                switch (_obj.Shape.PrimType) {
                    case MRMPrimType.Box: _shape = PrimType.Box; break;
                    case MRMPrimType.Cylinder: _shape = PrimType.Cylinder; break;
                    case MRMPrimType.Prism: _shape = PrimType.Prism; break;
                    case MRMPrimType.Ring: _shape = PrimType.Ring; break;
                    case MRMPrimType.Sculpt: _shape = PrimType.Sculpt; break;
                    case MRMPrimType.Sphere: _shape = PrimType.Sphere; break;
                    case MRMPrimType.Torus: _shape = PrimType.Torus; break;
                    case MRMPrimType.Tube: _shape = PrimType.Tube; break;
                }
                if (!_pos.Equals(oldPos) && _OnWorldMoved != null)
                    _OnWorldMoved(ID, oldPos);
            } catch (Exception e) {
                if (!CheckInWorld())
                    return;
            }
        }

        private void UpdateNotEditable() {
            if (_obj == null)
                return;
            if (!_obj.Exists) {
                _obj = _factory.RenewObject(ID);
                if (_OnWorldTouch != null)
                    _obj.OnTouch += TriggerTouched;
            }

            try {
                string name = _obj.Name;
                string description = _obj.Description;
                string touchText = _obj.TouchText;
                Color colour = _obj.Materials[0].Color;
                double glow = _obj.Materials[0].Bloom;
                Vector3 pos = _obj.WorldPosition;
                Quaternion rotation = _obj.WorldRotation;
                Vector3 scale = _obj.Scale;
                PrimType shape = PrimType.Unknown;

                switch (_obj.Shape.PrimType) {
                    case MRMPrimType.Box: shape = PrimType.Box; break;
                    case MRMPrimType.Cylinder: shape = PrimType.Cylinder; break;
                    case MRMPrimType.Prism: shape = PrimType.Prism; break;
                    case MRMPrimType.Ring: shape = PrimType.Ring; break;
                    case MRMPrimType.Sculpt: shape = PrimType.Sculpt; break;
                    case MRMPrimType.Sphere: shape = PrimType.Sphere; break;
                    case MRMPrimType.Torus: shape = PrimType.Torus; break;
                    case MRMPrimType.Tube: shape = PrimType.Tube; break;
                }

                if (!name.Equals(_name)) 
                    Name = _name;
                if (!description.Equals(_description)) 
                    Description = _description;
                if (!touchText.Equals(_touchText))
                    TouchText = _touchText;
                if (!colour.ToArgb().Equals(_colour.ToArgb()))
                    Colour = _colour;
                if (!glow.Equals(_glow))
                    Glow = _glow;
                if (!pos.Equals(_pos)) 
                    Pos = _pos;
                if (!rotation.Equals(_rotation))
                    Rotation = _rotation;
                if (!shape.Equals(_shape))
                    Shape = _shape;
                if (!scale.Equals(_scale))
                    Scale = _scale;
            } catch (Exception e) {
                if (!InWorld)
                    UpdateNotEditable();
                else
                    throw new Exception("Unable to update non-editable prim " + Name + ".", e);
            }
        }

        /// <inhertidoc />
        public UUID ID {
            get { return _id; }
        }

        /// <inhertidoc />
        public bool InWorld {
            get { return _exists && _obj != null && _obj.Exists; }
        }

        /// <inhertidoc />
        public string Name {
            get { return _name; }
            set {
                _name = value;
                if (InWorld)
                    _obj.Name = value;
            }
        }

        /// <inhertidoc />
        public string Creator {
            get { return _creator; }
        }

        /// <inhertidoc />
        public Vector3 Pos {
            get { return _pos; }
            set {
                _pos = value;
                if (InWorld) 
                    _obj.WorldPosition = value;
            }
        }

        public Color DefaultColour {
            get { return _defaultColour; }
            set { _defaultColour = value; }
        }

        /// <inhertidoc />
        public Color Colour {
            get { return _colour; }
            set {
                _colour = value;
                if (InWorld) {
                    IObjectMaterial[] mat = _obj.Materials;
                    for (int i = 0; _obj.Exists && i < mat.Length; i++)
                            mat[i].Color = _colour;
                }
            }
        }

        /// <inhertidoc />
        public Vector3 Scale {
            get { return _scale; }
            set {
                _scale = value;
                if (InWorld) 
                    _obj.Scale = value;
            }
        }

        /// <inhertidoc />
        public PrimType Shape {
            get { return _shape; }
            set {
                _shape = value;
                if (InWorld) {
                    switch (value) {
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
            }
        }

        /// <inhertidoc />
        public Quaternion Rotation {
            get { return _rotation; }
            set {                    
                _rotation = value.Equals(default(Quaternion)) ? Quaternion.Identity : value;
                if (InWorld) 
                    _obj.WorldRotation = _rotation;
            }
        }

        /// <inhertidoc />
        public string Description {
            get { return _description; }
            set {
                _description = value;
                if (InWorld)
                    _obj.Description = value;
            }
        }

        /// <inhertidoc />
        public string TouchText {
            get { return _touchText; }
            set {
                _touchText = value;
                if (InWorld)
                    _obj.TouchText = value;
            }
        }

        public double Glow {
            get { return _glow; }
            set {
                _glow = value;
                if (InWorld) {
                    IObjectMaterial[] mat = _obj.Materials;
                    for (int i = 0; _obj.Exists && i < mat.Length; i++)
                            mat[i].Bloom = _glow;
                }
            }
        }

        /// <inhertidoc />
        public bool Editable { get; set; }

        /// <inhertidoc />
        public void Say(string msg) {
            if (_obj.Exists)
                _obj.Say(msg);
            _logger.Debug(Name + ": " + msg);
        }

        /// <inhertidoc />
        public void Say(int channel, string msg) {
            if (_obj.Exists)
                _obj.Say(msg, channel);
            _logger.Debug(Name + ": " + msg);
        }

        /// <inhertidoc />
        public void Reset() {
            Colour = _defaultColour;
            Glow = 0d;
        }

        /// <inhertidoc />
        public bool Destroy() {
            if (_obj.Exists)
                _obj.Scale = Vector3.Zero;
            _exists = false;
            bool destroyed = _factory.RemovePrim(ID);
            _logger.Info(Name + (destroyed ? "" : " unsuccessfully") + " destroyed.");
            Editable = true;
            foreach (EntityMovedDelegate movedDelegate in new List<EntityMovedDelegate>(_movedDelegates))
                OnWorldMoved -= movedDelegate;
            foreach (EntityDeletedDelegate deletedDelegate in new List<EntityDeletedDelegate>(_deletedDelegates))
                OnWorldDelete -= deletedDelegate;
            foreach (EntityTouchedDelegate touchedDelegate in new List<EntityTouchedDelegate>(_touchedDelegates))
                OnWorldTouch -= touchedDelegate;
            return destroyed;
        }

        /// <inheritdoc/>
        public override String ToString() {
            return Name + " (" + ID + ")";
        }

        /// <inheritdoc/>
        public override bool Equals(Object o) {
            if (o == null)
                return false;
            if (o is common.framework.interfaces.entities.IEntity)
                return ((common.framework.interfaces.entities.IEntity)o).ID == ID;
            return false;
        }

        public override int GetHashCode() {
            return ID.GetHashCode();
        }
    }
}
