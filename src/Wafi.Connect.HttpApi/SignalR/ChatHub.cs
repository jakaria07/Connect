using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Wafi.Connect.Chat;

namespace Wafi.Connect.SignalR;

[Authorize]
public class ChatHub : Hub
{
    private readonly IConversationRepository _conversationRepository;

    public ChatHub(IConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository;
    }

    public async Task JoinConversation(Guid conversationId)
    {
        var currentUserId = Context.UserIdentifier != null
            ? Guid.Parse(Context.UserIdentifier)
            : throw new UnauthorizedAccessException("Unauthenticated.");

        var conversation = await _conversationRepository.GetAsync(conversationId);
        if (!conversation.IsParticipant(currentUserId))
        {
            throw new HubException("You are not a member of this conversation.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation-{conversationId}");
    }

    public Task LeaveConversation(Guid conversationId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation-{conversationId}");
    }
}
