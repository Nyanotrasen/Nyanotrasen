using Robust.Shared.Physics;

namespace Content.Server.Lamiae
{
    public sealed class LamiaSystem : EntitySystem
    {
        [Dependency] private readonly SharedJointSystem _jointSystem = default!;

        Queue<LamiaSegmentComponent> _segments = new();
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var segment in _segments)
            {
                var joint = _jointSystem.CreateDistanceJoint(segment.AttachedToUid, segment.Owner, id: ("Segment" + segment.SegmentNumber));
                joint.CollideConnected = false;
            }
            _segments.Clear();
        }
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<LamiaComponent, ComponentInit>(OnInit);
        }

        private void OnInit(EntityUid uid, LamiaComponent component, ComponentInit args)
        {
            var segment1 = AddSegment(uid, 1);
            var segment2 = AddSegment(segment1, 2);
            AddSegment(segment2, 3);
        }

        private EntityUid AddSegment(EntityUid uid, int segmentNumber)
        {
            LamiaSegmentComponent segmentComponent = new();
            segmentComponent.AttachedToUid = uid;
            var segment = EntityManager.SpawnEntity("LamiaSegment", Transform(uid).Coordinates.Offset((0f, 0.35f)));
            segmentComponent.Owner = segment;
            segmentComponent.SegmentNumber = segmentNumber;
            EntityManager.AddComponent(segment, segmentComponent, true);
            _segments.Enqueue(segmentComponent);
            return segment;
        }
    }
}
