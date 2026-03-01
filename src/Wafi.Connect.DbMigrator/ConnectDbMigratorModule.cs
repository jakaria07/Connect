using Wafi.Connect.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace Wafi.Connect.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(ConnectEntityFrameworkCoreModule),
    typeof(ConnectApplicationContractsModule)
)]
public class ConnectDbMigratorModule : AbpModule
{
}
