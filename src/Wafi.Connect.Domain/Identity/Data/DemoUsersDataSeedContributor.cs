using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.Uow;

namespace Wafi.Connect.Identity.Data;

public class DemoUsersDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IdentityUserManager _userManager;
    private readonly IIdentityUserRepository _userRepository;

    public DemoUsersDataSeedContributor(
        IdentityUserManager userManager,
        IIdentityUserRepository userRepository)
    {
        _userManager = userManager;
        _userRepository = userRepository;
    }

    [UnitOfWork]
    public virtual async Task SeedAsync(DataSeedContext context)
    {
        await EnsureUserAsync("alice", "alice@connect.local", "Demo12345!");
        await EnsureUserAsync("bob", "bob@connect.local", "Demo12345!");
        await EnsureUserAsync("charlie", "charlie@connect.local", "Demo12345!");
    }

    private async Task EnsureUserAsync(string userName, string email, string password)
    {
        var existing = await _userRepository.FindByNormalizedUserNameAsync(userName.ToUpperInvariant());
        if (existing != null)
        {
            return;
        }

        var user = new IdentityUser(Guid.NewGuid(), userName, email);
        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            throw new Exception($"Failed to create demo user '{userName}': {errors}");
        }
    }
}
