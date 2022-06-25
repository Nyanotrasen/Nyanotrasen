using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Xenoarchaeology.XenoArtifacts;

public sealed class ArtifactSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArtifactComponent, MapInitEvent>(OnInit);
    }

    private void OnInit(EntityUid uid, ArtifactComponent component, MapInitEvent args)
    {
        if (component.RandomTrigger)
        {
            AddRandomTrigger(uid, component);
        }
    }

    private void AddRandomTrigger(EntityUid uid, ArtifactComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var triggerName = _random.Pick(component.PossibleTriggers);
        var trigger = (Component) _componentFactory.GetComponent(triggerName);
        trigger.Owner = uid;

        if (EntityManager.HasComponent(uid, trigger.GetType()))
        {
            Logger.Error($"Attempted to add a random artifact trigger ({triggerName}) to an entity ({ToPrettyString(uid)}), but it already has the trigger");
            return;
        }

        EntityManager.AddComponent(uid, trigger);
        RaiseLocalEvent(uid, new RandomizeTriggerEvent(), true);
    }

    public bool TryActivateArtifact(EntityUid uid, EntityUid? user = null,
        ArtifactComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        // check if artifact is under suppression field
        if (component.IsSuppressed)
            return false;

        // check if artifact isn't under cooldown
        var timeDif = _gameTiming.CurTime - component.LastActivationTime;
        if (timeDif.TotalSeconds < component.CooldownTime)
            return false;

        ForceActivateArtifact(uid, user, component);
        return true;
    }

    public void ForceActivateArtifact(EntityUid uid, EntityUid? user = null,
        ArtifactComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.LastActivationTime = _gameTiming.CurTime;

        var ev = new ArtifactActivatedEvent()
        {
            Activator = user
        };
        RaiseLocalEvent(uid, ev, true);
    }
}
