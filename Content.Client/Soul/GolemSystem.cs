using Robust.Shared.Containers;
using Content.Shared.Soul;

namespace Content.Client.Soul;

public sealed class GolemSystem : SharedGolemSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GolemComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<GolemComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
    }

    private void OnEntInserted(EntityUid uid, SharedGolemComponent component, EntInsertedIntoContainerMessage args)
    {
        SharedOnEntInserted(args);
    }

    private void OnEntRemoved(EntityUid uid, SharedGolemComponent component, EntRemovedFromContainerMessage args)
    {
        SharedOnEntRemoved(args);
    }
}
