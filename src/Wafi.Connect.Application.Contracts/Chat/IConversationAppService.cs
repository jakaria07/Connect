using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Wafi.Connect.Chat;

public interface IConversationAppService : IApplicationService
{
    Task<ConversationDto> CreateConversationAsync(CreateConversationDto input);
    Task<List<ConversationDto>> GetMyConversationsAsync();
}
