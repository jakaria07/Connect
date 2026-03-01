using Volo.Abp.Modularity;

namespace Wafi.Connect;

/* Inherit from this class for your domain layer tests. */
public abstract class ConnectDomainTestBase<TStartupModule> : ConnectTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
