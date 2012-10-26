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

using Ninject;
using Ninject.Modules;
using Ninject.Parameters;
using dependencyinjection.impl;
using dependencyinjection.interfaces;

#endregion

namespace dependencyinjection.factory {
    public static class NinjectFactory {
        private static IKernel kernel;

        public static void Reset() {
            kernel = null;
        }

        public static IKernel getKernel() {
            return getKernel<DynamicLoaderModule>();
        }

        /// <summary>
        ///   Gets and if necessary initialises a kernel with a loader bound into it
        /// 
        ///   Initialising involves:
        ///   Creating the kernel
        ///   Binding Loader to DynamicLoaderModule so that any system can dynamically bind classes into the kernel
        ///   Loading a singleton instance of Loader into the kernel
        /// </summary>
        /// <typeparam name = "Loader">The loader to use to configure the kernel</typeparam>
        /// <param name = "loaderParams">Any parameters necessary when instantiating a Loader</param>
        /// <returns>The singleton kernel</returns>
        public static IKernel getKernel<Loader>(params IParameter[] loaderParams) where Loader : IDynamicLoaderModule {
            if (kernel == null) {
                kernel = (new StandardKernel()).Get<IKernel>();
                load<Loader>(loaderParams);
            }
            return kernel;
        }

        public static void load<Loader>(params IParameter[] loaderParams) where Loader : IDynamicLoaderModule {
            kernel.Rebind<IDynamicLoaderModule>().To(typeof (Loader)).InSingletonScope();

            bool alreadyLoaded = false;
            foreach (INinjectModule mod in kernel.GetModules())
                if (mod.GetType().Equals(typeof (Loader))) {
                    alreadyLoaded = true;
                    break;
                }
            if (!alreadyLoaded)
                kernel.Load(kernel.Get<IDynamicLoaderModule>(loaderParams));
        }
    }
}