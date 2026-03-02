using System;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Services;

namespace Wafi.Connect.Chat;

public class ChatDomainService : DomainService
{
    private readonly IConversationRepository _conversationRepository;

    public ChatDomainService(IConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository;
    }

    public async Task ValidateSenderMembershipAsync(
        Guid conversationId, 
        Guid senderUserId, 
        CancellationToken cancellationToken = default)
    {
        var conversation = await _conversationRepository.GetAsync(conversationId, cancellationToken: cancellationToken);
        
        if (!conversation.IsParticipant(senderUserId))
        {
            throw new UnauthorizedAccessException("User is not a participant of this conversation.");
        }
    }

    public static void OrderUserIds(Guid userA, Guid userB, out Guid user1Id, out Guid user2Id)
    {
        if (userA.CompareTo(userB) <= 0)
        {
            user1Id = userA;
            user2Id = userB;
        }
        else
        {
            user1Id = userB;
            user2Id = userA;
        }
    }
}
