using System;

namespace Wafi.Connect.Chat;

public class GetMessagesDto
{
    public Guid ConversationId { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 20;
}
