using Robust.Shared.Physics;
using Content.Shared.Lamiae;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.CharacterAppearance;
using Content.Shared.CharacterAppearance.Systems;
using Content.Shared.Species;
using Content.Server.Access.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Lamiae
{
    public sealed class LamiaSystem : EntitySystem
    {
        [Dependency] private readonly SharedJointSystem _jointSystem = default!;
        [Dependency] private readonly SharedHumanoidAppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypes = default!;
        [Dependency] private readonly IdCardSystem _idCardSystem = default!;

        Queue<(LamiaSegmentComponent segment, EntityUid lamia)> _segments = new();
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var segment in _segments)
            {
                var ev = new SegmentSpawnedEvent(segment.lamia);
                RaiseLocalEvent(segment.segment.Owner, ev, false);

                if (segment.segment.SegmentNumber == 1)
                {
                    var revoluteJoint = _jointSystem.CreateRevoluteJoint(segment.segment.AttachedToUid, segment.segment.Owner, id: ("Segment" + segment.segment.SegmentNumber));
                    revoluteJoint.CollideConnected = false;
                }
                var joint = _jointSystem.CreateDistanceJoint(segment.segment.AttachedToUid, segment.segment.Owner, id: ("Segment" + segment.segment.SegmentNumber));
                joint.CollideConnected = false;
                joint.Stiffness = 0.2f;
            }
            _segments.Clear();
        }
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<LamiaComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<LamiaSegmentComponent, SegmentSpawnedEvent>(OnSegmentSpawned);
        }

        private void OnSegmentSpawned(EntityUid uid, LamiaSegmentComponent component, SegmentSpawnedEvent args)
        {
            component.Lamia = args.Lamia;

            if (!TryComp<SpriteComponent>(uid, out var sprite))
                return;

            if (TryComp<HumanoidAppearanceComponent>(args.Lamia, out var appearanceComponent))
            {
                if (!HasComp<LamiaSexEnforcedComponent>(args.Lamia))
                {
                    if (appearanceComponent.Sex == Sex.Female)
                    {
                        AddComp<LamiaSexEnforcedComponent>(args.Lamia);
                    }
                    else
                    {
                        _appearanceSystem.UpdateSexGender(args.Lamia, Sex.Female, Robust.Shared.Enums.Gender.Female);
                        var name = "";
                        if (_prototypes.TryIndex<SpeciesPrototype>("Lamia", out var lamiaSpecies))
                        {
                            name += Sex.Female.GetFirstName(lamiaSpecies);
                            name += " ";
                            name += Sex.Female.GetLastName(lamiaSpecies);
                            MetaData(args.Lamia).EntityName = name;

                            if (_idCardSystem.TryFindIdCard(args.Lamia, out var card))
                            {
                                card.FullName = name;
                            }
                        }

                        AddComp<LamiaSexEnforcedComponent>(args.Lamia);
                    }
                }

                foreach (var marking in appearanceComponent.Appearance.Markings)
                {
                    if (marking.MarkingId != "LamiaBottom")
                        continue;

                    var color = marking.MarkingColors[0];
                    sprite.LayerSetColor(0, color);
                }
            }
        }

        private void OnInit(EntityUid uid, LamiaComponent component, ComponentInit args)
        {
            int i = 1;
            var addTo = uid;
            while (i < component.NumberOfSegments + 1)
            {
                var segment = AddSegment(addTo, uid, component, i);
                addTo = segment;
                i++;
            }
        }

        private EntityUid AddSegment(EntityUid uid, EntityUid lamia, LamiaComponent lamiaComponent, int segmentNumber)
        {
            LamiaSegmentComponent segmentComponent = new();
            segmentComponent.AttachedToUid = uid;
            EntityUid segment;
            if (segmentNumber == 1)
                segment = EntityManager.SpawnEntity("LamiaInitialSegment", Transform(uid).Coordinates.Offset((0f, 0.15f)));
            else if (segmentNumber == lamiaComponent.NumberOfSegments)
                segment = EntityManager.SpawnEntity("LamiaSegmentEnd", Transform(uid).Coordinates.Offset((0f, 0.12f)));
            else
                segment = EntityManager.SpawnEntity("LamiaSegment", Transform(uid).Coordinates.Offset((0f, 0.15f)));

            segmentComponent.Owner = segment;
            segmentComponent.SegmentNumber = segmentNumber;
            EntityManager.AddComponent(segment, segmentComponent, true);
            _segments.Enqueue((segmentComponent, lamia));
            return segment;
        }
    }
}
