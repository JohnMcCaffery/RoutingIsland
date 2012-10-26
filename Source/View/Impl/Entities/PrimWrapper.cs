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
using common.framework.abs.full;
using common.framework.interfaces.basic;
using OpenMetaverse;
using common.framework.impl.util;
using System.Drawing;
using common.interfaces.entities;
using log4net;

namespace Diagrams {
    public abstract class PrimWrapper : LogicEntity {
        private readonly ILog _log;
        protected ILog Logger {
            get { return _log; }
        }

        public PrimWrapper(IPrimFactory primFactory, Vector3 position, string name)
            : base(name) {

            _log = LogManager.GetLogger(GetType());
            _prim = InitPrim(primFactory, position);
            _prim.Name = name;
        }
        public PrimWrapper(IPrimFactory primFactory, Vector3 position, string name, Color colour, double selected, Parameters parameters)
            : base(name, colour, selected, parameters) {

            _log = LogManager.GetLogger(GetType());
            _prim = InitPrim(primFactory, position);
            _prim.Name = name;
        }

        protected virtual IPrim InitPrim(IPrimFactory primFactory, Vector3 position) {
            return primFactory.MakePrim(Name, position, DefaultColour);
        }

        private readonly IPrim _prim;
        protected IPrim Prim {
            get { return _prim; }
        }

        //public override string  Name {
        //    get { return _prim.Name; }
        //    set { _prim.Name = value; }
        //}

        private double _selectedDegree = .6d;
        public override double Selected {
            get {
                return base.Selected;
            }
            set {
                _selectedDegree = value;
                base.Selected = value;
                if (_prim != null) 
                    _prim.Glow = _selectedDegree;
            }
        }

        public override Color Colour {
            get {
                return _prim != null ? _prim.Colour : Color.White;
            }
            set {
                if (_prim != null)
                    _prim.Colour = value;
            }
        }

        public override bool IsSelected {
            get {
                return base.Selected == 0d;
            }
            set {
                Selected = value ? _selectedDegree : 0d;
            }
        }

        public override void Reset() {
            _prim.Colour = DefaultColour;  
            IsSelected = false;
        }

        public override void Say(string msg) {
            _prim.Say(msg);
        }

        public override void Say(int channel, string msg) {
            _prim.Say(channel, msg);
        }

        public override void ToggleSelected(double degree) {
            IsSelected = !IsSelected;
        }

        private event EntityDeletedDelegate _OnWorldDelete;
        public override event EntityDeletedDelegate OnWorldDelete {
            add {
                if (_OnWorldDelete == null)
                    _prim.OnWorldDelete += TriggerDelete;
                _OnWorldDelete += value;
            }
            remove {
                _OnWorldDelete += value;
                if (_OnWorldDelete == null)
                    _prim.OnWorldDelete -= TriggerDelete;
            }
        }
        private void TriggerDelete(UUID entity) {
            if (_OnWorldDelete != null)
                _OnWorldDelete(ID);
        }


        private event EntityTouchedDelegate _OnWorldTouch;
        public override event EntityTouchedDelegate OnWorldTouch {
            add {
                if (_OnWorldTouch == null)
                    _prim.OnWorldTouch += TriggerTouch;
                _OnWorldTouch += value;
            }
            remove {
                _OnWorldTouch += value;
                if (_OnWorldTouch == null)
                    _prim.OnWorldTouch -= TriggerTouch;
            }
        }
        private void TriggerTouch(UUID entity, TouchEventArgs args) {
            if (_OnWorldTouch != null) {
                _OnWorldTouch(ID, args);
            }
        }


        private event EntityMovedDelegate _OnWorldMove;
        public override event EntityMovedDelegate OnWorldMove {
            add {
                if (_OnWorldMove == null)
                    _prim.OnWorldMoved += TriggerMove;
                _OnWorldMove += value;
            }
            remove {
                _OnWorldMove += value;
                if (_OnWorldMove == null)
                    _prim.OnWorldMoved -= TriggerMove;
            }
        }
        private void TriggerMove(UUID entity, Vector3 oldPos, Vector3 newPos) {
            if (_OnWorldMove != null) 
                _OnWorldMove(ID, oldPos, newPos);
        }
    }
}
