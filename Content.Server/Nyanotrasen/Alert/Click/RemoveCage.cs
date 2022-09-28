using Content.Shared.Alert;
using Content.Shared.Abilities.Psionics;
using JetBrains.Annotations;

namespace Content.Server.Alert.Click
{
    /// <summary>
    ///     Try to remove a headcage from yourself.
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public sealed class RemoveCage : IAlertClick
    {
        public void AlertClicked(EntityUid player)
        {
            EntitySystem.Get<PsionicItemsSystem>().ResistCage(player);
        }
    }
}
