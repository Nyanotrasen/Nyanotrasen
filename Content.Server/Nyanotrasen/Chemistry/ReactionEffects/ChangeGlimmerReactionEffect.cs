using Content.Shared.Chemistry.Reagent;
using Content.Shared.Psionics.Glimmer;

namespace Content.Server.Chemistry.ReactionEffects;

[DataDefinition]
public sealed class ChangeGlimmerReactionEffect : ReagentEffect
{
    /// <summary>
    ///     Added to glimmer when reaction occurs.
    /// </summary>
    [DataField("change")]
    public int Change = 1;

    public override void Effect(ReagentEffectArgs args)
    {
        var glimmersys = args.EntityManager.EntitySysManager.GetEntitySystem<SharedGlimmerSystem>();

        glimmersys.Glimmer += Change;
    }
}
