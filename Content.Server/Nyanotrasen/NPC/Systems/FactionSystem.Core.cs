using Content.Server.NPC.Components;

namespace Content.Server.NPC.Systems;

public partial class FactionSystem : EntitySystem
{
    public void InitializeCore()
    {
        SubscribeLocalEvent<FactionComponent, GetNearbyHostilesEvent>(OnGetNearbyHostiles);
    }

    public bool ContainsFaction(EntityUid uid, string faction, FactionComponent? factions = null)
    {
        if (!Resolve(uid, ref factions, false))
            return false;

        return factions.Factions.Contains(faction);
    }

    public void AddFriendlyEntity(EntityUid uid, EntityUid fEntity, FactionComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        component.ExceptionalFriendlies.Add(fEntity);
    }

    private void OnGetNearbyHostiles(EntityUid uid, FactionComponent component, ref GetNearbyHostilesEvent args)
    {
        args.ExceptionalFriendlies.UnionWith(component.ExceptionalFriendlies);
    }
}

/// <summary>
/// Raised on an entity when it's trying to determine which nearby entities are hostile.
/// </summary>
/// <param name="ExceptionalHostiles">Entities that will be counted as hostile regardless of faction. Overriden by friendlies.</param>
/// <param name="ExceptionalFriendlies">Entities that will be counted as friendly regardless of faction. Overrides hostiles. </param>
[ByRefEvent]
public readonly record struct GetNearbyHostilesEvent(HashSet<EntityUid> ExceptionalHostiles, HashSet<EntityUid> ExceptionalFriendlies);
