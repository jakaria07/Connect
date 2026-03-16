using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Wafi.Connect.Chat;

namespace Wafi.Connect.Controllers.Chat;

[Route("api/chat/conversations")]
public class ConversationController : ConnectController
{
    private readonly IConversationAppService _conversationAppService;

    public ConversationController(IConversationAppService conversationAppService)
    {
        _conversationAppService = conversationAppService;
    }

    [HttpPost]
    public Task<ConversationDto> CreateAsync([FromBody] CreateConversationDto input)
    {
        return _conversationAppService.CreateConversationAsync(input);
    }

    [HttpGet("my")]
    public Task<List<ConversationDto>> GetMyConversationsAsync()
    {
        return _conversationAppService.GetMyConversationsAsync();
    }
}
