using JetBrains.Annotations;
using Content.Shared.Disease;
using Content.Server.Medical;

namespace Content.Server.Disease.Effects
{
    /// <summary>
    /// Forces you to vomit.
    /// </summary>
    [UsedImplicitly]
    public sealed class DiseaseVomit : DiseaseEffect
    {
        [DataField("thirstAmount")]
        public float ThirstAmount = -40f;

        [DataField("hungerAmount")]
        public float HungerAmount = -40f;

        public override void Effect(DiseaseEffectArgs args)
        {
            var vomitSys = args.EntityManager.EntitySysManager.GetEntitySystem<VomitSystem>();

            vomitSys.Vomit(args.DiseasedEntity, ThirstAmount, HungerAmount);
        }
    }
}
