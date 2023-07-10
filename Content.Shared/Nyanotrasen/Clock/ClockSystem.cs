using Content.Shared.Examine;

namespace Content.Shared.Nyanotrasen.Clock
{
    public sealed class ClockSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ClockComponent, ExaminedEvent>(OnExamined);
        }

        private void OnExamined(EntityUid uid, ClockComponent component, ExaminedEvent args)
        {
            Logger.Error("Received event.");
            args.PushMarkup(Loc.GetString("clock-examined", ("clock", uid), ("time", "12:00")));
        }
    }
}
