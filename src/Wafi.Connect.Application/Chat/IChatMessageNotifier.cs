using System;
using System.Threading;
using System.Threading.Tasks;

namespace Wafi.Connect.Chat;

public interface IChatMessageNotifier
{
    Task NotifyMessageReceivedAsync(Guid conversationId, MessageDto messageDto, CancellationToken cancellationToken = default);
}
