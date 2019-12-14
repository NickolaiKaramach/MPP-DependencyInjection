using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DependencyInjector.Config
{
    public class DependencyConfig : IDependencyConfig
    {
        private readonly ConcurrentDictionary<Type, List<Implementation>> _implsByDependencyType =
            new ConcurrentDictionary<Type, List<Implementation>>();

        public IEnumerable<Implementation> GetImplementations(Type type)
        {
            _implsByDependencyType.TryGetValue(type, out List<Implementation> result);
            return result;
        }

        public void Register<TDependency, TImplementation>(bool isSingleton = false, string name = null)
            where TDependency : class
            where TImplementation : TDependency
        {
            Register(typeof(TDependency), typeof(TImplementation), isSingleton, name);
        }

        public void Register(Type dependency, Type implementation, bool isSingleton = false, string name = null)
        {
            ValidateRegistration(dependency, implementation, isSingleton, name);

            if (!_implsByDependencyType.TryGetValue(dependency, out var impls))
            {
                impls = new List<Implementation>();
                _implsByDependencyType[dependency] = impls;
            }

            if (name != null)
                impls.RemoveAll(impl => impl.Name == name);

            impls.Add(new Implementation(implementation, isSingleton, name, null));
        }

        public void ValidateRegistration(Type dependency, Type implementation, bool isSingleton = false,
            string name = null)
        {
            if (!dependency.IsAssignableFrom(implementation)
                && !(dependency.IsGenericTypeDefinition && implementation.IsGenericTypeDefinition
                                                        && IsAssignableFromAsOpenGeneric(dependency, implementation))
            )
            {
                throw new ArgumentException("Invalid dependency registration types");
            }

            if (!dependency.IsClass && !dependency.IsInterface
                || implementation.IsAbstract)
                throw new ArgumentException("Invalid dependency registration types");
        }

        public bool IsAssignableFromAsOpenGeneric(Type type, Type c)
        {
            if (!type.IsGenericTypeDefinition || !c.IsGenericTypeDefinition)
            {
                throw new ArgumentException("Specified types should be generic");
            }

            var baseTypes = new Queue<Type>();
            baseTypes.Enqueue(c);

            bool result;

            do
            {
                var comparedType = baseTypes.Dequeue();
                var baseType = comparedType.BaseType;
                if ((baseType != null) && (baseType.IsGenericType || baseType.IsGenericTypeDefinition))
                {
                    baseTypes.Enqueue(baseType.GetGenericTypeDefinition());
                }

                foreach (var baseInterface in comparedType.GetInterfaces()
                    .Where((intf) => intf.IsGenericType || intf.IsGenericTypeDefinition))
                {
                    baseTypes.Enqueue(baseInterface.GetGenericTypeDefinition());
                }

                result = comparedType == type;
            } while (!result && (baseTypes.Count > 0));

            return result;
        }
    }
}