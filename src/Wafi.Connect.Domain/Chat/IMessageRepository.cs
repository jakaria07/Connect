using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Wafi.Connect.Chat;

public interface IMessageRepository : IRepository<Message, Guid>
{
    Task<List<Message>> GetByConversationAsync(
        Guid conversationId, 
        int skip, 
        int take, 
        CancellationToken cancellationToken = default);
}
