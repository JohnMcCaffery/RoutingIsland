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
using Diagrams.MRM.Controls.Buttons;
using common.framework.interfaces.basic;

namespace Diagrams.Control.impl.Buttons.ControlButtons {
    public abstract class ControlButton : IButton{
        private IButton _button;

        public string Name {
            get { return _button.Name; }
        }

        protected ControlButton(IButton button) {
            _button = button;

            _button.OnTouched += (entity, args) => {
                try {
                    Touched(entity, args);
                } catch (Exception e) {
                    Console.Error.WriteLine(e.Message + "\n" + e.StackTrace);
                    Exception inner = e.InnerException;
                    while (inner != null) {
                        Console.Error.WriteLine("\nInner Exception - " + inner.Message + "\n" + inner.StackTrace);
                        inner = inner.InnerException;
                    }

                }
            };
        }

        protected IButton Button {
            get {
                return _button;
            }
        }

        protected abstract void Touched(UUID entity, TouchEventArgs args);

        #region IButton Members

        public event EntityTouchedDelegate OnTouched {
            add { _button.OnTouched += value; }
            remove { _button.OnTouched -= value; }
        }

        public IEnumerable<IPrim> Prims {
            get { return _button.Prims; }
        }

        public void Dispose() {
            _button.Dispose();
        }

        #endregion
    }
}
