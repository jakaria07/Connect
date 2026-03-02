using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Wafi.Connect.Chat;

namespace Wafi.Connect.SignalR;

public class SignalRChatMessageNotifier : IChatMessageNotifier
{
    private readonly IHubContext<ChatHub> _hubContext;

    public SignalRChatMessageNotifier(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyMessageReceivedAsync(Guid conversationId, MessageDto messageDto, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .Group($"conversation-{conversationId}")
            .SendAsync("MessageReceived", new { conversationId, message = messageDto }, cancellationToken);
    }
}
