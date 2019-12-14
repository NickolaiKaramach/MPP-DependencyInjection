using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using DependencyInjector.Config;

namespace DependencyInjector
{
    public class DependencyInjection : IDependencyInjection
    {
        private readonly IDependencyConfig _config;

        private readonly ConcurrentDictionary<int, Stack<Type>> _recursionControlStackByThreadId =
            new ConcurrentDictionary<int, Stack<Type>>();

        public DependencyInjection(IDependencyConfig config)
        {
            _config = config;
        }

        public TDependency Resolve<TDependency>(string name = null) where TDependency : class
        {
            var dependencyType = typeof(TDependency);

            var result = Resolve(dependencyType, name);

            return (TDependency) result;
        }

        internal object Resolve(Type dependency, string name = null)
        {
            Implementation[] impls;
            object result;

            var currThreadId = Thread.CurrentThread.ManagedThreadId;
            if (!_recursionControlStackByThreadId
                .TryGetValue(currThreadId, out var atResolvingTypes))
            {
                atResolvingTypes = new Stack<Type>();

                if (!_recursionControlStackByThreadId.TryAdd(currThreadId, atResolvingTypes))
                {
                    throw new ApplicationException();
                }
            }
            else
            {
                //check recursion dependency
                if (atResolvingTypes.Contains(dependency))
                {
                    //try create it by constructor
                    result = CreateByConstructor(dependency);
                    if (result != null)
                    {
                        return result;
                    }

                    throw new ArgumentException("Dependencies are recursive.");
                }
            }

            atResolvingTypes.Push(dependency);

            if (dependency.IsPrimitive
                || !dependency.IsClass && !dependency.IsInterface)
            {
                result = Activator.CreateInstance(dependency);
            }
            else if (typeof(IEnumerable).IsAssignableFrom(dependency))
            {
                var dependencyType = dependency.GetGenericArguments()[0];
                impls = _config.GetImplementations(dependencyType).ToArray();

                var implInstances =
                    (object[]) Activator.CreateInstance(dependencyType.MakeArrayType(), impls.Length);

                for (var i = 0; i < impls.Length; i++)
                {
                    implInstances[i] = impls[i].ResolveOrReturnSingletonInstance(this, name);
                }

                result = implInstances;
            }
            else
            {
                impls = _config.GetImplementations(dependency)?.ToArray();
                if (impls == null && dependency.IsGenericType) //handle search for open generic types impls as well
                {
                    impls = _config.GetImplementations(dependency.GetGenericTypeDefinition())?.ToArray();
                }

                if (impls != null)
                {
                    var implToUse = impls.First();
                    if (name != null)
                    {
                        implToUse = Array.Find(impls, impl => impl.Name == name);
                    }

                    result = implToUse?.ResolveOrReturnSingletonInstance(this,
                        name); //TODO: resolve dependency on GenericType of impl of open generic dependency
                }
                else
                {
                    result = CreateByConstructor(dependency);
                }
            }

            atResolvingTypes.Pop();
            return result;
        }

        private object CreateByConstructor(Type type)
        {
            object result = null;

            ResolveIfContainsGenericParameter(ref type);

            var constructorInfos = type.GetConstructors();
            foreach (var constructorInfo in constructorInfos)
            {
                var parameterInfos = constructorInfo.GetParameters();
                var parameters = new List<object>();

                try
                {
                    parameters.AddRange(
                        from parameterInfo in parameterInfos
                        let dependencyExplicitName =
                            parameterInfo.GetCustomAttribute<DependencyKeyAttribute>()?.Name
                        select Resolve(parameterInfo.ParameterType, dependencyExplicitName));

                    result = Activator.CreateInstance(type, parameters.ToArray());
                }
                finally
                {
                    parameters.Clear();
                }
            }

            return result;
        }

        private void ResolveIfContainsGenericParameter(ref Type type)
        {
            if (!type.ContainsGenericParameters)
            {
                return;
            }

            var toResolve = type.GetGenericArguments();

            var genericParameters = toResolve.Select(dep =>
                {
                    var impls = _config.GetImplementations(dep.BaseType)?.ToArray();
                    return impls != null ? impls.First().Type : dep.BaseType;
                })
                .ToArray();

            type = type.MakeGenericType(genericParameters);
        }
    }
}