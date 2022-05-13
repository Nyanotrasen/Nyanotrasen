using Content.Server.Hands.Components;
using JetBrains.Annotations;

namespace Content.Server.AI.WorldState.States.Inventory
{
    /// <summary>
    /// AKA what's in active hand
    /// </summary>
    [UsedImplicitly]
    public sealed class EquippedEntityState : StateData<EntityUid?>
    {
        public override string Name => "EquippedEntity";

        public override EntityUid? GetValue()
        {
            return IoCManager.Resolve<IEntityManager>().GetComponentOrNull<HandsComponent>(Owner)?.ActiveHandEntity;
        }
    }
}
