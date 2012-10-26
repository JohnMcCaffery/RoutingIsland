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
using common;
using log4net;
using System.Diagnostics;

namespace Diagrams {
    // TODO IMPLEMENTED
    public class Process {
        public static Process CreateProcess(string name, Action processor) {
            return new Process(name, processor);
        }

        private readonly string _name;
        private readonly Action _task;
        private readonly ILog logger = LogManager.GetLogger(typeof(Process));
        private readonly StackTrace _trace;
        private double _time = -1;

        public Process(string name, Action processor) {
            _name = name;
            _task = processor;
            _created = DateTime.Now;
            //_trace = new StackTrace();
        }

        public void Run() {
            //Console.WriteLine(_name + "\n" + _trace);
            DateTime start = DateTime.Now;
            _task();
            _time = DateTime.Now.Subtract(start).TotalMilliseconds;
        }

        public double ProcessingTime { get { return _time; } }

        public string Name { get { return _name + " : " + _created.ToString("HH:MM:SS"); } }

        private readonly DateTime _created;
    }
}