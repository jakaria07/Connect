using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;
using Wafi.Connect.Chat;

namespace Wafi.Connect;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class MessageApplicationMappers : MapperBase<Message, MessageDto>
{
    public override partial MessageDto Map(Message source);
    public override partial void Map(Message source, MessageDto destination);
}
