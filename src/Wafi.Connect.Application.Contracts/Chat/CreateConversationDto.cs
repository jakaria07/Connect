using System;
using System.ComponentModel.DataAnnotations;

namespace Wafi.Connect.Chat;

public class CreateConversationDto
{
    [Required]
    public Guid OtherUserId { get; set; }
}
