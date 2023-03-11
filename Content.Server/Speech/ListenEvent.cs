using Content.Server.Chat.Systems;

namespace Content.Server.Speech;

public sealed class ListenEvent : EntityEventArgs
{
    public readonly EntityChat Chat;

    public ListenEvent(EntityChat chat)
    {
        Chat = chat;
    }
}

public sealed class ListenAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid Source;

    public ListenAttemptEvent(EntityUid source)
    {
        Source = source;
    }
}
