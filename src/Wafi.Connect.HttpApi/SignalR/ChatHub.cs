using System;
using System.Linq;
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

    public async Task JoinConversation(string conversationId)
    {
        Console.WriteLine($"ChatHub: JoinConversation called with conversationId={conversationId}");
        Console.WriteLine($"ChatHub: Context.UserIdentifier={Context.UserIdentifier}");
        Console.WriteLine($"ChatHub: Context.User?.Claims={string.Join(", ", Context.User?.Claims.Select(c => $"{c.Type}:{c.Value}") ?? Array.Empty<string>())}");
        
        if (!Guid.TryParse(conversationId, out var convId))
        {
            Console.WriteLine($"ChatHub: Invalid conversation ID format: {conversationId}");
            throw new HubException("Invalid conversation ID format.");
        }

        // Try to get the actual user ID from claims
        Guid currentUserId;
        var subClaim = Context.User?.Claims?.FirstOrDefault(c => c.Type == "sub")?.Value;
        if (!string.IsNullOrEmpty(subClaim) && Guid.TryParse(subClaim, out var parsedUserId))
        {
            currentUserId = parsedUserId;
            Console.WriteLine($"ChatHub: Using actual user ID from 'sub' claim: {currentUserId}");
        }
        else
        {
            Console.WriteLine($"ChatHub: No valid 'sub' claim found, available claims: {string.Join(", ", Context.User?.Claims.Select(c => $"{c.Type}:{c.Value}") ?? Array.Empty<string>())}");
            throw new HubException("User identity not found in authentication context.");
        }
        
        Console.WriteLine($"ChatHub: User {currentUserId} joining conversation {convId}");

        var conversation = await _conversationRepository.GetAsync(convId);
        if (!conversation.IsParticipant(currentUserId))
        {
            Console.WriteLine($"ChatHub: User {currentUserId} is not a participant in conversation {convId}");
            // Log available properties to debug
            Console.WriteLine($"ChatHub: Conversation properties: {string.Join(", ", conversation.GetType().GetProperties().Select(p => p.Name))}");
            throw new HubException("You are not a member of this conversation.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation-{convId}");
        Console.WriteLine($"ChatHub: Successfully added connection {Context.ConnectionId} to group conversation-{convId}");
    }

    public Task LeaveConversation(string conversationId)
    {
        if (!Guid.TryParse(conversationId, out var convId))
        {
            return Task.CompletedTask;
        }

        return Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation-{convId}");
    }
}
