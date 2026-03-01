using Microsoft.Extensions.Localization;
using Wafi.Connect.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace Wafi.Connect;

[Dependency(ReplaceServices = true)]
public class ConnectBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<ConnectResource> _localizer;

    public ConnectBrandingProvider(IStringLocalizer<ConnectResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
