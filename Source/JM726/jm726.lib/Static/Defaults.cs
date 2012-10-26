/*************************************************************************
Copyright (c) 2012 John McCaffery 

This file is part of JohnLib.

JohnLib is free software: you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

JohnLib is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License
along with JohnLib.  If not, see <http://www.gnu.org/licenses/>.

**************************************************************************/

namespace common.config {
    /// <summary>
    ///   Keys common to more than one layer
    /// </summary>
    public class CommonDefaults {
        /// <summary>
        ///   Wait value controlling how fast the system will run
        /// </summary>
        public int Wait = 20;
    }

    /// <summary>
    ///   Default values specific to the control layer
    /// </summary>
    public class ControlDefaults {
        /// <summary>
        ///   Default for how long to wait between sending each packet when sending multiple packets
        /// </summary>
        public int MultiSendDelay = 3000;

        /// <summary>
        ///   Default for how many packets to sebd when sending multiple packets
        /// </summary>
        public int MultiSendNumber = 20;
    }

    /// <summary>
    ///   Default values specific to the initialisation layer
    /// </summary>
    public class InitDefaults {}

    /// <summary>
    ///   Default values specific to the model layer
    /// </summary>
    public class ModelDefaults {
        /// <summary>
        ///   Multiplier for the wait value, used so that different layers can run at different speeds but overall speed can be controlled by altering the wait value.
        /// </summary>
        public int WaitMultiplier = 100;
    }

    /// <summary>
    ///   Default values specific to the view layer
    /// </summary>
    public class ViewDefaults {
        /// <summary>
        ///   Used to scale _link widths relative to _node sizes
        /// </summary>
        public float Linkscale = 25f;

        /// <summary>
        ///   PlayNextEvent multiplier governing how many steps a packet must take to get down a _link. 
        ///   Is combined with the weight of the _link to make packets move at different speed along differently weighted links
        /// </summary>
        public float MaxLinkWidth = .5f;

        /// <summary>
        ///   PlayNextEvent multiplier governing how many steps a packet must take to get down a _link. 
        ///   Is combined with the weight of the _link to make packets move at different speed along differently weighted links
        /// </summary>
        public float MinLinkWidth = .05f;

        /// <summary>
        ///   Scale to scale the size of all new nodes by
        /// </summary>
        public float Nodescale = .5f;

        /// <summary>
        ///   PlayNextEvent multiplier governing how many steps a packet must take to get down a _link. 
        ///   Is combined with the weight of the _link to make packets move at different speed along differently weighted links
        /// </summary>
        public float StepMultiplier = 25f;

        /// <summary>
        ///   Multiplier for the wait value, used so that different layers can run at different speeds but overall speed can be controlled by altering the wait value.
        /// </summary>
        public float WaitMultiplier = 2f;
    }

    /// <summary>
    ///   Default values associated with layers
    /// </summary>
    public static class Defaults {
        public static CommonDefaults Common = new CommonDefaults();
        public static ControlDefaults Control = new ControlDefaults();
        public static ModelDefaults Model = new ModelDefaults();
        public static ViewDefaults View = new ViewDefaults();
        public static InitDefaults Init = new InitDefaults();
    }
}