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
using OpenMetaverse;
using common.framework.interfaces.entities;

namespace Diagrams.Control.impl.Util {
    public interface ISequenceManager : IModule {
        bool PlayingSequence {
            get;
        }

        /// <summary>
        /// Play back a previously recorded sequence of events.
        /// </summary>
        void PlayRecording(string name, UUID id, string file);

        void PlayNextEvent();

        /// <summary>
        /// Write the sequence of user triggered events to an XML events.
        /// </summary>
        /// <param name="topology">The method of the topology events to load in as the start point for the sequence.</param>
        void SaveRecording(string name, string file, string topology = null);

        /// <summary>
        /// Start recording all user triggered events.
        /// </summary>
        void StartRecording();

        void StopPlayback();

        /// <summary>
        /// Finish recording all user triggered events.
        /// </summary>
        void StopRecording();

        T Make<T>(T instance, bool recursive) where T : class;

        void UnMap(OpenMetaverse.UUID id, string name);

        void Stop();

        /// <summary>
        /// Map a string to an ID so it can be looked up when serializing and deserializing.
        /// </summary>
        T MakeMapped<T>(T instance) where T : class, IEntity;
        
        /// <summary>
        /// The folder where topologies for a given user are stored.
        /// </summary>
        /// <param name="name">The user to get the folder for.</param>
        /// <returns></returns>
        string GetUserFolder(string name);
        /// <summary>
        /// The folder where shared topologies are stored.
        /// </summary>
        string SharedFolder { get; }
    }
}
