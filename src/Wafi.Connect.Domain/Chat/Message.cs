using System;
using Volo.Abp.Domain.Entities;

namespace Wafi.Connect.Chat;

public class Message : Entity<Guid>
{
    public Guid ConversationId { get; private set; }
    public Guid SenderUserId { get; private set; }
    public string Text { get; private set; }
    public DateTime CreationTime { get; private set; }
    
    public Conversation Conversation { get; private set; }

    private Message()
    {
        /* Required by EF Core */
        Text = string.Empty;
    }

    public Message(Guid id, Guid conversationId, Guid senderUserId, string text)
    {
        Id = id;
        ConversationId = conversationId;
        SenderUserId = senderUserId;
        Text = text ?? throw new ArgumentNullException(nameof(text));
        CreationTime = DateTime.UtcNow;
    }
}
