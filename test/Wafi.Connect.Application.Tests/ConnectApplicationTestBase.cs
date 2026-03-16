using Volo.Abp.Modularity;

namespace Wafi.Connect;

public abstract class ConnectApplicationTestBase<TStartupModule> : ConnectTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
