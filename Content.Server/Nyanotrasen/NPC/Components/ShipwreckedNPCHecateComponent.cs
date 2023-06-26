using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.NPC.Events;
using Content.Server.NPC.Prototypes;

namespace Content.Server.NPC.Components;

[RegisterComponent]
[Access(typeof(ShipwreckedRuleSystem))]
public sealed class ShipwreckedNPCHecateComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public ShipwreckedRuleComponent? Rule;

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? GunSafe;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool UnlockedSafe;

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? EngineBayDoor;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool UnlockedEngineBay;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool Launching;
}



[Access(typeof(ShipwreckedRuleSystem))]
public sealed class ShipwreckedHecateAskGeneratorUnlockEvent : NPCConversationEvent
{
    [DataField("accessGranted", required: true)]
    public readonly NPCResponse AccessGranted = default!;
}

[Access(typeof(ShipwreckedRuleSystem))]
public sealed class ShipwreckedHecateAskWeaponsUnlockEvent : NPCConversationEvent { }

[Access(typeof(ShipwreckedRuleSystem))]
public sealed class ShipwreckedHecateAskWeaponsEvent : NPCConversationEvent
{
    [DataField("beforeUnlock", required: true)]
    public readonly NPCResponse BeforeUnlock = default!;

    [DataField("afterUnlock", required: true)]
    public readonly NPCResponse AfterUnlock = default!;
}

[Access(typeof(ShipwreckedRuleSystem))]
public abstract class ShipwreckedHecateAskStatusOrLaunchEvent : NPCConversationEvent
{
    [DataField("needConsole", required: true)]
    public readonly NPCResponse NeedConsole = default!;

    [DataField("needGenerator", required: true)]
    public readonly NPCResponse NeedGenerator = default!;

    [DataField("needThrusters", required: true)]
    public readonly NPCResponse NeedThrusters = default!;

}

[Access(typeof(ShipwreckedRuleSystem))]
public sealed class ShipwreckedHecateAskStatusEvent : ShipwreckedHecateAskStatusOrLaunchEvent
{
    [DataField("allGreenFirst", required: true)]
    public readonly NPCResponse AllGreenFirst = default!;

    [DataField("allGreenAgain", required: true)]
    public readonly NPCResponse AllGreenAgain = default!;
}

[Access(typeof(ShipwreckedRuleSystem))]
public sealed class ShipwreckedHecateAskLaunchEvent : ShipwreckedHecateAskStatusOrLaunchEvent
{
    [DataField("launch", required: true)]
    public readonly NPCResponse Launch = default!;
}

