using Wafi.Connect.Localization;
using Volo.Abp.Application.Services;

namespace Wafi.Connect;

/* Inherit your application services from this class.
 */
public abstract class ConnectAppService : ApplicationService
{
    protected ConnectAppService()
    {
        LocalizationResource = typeof(ConnectResource);
    }
}
