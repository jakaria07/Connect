using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Identity;
using Wafi.Connect.Users;

namespace Wafi.Connect.Users;

[Authorize]
public class UserLookupAppService : ConnectAppService, IUserLookupAppService
{
    private readonly IIdentityUserRepository _userRepository;

    public UserLookupAppService(IIdentityUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<List<UserLookupDto>> GetListAsync()
    {
        var currentUserId = CurrentUser.Id;

        var users = await _userRepository.GetListAsync();

        return users
            .OrderBy(u => u.UserName)
            .Select(u => new UserLookupDto
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                DisplayName = string.IsNullOrWhiteSpace(u.Name)
                    ? (u.UserName ?? string.Empty)
                    : $"{u.Name} {u.Surname}".Trim(),
            })
            .ToList();
    }
}
