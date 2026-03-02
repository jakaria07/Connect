using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Wafi.Connect.Chat;
using Wafi.Connect.EntityFrameworkCore;

namespace Wafi.Connect.Repositories;

public class MessageRepository : EfCoreRepository<ConnectDbContext, Message, Guid>, IMessageRepository
{
    public MessageRepository(IDbContextProvider<ConnectDbContext> dbContextProvider) 
        : base(dbContextProvider)
    {
    }

    public async Task<List<Message>> GetByConversationAsync(
        Guid conversationId, 
        int skip, 
        int take, 
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();
        return await dbContext.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreationTime)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
