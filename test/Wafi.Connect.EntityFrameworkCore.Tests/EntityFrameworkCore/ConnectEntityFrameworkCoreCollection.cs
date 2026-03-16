using Xunit;

namespace Wafi.Connect.EntityFrameworkCore;

[CollectionDefinition(ConnectTestConsts.CollectionDefinitionName)]
public class ConnectEntityFrameworkCoreCollection : ICollectionFixture<ConnectEntityFrameworkCoreFixture>
{

}
