using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Wafi.Connect.Chat;

public interface IConversationRepository : IRepository<Conversation, Guid>
{
    Task<Conversation?> GetByUserPairAsync(Guid userA, Guid userB, CancellationToken cancellationToken = default);
    
    Task<List<Conversation>> GetUserConversationsAsync(Guid userId, CancellationToken cancellationToken = default);
}
