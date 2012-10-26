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
using OpenSim.Region.OptionalModules.Scripting.Minimodule;
using OpenMetaverse;
using Diagrams;
using common.interfaces.entities;
using common.framework.interfaces.basic;
namespace MRM {
    public interface IMRMPrimFactory : IPrimFactory {
        IObject RegisterPrim(IPrim prim, UUID id);
        IObject RegisterPrim(IPrim prim, string name, Vector3 pos);
        bool RemovePrim(OpenMetaverse.UUID id);
        IObject RenewObject(OpenMetaverse.UUID id, Vector3 pos);
    }
}
