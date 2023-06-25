using Robust.Server.GameObjects;
using Robust.Server.Physics;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Content.Server.Disease;
using Content.Server.Explosion.EntitySystems;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Zombies;
using Content.Shared.Humanoid;
using Content.Shared.Tools.Components;
using Content.Shared.Zombies;

namespace Content.Server.NPC;

public sealed class NPCZombieSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly DiseaseSystem _diseaseSystem = default!;
    [Dependency] private readonly FactionSystem _factionSystem = default!;
    [Dependency] private readonly JointSystem _jointSystem = default!;
    [Dependency] private readonly NPCSystem _npcSystem = default!;
    [Dependency] private readonly ZombifyOnDeathSystem _zombifyOnDeathSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityZombifiedEvent>(OnZombified);

        SubscribeLocalEvent<ZombifiedOnSpawnComponent, ComponentStartup>(OnSpawnZombifiedStartup);
        SubscribeLocalEvent<ZombieSurpriseComponent, ComponentInit>(OnZombieSurpriseInit);
        SubscribeLocalEvent<ZombieWakeupOnTriggerComponent, TriggerEvent>(OnZombieWakeupTrigger);
    }

    private void OnZombified(EntityZombifiedEvent ev)
    {
        _factionSystem.ClearFactions(ev.Target, false);
        _factionSystem.AddFaction(ev.Target, "Zombie", true);

        // Add the NPC AI.
        var htn = EnsureComp<HTNComponent>(ev.Target);
        htn.RootTask = "SimpleHostileCompound";

        // This is normally set on MapInit.
        htn.Blackboard.SetValue(NPCBlackboard.Owner, ev.Target);

        // Make these zombies more of a threat.
        if (HasComp<HumanoidAppearanceComponent>(ev.Target) &&
            !HasComp<ToolComponent>(ev.Target))
        {
            var tool = AddComp<ToolComponent>(ev.Target);
            tool.Qualities.Add("Prying", _prototypeManager);
            tool.SpeedModifier = 0.3f;
            tool.UseSound = new SoundPathSpecifier("/Audio/Items/jaws_pry.ogg");

            htn.Blackboard.SetValue(NPCBlackboard.NavPry, true);
        }

        htn.Blackboard.SetValue(NPCBlackboard.NavSmash, true);
        htn.Blackboard.SetValue(NPCBlackboard.NavInteract, true);

        // Wake it, if it's not player-controlled.
        if (!HasComp<ActorComponent>(ev.Target))
            _npcSystem.WakeNPC(ev.Target);
    }

    private void OnSpawnZombifiedStartup(EntityUid uid, ZombifiedOnSpawnComponent component, ComponentStartup args)
    {
        RemCompDeferred<ZombifiedOnSpawnComponent>(uid);
        _diseaseSystem.TryAddDisease(uid, "ActiveZombieVirus");
    }

    private void OnZombieSurpriseInit(EntityUid uid, ZombieSurpriseComponent component, ComponentInit args)
    {
        // Spawn a separate collider attached to the entity.
        var trigger = Spawn("ZombieSurpriseDetector", Transform(uid).Coordinates);
        Comp<ZombieWakeupOnTriggerComponent>(trigger).ToZombify = uid;
    }

    private void OnZombieWakeupTrigger(EntityUid uid, ZombieWakeupOnTriggerComponent component, TriggerEvent args)
    {
        QueueDel(uid);

        var toZombify = component.ToZombify;
        if (toZombify == null || Deleted(toZombify))
            return;

        _zombifyOnDeathSystem.ZombifyEntity(toZombify.Value);
    }
}

