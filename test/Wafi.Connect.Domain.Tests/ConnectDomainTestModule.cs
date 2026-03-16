using Volo.Abp.Modularity;

namespace Wafi.Connect;

[DependsOn(
    typeof(ConnectDomainModule),
    typeof(ConnectTestBaseModule)
)]
public class ConnectDomainTestModule : AbpModule
{

}
