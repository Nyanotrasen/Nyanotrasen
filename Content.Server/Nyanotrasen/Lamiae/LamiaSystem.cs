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
                if (segment.SegmentNumber == 1)
                {
                    var revoluteJoint = _jointSystem.CreateRevoluteJoint(segment.AttachedToUid, segment.Owner, id: ("Segment" + segment.SegmentNumber));
                    revoluteJoint.CollideConnected = false;
                }
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
            EntityUid segment;
            if (segmentNumber == 1)
                segment = EntityManager.SpawnEntity("LamiaInitialSegment", Transform(uid).Coordinates.Offset((0f, 0.35f)));
            else
                segment = EntityManager.SpawnEntity("LamiaSegment", Transform(uid).Coordinates.Offset((0f, 0.35f)));
            segmentComponent.Owner = segment;
            segmentComponent.SegmentNumber = segmentNumber;
            EntityManager.AddComponent(segment, segmentComponent, true);
            _segments.Enqueue(segmentComponent);
            return segment;
        }
    }
}
