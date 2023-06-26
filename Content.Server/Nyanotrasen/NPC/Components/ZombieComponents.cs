namespace Content.Server.NPC;

[RegisterComponent]
[Access(typeof(NPCZombieSystem))]
public sealed class ZombieWakeupOnTriggerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? ToZombify;
}

[RegisterComponent]
[Access(typeof(NPCZombieSystem))]
public sealed class ZombieSurpriseComponent : Component { }

[RegisterComponent]
[Access(typeof(NPCZombieSystem))]
public sealed class ZombifiedOnSpawnComponent : Component { }

