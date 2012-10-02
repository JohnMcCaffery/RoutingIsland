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
using Diagrams.Control.Impl.Module;
using Diagrams.Common.interfaces.keytable;
using common.Queue;
using common.interfaces.entities;
using OpenMetaverse;
using Nini.Config;
using Diagrams.Control.impl.Buttons;
using Diagrams.Control.impl.Buttons.ControlButtons;
using common.framework.interfaces.basic;
using Diagrams.MRM.Controls.Buttons;
using Diagrams.Control.impl.Util;
using Diagrams.MRM.Controls;

namespace Diagrams.Control.Impl.Module {
    public class LockedWallSandbox : SandboxControl {
        private readonly int _lockWait;

        public LockedWallSandbox(IKeyTableFactory tableFactory, IAsynchQueueFactory queueFactory, IPrimFactory primFactory, IModel model, IConfigSource config)
            : base(tableFactory, queueFactory, primFactory, model, config) {

                _lockWait = config.Configs[CONTROL_CONFIG] != null ? config.Configs[CONTROL_CONFIG].GetInt("ControlsLockTimeout", 300000) : 300000;
        }

        protected override IPermissions MakePermissions(IKeyTableFactory tableFactory, IConfigSource config) {
            return new LockedPermissions(Factory, config, HostPrim, this);
        }
    }
}
