using System;

namespace DependencyInjector.Config
{
    public class Implementation
    {
        public Type Type { get; private set; }

        public bool IsSingleton { get; private set; }

        public object SingletonInstance { get; set; }

        public string Name { get; private set; }

        public Implementation(Type implementationType, bool isSingleton, string name, object singletonInstance)
        {
            Type = implementationType;
            IsSingleton = isSingleton;
            Name = name;
            SingletonInstance = singletonInstance;
        }

        internal object ResolveOrReturnSingletonInstance(DependencyInjection di, string name)
        {
            object instance;
            if (IsSingleton)
            {
                if (SingletonInstance == null)
                {
                    instance = di.Resolve(Type, name);
                    SingletonInstance = instance;
                }
                else
                    instance = SingletonInstance;
            }
            else
                instance = di.Resolve(Type, name);

            return instance;
        }
    }
}