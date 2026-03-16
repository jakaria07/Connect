using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Wafi.Connect.Chat;

public interface IMessageAppService : IApplicationService
{
    Task<MessageDto> SendMessageAsync(SendMessageDto input);
    Task<List<MessageDto>> GetMessagesAsync(GetMessagesDto input);
}
