using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Identity;
using Volo.Abp.Users;
using Wafi.Connect.Chat;

namespace Wafi.Connect.Chat;

[Authorize]
public class ConversationAppService : ConnectAppService, IConversationAppService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IIdentityUserRepository _userRepository;

    public ConversationAppService(
        IConversationRepository conversationRepository,
        IIdentityUserRepository userRepository)
    {
        _conversationRepository = conversationRepository;
        _userRepository = userRepository;
    }

    public async Task<ConversationDto> CreateConversationAsync(CreateConversationDto input)
    {
        var currentUserId = CurrentUser.Id!.Value;
        
        if (!Guid.TryParse(input.OtherUserId, out var otherUserId))
        {
            throw new UserFriendlyException("Invalid user ID format.");
        }

        var otherUser = await _userRepository.GetAsync(otherUserId);
        if (otherUser == null)
        {
            throw new UserFriendlyException("User not found.");
        }

        if (currentUserId == otherUserId)
        {
            throw new UserFriendlyException("Cannot create a conversation with yourself.");
        }

        Guid user1Id, user2Id;
        ChatDomainService.OrderUserIds(currentUserId, otherUserId, out user1Id, out user2Id);

        var existingConversation = await _conversationRepository.GetByUserPairAsync(user1Id, user2Id);
        if (existingConversation != null)
        {
            return MapToConversationDto(existingConversation, currentUserId);
        }

        var conversation = new Conversation(Guid.NewGuid(), user1Id, user2Id);
        await _conversationRepository.InsertAsync(conversation);

        return MapToConversationDto(conversation, currentUserId);
    }

    public async Task<List<ConversationDto>> GetMyConversationsAsync()
    {
        var currentUserId = CurrentUser.Id!.Value;
        var conversations = await _conversationRepository.GetUserConversationsAsync(currentUserId);

        return conversations
            .Select(c => MapToConversationDto(c, currentUserId))
            .ToList();
    }

    private ConversationDto MapToConversationDto(Conversation conversation, Guid currentUserId)
    {
        var otherUserId = conversation.User1Id == currentUserId 
            ? conversation.User2Id 
            : conversation.User1Id;

        return new ConversationDto
        {
            Id = conversation.Id,
            OtherUserId = otherUserId,
            CreationTime = conversation.CreationTime,
            IsArchived = conversation.IsArchived
        };
    }
}
