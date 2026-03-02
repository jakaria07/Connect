using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Wafi.Connect.Users;

namespace Wafi.Connect.Controllers.Users;

[Route("api/users")]
public class UserLookupController : ConnectController
{
    private readonly IUserLookupAppService _userLookupAppService;

    public UserLookupController(IUserLookupAppService userLookupAppService)
    {
        _userLookupAppService = userLookupAppService;
    }

    [HttpGet]
    public Task<List<UserLookupDto>> GetListAsync()
    {
        return _userLookupAppService.GetListAsync();
    }
}
