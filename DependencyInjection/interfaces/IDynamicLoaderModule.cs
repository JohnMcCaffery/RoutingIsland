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

#region Namespace imports

using System;
using Ninject.Modules;
using Ninject.Parameters;

#endregion

namespace dependencyinjection.interfaces {
    public interface IDynamicLoaderModule : INinjectModule {
        /// <summary>
        ///   Bind an implementation of an interface from an assembly specified dynamically.
        /// </summary>
        /// <param name = "serviceType">The interface which is to be bound to</param>
        /// <param name = "assembly">The assembly where the implementation of serviceType is</param>
        /// <param name = "providerTypeName">The name of the class which will implement serviceType</param>
        /// <param name = "singleton">[Optional] Whether implementations are to use the singleton pattern. If not set will be the default (transient)</param>
        /// <param name = "arg">[Optional] An argument which will be passed in to the constructor</param>
        void BindAllInAssembly(Type serviceType, string assembly, bool singleton = false, params IParameter[] args);

        void BindAllInFolder(Type serviceType, string folder, bool singleton = false, params IParameter[] args);

        void BindDynamic(Type serviceType, string assembly, string providerTypeName, bool singleton = false,
                         params IParameter[] args);

        void BindDynamic(Type serviceType, Type providerType, bool singleton = false, params IParameter[] args);
    }
}