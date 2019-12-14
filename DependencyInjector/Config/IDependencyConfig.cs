using System;
using System.Collections.Generic;

namespace DependencyInjector.Config
{
    public interface IDependencyConfig
    {
        void Register<TDependency, TImplementation>(bool isSingleton = false, string name = null)
            where TDependency : class
            where TImplementation : TDependency;
        
        void Register(Type dependency, Type implementation, bool isSingleton = false, string name = null);

        IEnumerable<Implementation> GetImplementations(Type type);
    }
}