using Wafi.Connect.Samples;
using Xunit;

namespace Wafi.Connect.EntityFrameworkCore.Domains;

[Collection(ConnectTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<ConnectEntityFrameworkCoreTestModule>
{

}
