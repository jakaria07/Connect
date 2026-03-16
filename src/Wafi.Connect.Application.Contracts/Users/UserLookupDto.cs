using System;

namespace Wafi.Connect.Users;

public class UserLookupDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
