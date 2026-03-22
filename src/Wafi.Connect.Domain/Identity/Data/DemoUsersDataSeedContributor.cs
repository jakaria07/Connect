using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.Uow;

namespace Wafi.Connect.Identity.Data;

// IDataSeedContributor → ABP knows to call this class during seeding.
// ITransientDependency → ABP will automatically register this class to the dependency injection system with a transient lifetime.  
public class DemoUsersDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IdentityUserManager _userManager; // IdentityUserManager = high-level service that knows
                                                       // how to create users safely (hashes passwords, validates,
                                                       // raises events…)
    private readonly IIdentityUserRepository _userRepository; // IIdentityUserRepository = low-level repository that
                                                              // allows us to query users directly (without any logic)

    public DemoUsersDataSeedContributor(
        IdentityUserManager userManager,
        IIdentityUserRepository userRepository)
    {
        _userManager = userManager;
        _userRepository = userRepository;
    }

    [UnitOfWork]  // Ensures that all operations in this method are executed within a single database transaction.  If creating 3 users fails halfway, nothing is saved
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
