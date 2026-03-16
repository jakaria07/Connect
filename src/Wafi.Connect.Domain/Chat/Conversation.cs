using System;
using Volo.Abp.Domain.Entities;

namespace Wafi.Connect.Chat;

public class Conversation : AggregateRoot<Guid>
{
    public Guid User1Id { get; private set; }
    public Guid User2Id { get; private set; }
    public DateTime CreationTime { get; private set; }
    public bool IsArchived { get; private set; }

    private Conversation()
    {
        /* Required by EF Core */
    }

    public Conversation(Guid id, Guid user1Id, Guid user2Id)
    {
        Id = id;
        User1Id = user1Id;
        User2Id = user2Id;
        CreationTime = DateTime.UtcNow;
        IsArchived = false;
    }

    public void Archive()
    {
        IsArchived = true;
    }

    public void Unarchive()
    {
        IsArchived = false;
    }

    public bool IsParticipant(Guid userId)
    {
        return User1Id == userId || User2Id == userId;
    }
}
