using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Wafi.Connect.Chat;

namespace Wafi.Connect.Controllers.Chat;

[Route("api/chat/messages")]
public class MessageController : ConnectController
{
    private readonly IMessageAppService _messageAppService;

    public MessageController(IMessageAppService messageAppService)
    {
        _messageAppService = messageAppService;
    }

    [HttpPost]
    public Task<MessageDto> SendAsync(SendMessageDto input)
    {
        return _messageAppService.SendMessageAsync(input);
    }

    [HttpGet]
    public Task<List<MessageDto>> GetMessagesAsync([FromQuery] GetMessagesDto input)
    {
        return _messageAppService.GetMessagesAsync(input);
    }
}
