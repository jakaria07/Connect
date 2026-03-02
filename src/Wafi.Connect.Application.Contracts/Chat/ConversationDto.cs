using System;

namespace Wafi.Connect.Chat;

public class ConversationDto
{
    public Guid Id { get; set; }
    public Guid OtherUserId { get; set; }
    public DateTime CreationTime { get; set; }
    public bool IsArchived { get; set; }
}
