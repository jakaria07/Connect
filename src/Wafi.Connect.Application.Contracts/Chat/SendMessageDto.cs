using System;
using System.ComponentModel.DataAnnotations;

namespace Wafi.Connect.Chat;

public class SendMessageDto
{
    [Required]
    public Guid ConversationId { get; set; }

    [Required]
    [StringLength(4000)]
    public string Text { get; set; } = string.Empty;
}
