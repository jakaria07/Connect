using System.Threading.Tasks;

namespace Wafi.Connect.Data;

public interface IConnectDbSchemaMigrator
{
    Task MigrateAsync();
}
