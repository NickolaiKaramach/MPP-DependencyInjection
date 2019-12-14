namespace DependencyInjector
{
    public interface IDependencyInjection
    {
        TDependency Resolve<TDependency>(string name = null)
            where TDependency : class;
    }
}