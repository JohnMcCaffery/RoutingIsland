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
using System.IO;
using System.Linq;
using Ninject;
using Ninject.Activation;
using Ninject.Extensions.Conventions;
using Ninject.Modules;
using Ninject.Parameters;
using Ninject.Planning.Bindings;
using dependencyinjection.interfaces;

#endregion

namespace dependencyinjection.impl {
    /// <summary>
    /// </summary>
    public class DynamicLoaderModule : NinjectModule, IDynamicLoaderModule {
        private readonly String[] toInclude = new String[] {  "*" };
        private String[] toIgnore = new[] { "*Opensim*.dll" };

        #region IDynamicLoaderModule Members

        /// </inheritdoc>
        public void BindDynamic(Type serviceType, String assembly, String providerTypeName, bool singleton = false,
                                params IParameter[] args) {

                        
            if (assembly == null || assembly.Trim().Length == 0)
                throw new Exception("Assembly cannot be null or empty");
            else if (!File.Exists(assembly))
                throw new Exception(assembly + " file for " + serviceType.Name + " does not exist");
            else if (providerTypeName == null || providerTypeName.Trim().Length == 0)
                throw new Exception("Provider cannot be null or empty");

            Kernel.Scan(x => {
                            x.From(assembly);
                            if (singleton)
                                x.InSingletonScope();

                            x.BindWith(new SimpleBindingGenerator(serviceType, providerTypeName));
                        });

            if (Kernel.GetBindings(serviceType).ToList().Count == 0)
                throw new Exception(providerTypeName + " is not a valid type for " + serviceType.FullName);

            setArgs(serviceType, args);
        }

        /// </inheritdoc>
        public void BindDynamic(Type serviceType, Type providerType, bool singleton = false, params IParameter[] args) {
            if (providerType.GetInterface(serviceType.Name) == null)
                throw new Exception("Invalid provider, " + providerType.FullName + " does not implement " +
                                    serviceType.FullName);

            if (singleton)
                Bind(serviceType).To(providerType).InSingletonScope();
            else
                Bind(serviceType).To(providerType);
            setArgs(serviceType, args);
        }

        /// </inheritdoc>
        public void BindAllInFolder(Type serviceType, String folder, bool singleton = false, params IParameter[] args) {
            if (!Directory.Exists(folder))
                throw new Exception(folder + " does not exist so cannot be scanned");
            
            Kernel.Scan(x => {
                            x.FromAssembliesMatching(
                                toInclude.Select(element => Path.Combine(folder, element)), 
                                toIgnore.Select(element => Path.Combine(folder, element)
                            ));
                            if (singleton)
                                x.InSingletonScope();
                            x.BindWith(new RegexBindingGenerator(serviceType.Name));
                        });

            setArgs(serviceType, args);
        }

        /// </inheritdoc>
        public void BindAllInAssembly(Type serviceType, String assembly, bool singleton = false,
                                      params IParameter[] args) {
            if (!File.Exists(assembly))
                throw new Exception(assembly + " does not exist so cannot be scanned");

            Console.WriteLine("Binding " + serviceType.Name + ". Already bound " + Kernel.GetBindings(serviceType).ToList().Count + " times.");
            Kernel.Scan(x => {
                            x.From(assembly);
                            if (singleton)
                                x.InSingletonScope();
                            x.BindWith(new RegexBindingGenerator(serviceType.Name));
                        });

            setArgs(serviceType, args);
        }

        #endregion

        public override void Load() {}

        /// <summary>
        ///   Set the arguments for a specified binding
        /// </summary>
        /// <param name = "serviceType"></param>
        /// <param name = "args"></param>
        private void setArgs(Type serviceType, params IParameter[] args) {
            if (args != null && args.Length != 0)
                foreach (IBinding b in Kernel.GetBindings(serviceType)) {
                    foreach (IParameter arg in args)
                        b.Parameters.Add(arg);
                }
        }

        #region Nested type: SimpleBindingGenerator

        /// <summary>
        ///   Creates binding of any class called typeName to the interface interfaceType.
        /// </summary>
        public class SimpleBindingGenerator : IBindingGenerator {
            /// <summary>
            ///   Create a binding generator to bind class of type typeName to interface interfaceType
            /// </summary>
            /// <param name = "interfaceType">The interface to bind to</param>
            /// <param name = "typeName">The class to bind to interfaceType</param>
            public SimpleBindingGenerator(Type interfaceType, string typeName) {
                TypeName = typeName;
                InterfaceType = interfaceType;
            }

            #region Implementation of IBindingGenerator

            /// <summary>
            ///   Processes the specified type creating kernel bindings.
            ///   If the type is called typeName and implements interfaceType it will be bound to interfaceType
            /// </summary>
            /// <param name = "type">The type to process.</param>
            /// <param name = "scopeCallback">the scope callback.</param>
            /// <param name = "kernel">The kernel to configure.</param>
            public void Process(Type type, Func<IContext, object> scopeCallback, IKernel kernel) {
                if (type.IsInterface || type.IsAbstract) {
                    return;
                }

                if (TypeName.Equals(type.Name) || TypeName.Equals(type.FullName))
                    if (type.GetInterface(InterfaceType.FullName) != null)
                        kernel.Rebind(InterfaceType).To(type).InScope(scopeCallback);
            }

            #endregion

            /// <summary>
            /// </summary>
            public String TypeName { get; set; }

            public Type InterfaceType { get; set; }
        }

        #endregion
    }
}