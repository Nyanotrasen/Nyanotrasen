using Content.Shared.CharacterAppearance.Components;

namespace Content.Shared.Lamiae;

public sealed class SharedLamiaeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LamiaSegmentComponent, SegmentSpawnedEvent>(OnSegmentSpawned);
    }

    private void OnSegmentSpawned(EntityUid uid, LamiaSegmentComponent component, SegmentSpawnedEvent args)
    {
        component.Lamia = args.Lamia;

        if (TryComp<HumanoidAppearanceComponent>(args.Lamia, out var appearanceComponent))
        {
            Logger.Error("We have an appearance component.");
            Logger.Error(appearanceComponent.Species);
            foreach (var marking in appearanceComponent.Appearance.Markings)
            {
                Logger.Error(marking.ToString());
            }
        }
    }
}

public sealed class SegmentSpawnedEvent : EntityEventArgs
{
    public EntityUid Lamia = default!;

    public SegmentSpawnedEvent(EntityUid lamia)
    {
        Lamia = lamia;
    }
}