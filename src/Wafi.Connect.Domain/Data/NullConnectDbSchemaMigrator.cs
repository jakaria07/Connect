using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Wafi.Connect.Data;

/* This is used if database provider does't define
 * IConnectDbSchemaMigrator implementation.
 */
public class NullConnectDbSchemaMigrator : IConnectDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
