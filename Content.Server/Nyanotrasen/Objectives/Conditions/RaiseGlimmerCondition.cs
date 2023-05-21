using Content.Server.Objectives.Interfaces;
using Content.Shared.Psionics.Glimmer;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class RaiseGlimmerCondition : IObjectiveCondition
    {
        private Mind.Mind? _mind;
        [DataField("target")] private int _target = 500;

        public IObjectiveCondition GetAssigned(Mind.Mind mind)
        {
            return new RaiseGlimmerCondition {_mind = mind};
        }

        public string Title => Loc.GetString("objective-condition-raise-glimmer-title", ("target", _target));

        public string Description => Loc.GetString("objective-condition-raise-glimmer-description", ("target", _target));

        public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ("Nyanotrasen/Icons/psi.rsi"), "psi");

        public float Progress
        {
            get {
                var glimmer = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedGlimmerSystem>();

                var progress = Math.Min((float) glimmer.Glimmer / (float) _target, 1f);
                return progress;
            }
        }

        public float Difficulty => 2.5f;

        public bool Equals(IObjectiveCondition? other)
        {
            return other is RaiseGlimmerCondition condition && Equals(_mind, condition._mind);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((RaiseGlimmerCondition) obj);
        }

        public override int GetHashCode()
        {
            return (_mind != null ? _mind.GetHashCode() : 0);
        }
    }
}
