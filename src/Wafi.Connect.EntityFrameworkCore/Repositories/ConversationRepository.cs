using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Wafi.Connect.Chat;
using Wafi.Connect.EntityFrameworkCore;

namespace Wafi.Connect.Repositories;

public class ConversationRepository : EfCoreRepository<ConnectDbContext, Conversation, Guid>, IConversationRepository
{
    public ConversationRepository(IDbContextProvider<ConnectDbContext> dbContextProvider) 
        : base(dbContextProvider)
    {
    }

    public async Task<Conversation?> GetByUserPairAsync(Guid userA, Guid userB, CancellationToken cancellationToken = default)
    {
        var (user1Id, user2Id) = userA.CompareTo(userB) <= 0 
            ? (userA, userB) 
            : (userB, userA);

        var dbContext = await GetDbContextAsync();
        return await dbContext.Conversations
            .FirstOrDefaultAsync(c => c.User1Id == user1Id && c.User2Id == user2Id, cancellationToken);
    }

    public async Task<List<Conversation>> GetUserConversationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();
        return await dbContext.Conversations
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .OrderByDescending(c => c.CreationTime)
            .ToListAsync(cancellationToken);
    }
}
