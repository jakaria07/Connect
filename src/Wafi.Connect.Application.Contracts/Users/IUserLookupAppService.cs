using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Wafi.Connect.Users;

public interface IUserLookupAppService : IApplicationService
{
    Task<List<UserLookupDto>> GetListAsync();
}
