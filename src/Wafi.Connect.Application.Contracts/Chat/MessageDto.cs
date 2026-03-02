using System;

namespace Wafi.Connect.Chat;

public class MessageDto
{
    public Guid Id { get; set; }
    public Guid SenderUserId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreationTime { get; set; }
}
