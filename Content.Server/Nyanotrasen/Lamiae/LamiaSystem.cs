using Robust.Shared.Physics;
using Content.Shared.Lamiae;
using Content.Shared.CharacterAppearance.Components;

namespace Content.Server.Lamiae
{
    public sealed class LamiaSystem : EntitySystem
    {
        [Dependency] private readonly SharedJointSystem _jointSystem = default!;

        Queue<(LamiaSegmentComponent segment, EntityUid lamia)> _segments = new();
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var segment in _segments)
            {
                if (segment.segment.SegmentNumber == 1)
                {
                    var revoluteJoint = _jointSystem.CreateRevoluteJoint(segment.segment.AttachedToUid, segment.segment.Owner, id: ("Segment" + segment.segment.SegmentNumber));
                    revoluteJoint.CollideConnected = false;
                    var ev = new SegmentSpawnedEvent(segment.lamia);
                    RaiseLocalEvent(segment.segment.Owner, ev, false);
                }
                var joint = _jointSystem.CreateDistanceJoint(segment.segment.AttachedToUid, segment.segment.Owner, id: ("Segment" + segment.segment.SegmentNumber));
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
            var segment1 = AddSegment(uid, uid, 1);
            var segment2 = AddSegment(segment1, uid, 2);
            var segment3 = AddSegment(segment2, uid, 3);
            component.Segments.Add(segment1);
            component.Segments.Add(segment2);
            component.Segments.Add(segment3);
        }

        private EntityUid AddSegment(EntityUid uid, EntityUid lamia, int segmentNumber)
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
            _segments.Enqueue((segmentComponent, lamia));
            return segment;
        }
    }
}
