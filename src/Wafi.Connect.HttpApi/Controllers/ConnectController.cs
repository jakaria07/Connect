using Wafi.Connect.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace Wafi.Connect.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class ConnectController : AbpControllerBase
{
    protected ConnectController()
    {
        LocalizationResource = typeof(ConnectResource);
    }
}
