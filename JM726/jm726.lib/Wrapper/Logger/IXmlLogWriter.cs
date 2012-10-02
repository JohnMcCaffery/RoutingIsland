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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using jm726.lib.wrapper;
using System.Xml;

namespace jm726.lib.wrapper.logger {
    public interface IXmlLogWriter {
        /// <summary>
        /// The log that has been created.
        /// </summary>
        XmlDocument Log {
            get;
        }
        /// <summary>
        /// Pause the recording of events. Recording can then be started up again without losing all previously recorded events. The time the recording was paused will not be included in the timestamps.
        /// </summary>
        void PauseRecording();

        /// <summary>
        /// Restart recording after it has previously been paused. New events are appended to the list of events that had been accumulated prior to the logger being paused. If the logger was not paused nothing happens. If the logger had not started recording it starts recording.
        /// </summary>
        void RestartRecording();

        /// <summary>
        /// Start a new recording with a new xml document to record to.
        /// </summary>
        void StartRecording();

        /// <summary>
        /// Start recording and log the recorded events to the provided xml document. Allows two separate loggers to log to the same file.
        /// </summary>
        /// <param name="doc">The document to log to.</param>
        void StartRecording(XmlDocument doc);

        /// <summary>
        /// Stop recording.
        /// </summary>
        void StopRecording();
    }
    public interface IXmlLogWriter<out TToLog> : IXmlLogWriter, IWrapper<TToLog> where TToLog : class {
    }
}
