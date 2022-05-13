using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Pulling.Components;
using Content.Shared.Pulling;
using JetBrains.Annotations;

namespace Content.Server.Alert.Click
{
    /// <summary>
    /// Stop pulling something
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public sealed class StopBeingPulled : IAlertClick
    {
        public void AlertClicked(EntityUid player)
        {
            if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(player, null))
                return;

            if (IoCManager.Resolve<IEntityManager>().TryGetComponent<SharedPullableComponent?>(player, out var playerPullable))
            {
                EntitySystem.Get<SharedPullingSystem>().TryStopPull(playerPullable);
            }
        }
    }
}
