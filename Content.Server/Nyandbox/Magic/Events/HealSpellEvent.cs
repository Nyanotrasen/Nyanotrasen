using Content.Shared.Actions;
using Content.Shared.Damage;

namespace Content.Server.Magic.Events;

public sealed class HealSpellEvent : EntityTargetActionEvent
{
    [DataField("healAmount", required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier HealAmount = default!;
}
