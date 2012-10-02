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
using common.framework.abs.wrapper;
using common.framework.impl.util;
using common.interfaces.entities;
using common.model.framework.interfaces;
using core.view.interfaces;
using OpenMetaverse;
using log4net;
using core.view.impl.entities;
using common.config;
using common.framework.interfaces.basic;
using jm726.lib.wrapper;
using jm726.lib.wrapper.spy;
using System.Linq;
using common;
using Amib.Threading;

namespace Diagrams {
    public class VPacket : PrimWrapper, IVPacket {
        public static int DefaultMovesPerUnit = 30;
        public static int MinMovesPerUnit = 5;
        public static int MaxMovesPerUnit = 55;
        public const float Length = .2f;
        private const float ExtraWidth = .1f;

        private IPacket _packet;
        private IVNode _from, _to;
        private IVLink _link;

        private bool _dropped = false;

        /// <summary>
        ///   The number of steps the packet is to take.
        /// </summary>
        private int _numSteps = 500;
        /// <summary>
        ///   Which step the packet is currently at.
        /// </summary>
        private int _step;
        /// <summary>
        ///   The amount the packet is to be shifted by each step.
        /// </summary>
        private Vector3 _shiftPerStep;

        private readonly View _view;

        private readonly Action _tickListener;

        public VPacket(IPacket packet, IVNode from, IVNode to, IVLink link, IPrimFactory primFactory, IAsynchQueue queue, View view)
            : base(primFactory, from.Pos, packet.Name, packet.Colour, packet.Selected, packet.Parameters) {

            Prim.Editable = false;
            _packet = packet;
            _from = from;
            _to = to;
            _link = link;
            _step = 0;
            _view = view;
            Selected = packet.Selected;
            Configure();

            _deleteListener = id => Dropped("Visualisation layer dropped " + Name + "."); 

            _from.OnWorldMove += (id, oldPos, newPos) => Reconfigure();
            _to.OnWorldMove += (id, oldPos, newPos) => Reconfigure();
            _from.OnAPIMove += (id, oldPos, newPos) => Reconfigure();
            _to.OnAPIMove += (id, oldPos, newPos) => Reconfigure();


            link.OnWeightChanged += (id, weight) => Reconfigure();
            link.OnWorldDelete += _deleteListener;
            link.OnSystemDelete += _deleteListener;

            //_tickListener = () => queue.QueueWorkItem(state => UpdatePosition());
            _tickListener = () => queue.QWork("Move Packet", () => UpdatePosition());
            view.OnTick += _tickListener;
        }

        private readonly EntityDeletedDelegate _deleteListener;

        protected override IPrim InitPrim(IPrimFactory primFactory, Vector3 position) {
            IPrim p = primFactory.MakePrim(Name, position, DefaultColour, new Vector3(0f, 0f, .001f), PrimType.Cylinder);
            p.Editable = false;
            //ISpy<IPrim> pSpy = new Spy<IPrim>(p);
            //pSpy.OnMethodEvent += (source, method, ret, parameters) =>  p.Say("Called " + method.Name);
            //return pSpy.Instance;
            return p;
        }

        private static Vector3 GetScale(IVLink link) {
            return new Vector3(link.Scale.X + ExtraWidth, link.Scale.Y + ExtraWidth, Length);
        }

        #region IVPacket Members

        public bool InWorld {
            get { return Prim.InWorld; }
        }

        private bool _delivered;

        public bool UpdatePosition() {
            lock (this) {
                if (_dropped || _delivered)
                    return false;
                if (!Visualise || ++_step >= _numSteps) {
                    View.PacketLogger.Debug("Delivering '" + Name + "' to '" + _to.Name + "' after visualising transfer.");
                    Destroy();
                    _to.PacketReceived(_packet);
                    View.PacketLogger.Info("Delivered '" + Name + "' to '" + _to.Name + "' after visualising transfer.");
                    _delivered = true;
                    _view.OnTick -= _tickListener;
                    return false;
                } else
                    MovePosition();
                return true;
            }
        }

        /// <inheritdoc />
        public bool Visualise {
            get { return _packet.Visualise; }
            set { _packet.Visualise = value; }
        }

        public void Dropped(string msg) {
            _dropped = true;
            Logger.Debug(msg);
            ///*if (packet.InWorld)
            //    packet.say("Dropped on " + link.Name);
            //else*/
            //if (_link.InWorld)
            //    _link.Say("Dropped " + _packet.Name);
            //else if (_from.InWorld)
            //    _from.Say("Unable to send " + _packet.Name + " along " + _link.Name);
            //else if (_to.InWorld)
            //    _to.Say("Never received " + _packet.Name + " along " + _link.Name);
            //else if (InWorld)
            //    Say(msg);
            Destroy();
        }

        #endregion

        private void Reconfigure() {
            float portionDone = (float) _step / (float) _numSteps;
            Configure();
            _step = (int)(_numSteps * portionDone);
        }

        private void Configure() {
            float distance = _link.Length;
            float movesPerUnit = DefaultMovesPerUnit;

            if (!_link.Weight.Equals(distance)) {
                float movesPerUnitRange = MaxMovesPerUnit - MinMovesPerUnit;
                float weightAdjustedMovesPerUnit = movesPerUnitRange * _link.Weight;
                movesPerUnit = MinMovesPerUnit + weightAdjustedMovesPerUnit;
            }
            
            _numSteps = (int)(distance * movesPerUnit);
            _shiftPerStep = Vector3.Divide(Vector3.Subtract(_to.Pos, _from.Pos), _numSteps);
            Prim.Rotation = VLink.GetRotatation(_from, _to);
            Prim.Scale = GetScale(_link);
        }

        private void MovePosition() {
            Prim.Pos = Vector3.Add(_from.Pos, Vector3.Multiply(_shiftPerStep, _step));
        }

        public override bool Destroy() {
            _link.OnWorldDelete -= _deleteListener;
            _link.OnSystemDelete -= _deleteListener;
            return Prim.Destroy();
        }
    }
}