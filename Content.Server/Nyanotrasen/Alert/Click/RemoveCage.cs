using Content.Shared.Alert;
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

        }
    }
}
