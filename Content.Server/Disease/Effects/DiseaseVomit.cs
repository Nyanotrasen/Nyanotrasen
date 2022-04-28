using JetBrains.Annotations;
using Content.Shared.Disease;
using Content.Server.Medical;

namespace Content.Server.Disease.Effects
{
    /// <summary>
    /// Adds or removes reagents from the
    /// host's chemstream.
    /// </summary>
    [UsedImplicitly]
    public sealed class DiseaseVomit : DiseaseEffect
    {
        [DataField("thirstAmount")]
        public float ThirstAmount = 15f;

        [DataField("hungerAmount")]
        public float HungerAmount = 15f;

        public override void Effect(DiseaseEffectArgs args)
        {
            var vomitSys = args.EntityManager.EntitySysManager.GetEntitySystem<VomitSystem>();

            vomitSys.Vomit(args.DiseasedEntity, ThirstAmount, HungerAmount);
        }
    }
}
