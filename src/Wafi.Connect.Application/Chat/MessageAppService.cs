using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Wafi.Connect.Chat;

namespace Wafi.Connect.Chat;

[Authorize]
public class MessageAppService : ConnectAppService, IMessageAppService
{
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly ChatDomainService _chatDomainService;
    private readonly IChatMessageNotifier _chatMessageNotifier;

    public MessageAppService(
        IMessageRepository messageRepository,
        IConversationRepository conversationRepository,
        ChatDomainService chatDomainService,
        IChatMessageNotifier chatMessageNotifier)
    {
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
        _chatDomainService = chatDomainService;
        _chatMessageNotifier = chatMessageNotifier;
    }

    public async Task<MessageDto> SendMessageAsync(SendMessageDto input)
    {
        var currentUserId = CurrentUser.Id!.Value;
        var conversationId = input.ConversationId;

        await _chatDomainService.ValidateSenderMembershipAsync(conversationId, currentUserId);

        var message = new Message(
            Guid.NewGuid(),
            conversationId,
            currentUserId,
            input.Text
        );

        await _messageRepository.InsertAsync(message);

        var messageDto = new MessageDto
        {
            Id = message.Id,
            SenderUserId = message.SenderUserId,
            Text = message.Text,
            CreationTime = message.CreationTime
        };

        await _chatMessageNotifier.NotifyMessageReceivedAsync(conversationId, messageDto);

        return messageDto;
    }

    public async Task<List<MessageDto>> GetMessagesAsync(GetMessagesDto input)
    {
        var currentUserId = CurrentUser.Id!.Value;

        await _chatDomainService.ValidateSenderMembershipAsync(input.ConversationId, currentUserId);

        var messages = await _messageRepository.GetByConversationAsync(
            input.ConversationId,
            input.Skip,
            input.Take
        );

        return messages
            .Select(m => new MessageDto
            {
                Id = m.Id,
                SenderUserId = m.SenderUserId,
                Text = m.Text,
                CreationTime = m.CreationTime
            })
            .ToList();
    }
}
