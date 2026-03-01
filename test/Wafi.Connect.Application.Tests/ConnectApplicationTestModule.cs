using Volo.Abp.Modularity;

namespace Wafi.Connect;

[DependsOn(
    typeof(ConnectApplicationModule),
    typeof(ConnectDomainTestModule)
)]
public class ConnectApplicationTestModule : AbpModule
{

}
