using Wafi.Connect.Samples;
using Xunit;

namespace Wafi.Connect.EntityFrameworkCore.Applications;

[Collection(ConnectTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<ConnectEntityFrameworkCoreTestModule>
{

}
