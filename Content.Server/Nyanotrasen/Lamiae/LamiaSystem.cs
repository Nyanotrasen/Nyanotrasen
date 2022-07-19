using Robust.Shared.Physics;

namespace Content.Server.Lamiae
{
    public sealed class LamiaSystem : EntitySystem
    {
        [Dependency] private readonly SharedJointSystem _jointSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<LamiaComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<LamiaSegmentComponent, ComponentStartup>(OnSegmentStartup);
        }

        private void OnInit(EntityUid uid, LamiaComponent component, ComponentInit args)
        {
            LamiaSegmentComponent segmentComponent = new();
            segmentComponent.AttachedToUid = uid;
            var segment = EntityManager.SpawnEntity("LamiaSegment", Transform(uid).Coordinates.Offset((0f, 0.35f)));
            segmentComponent.Owner = segment;
            EntityManager.AddComponent(segment, segmentComponent, true);
        }

        private void OnSegmentStartup(EntityUid uid, LamiaSegmentComponent component, ComponentStartup args)
        {
            _jointSystem.CreateRevoluteJoint(component.AttachedToUid, uid, "Segment");
        }
    }
}
