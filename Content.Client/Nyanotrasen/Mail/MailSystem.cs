using Robust.Client.GameObjects;
using Content.Shared.Mail;

namespace Content.Client.Mail
{
    /// <summary>
    /// GenericVisualizer is not powerful enough to handle setting a string on
    /// visual data then directly relaying that string to a layer's state.
    /// I.e. there is nothing like a regex capture group for visual data.
    ///
    /// Hence why this system exists.
    ///
    /// To do this with GenericVisualizer would require a separate condition
    /// for every job value, which would be extra mess to maintain.
    ///
    /// It would look something like this, multipled a couple dozen times.
    ///
    ///   enum.MailVisuals.JobIcon:
    ///     enum.MailVisualLayers.JobStamp:
    ///       StationEngineer:
    ///         state: StationEngineer
    ///       SecurityOfficer:
    ///         state: SecurityOfficer
    /// </summary>
    public sealed class MailJobVisualizerSystem : VisualizerSystem<MailComponent>
    {
        protected override void OnAppearanceChange(EntityUid uid, MailComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            if (args.Component.TryGetData(MailVisuals.JobIcon, out string job))
                args.Sprite.LayerSetState(MailVisualLayers.JobStamp, job);
        }
    }

    public enum MailVisualLayers : byte
    {
        Icon,
        Lock,
        Fragile,
        JobStamp,
    }
}
