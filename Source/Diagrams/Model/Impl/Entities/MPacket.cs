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
using System.Drawing;
using OpenMetaverse;
using common.framework.abs.full;
using common.framework.impl.util;
using common.framework.interfaces.entities;
using common.model.framework.interfaces;

namespace Diagrams {
    public class MPacket : LogicEntity, IMPacket {
        public MPacket(INode source, INode destination, IMNodeInternal hop, Parameters parameters, string algorithm = null, bool visualise = true)
            : base("Packet from " + source.Name + " to " + destination.Name, source.Colour, 0d, parameters) {

            S = source.ID;
            D = destination.ID;
            Source = source;
            Destination = destination;

            Hop = hop;
            Type = algorithm == null ? PTypes.data : PTypes.Algorithm;
            Visualise = Type == PTypes.data || visualise;
            Algorithm = algorithm;
        }

        public UUID S { get; set; }

        public UUID D { get; set; }

        public IMNodeInternal Hop { get; set; }

        public PTypes Type { get; set; }

        public INode Source { get; set; }

        public INode Destination { get; set; }

        public bool Visualise { get; set; }

        public string Algorithm { get; set; }

        public override event EntityDeletedDelegate OnWorldDelete;

        public override event EntityTouchedDelegate OnWorldTouch;

        public override void ToggleSelected(double degree) {
            IsSelected = !IsSelected;
        }

        public override bool Destroy() {
            return true;
        }

        public override void Reset() {
            IsSelected = false;
            Colour = DefaultColour;
        }

        public override void Say(string msg) {
            throw new NotImplementedException();
        }

        public override void Say(int channel, string msg) {
            throw new NotImplementedException();
        }

        public override event EntityMovedDelegate OnWorldMove;
    }
}