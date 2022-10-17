using Content.Server.Objectives.Interfaces;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Psionics.Glimmer;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class Glimmer400Condition : IObjectiveCondition
    {
        private Mind.Mind? _mind;
        public IObjectiveCondition GetAssigned(Mind.Mind mind)
        {
            return new Glimmer400Condition {_mind = mind};
        }

        public string Title => Loc.GetString("objective-condition-glimmer-400-title");

        public string Description => Loc.GetString("objective-condition-glimmer-400-description");

        public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ResourcePath("Nyanotrasen/Icons/psi.rsi"), "psi");

        public float Progress
        {
            get {
                var glimmer = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedGlimmerSystem>();

                var progress = Math.Min((float) glimmer.Glimmer / 400f, 1f);
                return progress;
            }
        }

        public float Difficulty => 2f;

        public bool Equals(IObjectiveCondition? other)
        {
            return other is Glimmer400Condition condition && Equals(_mind, condition._mind);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Glimmer400Condition) obj);
        }

        public override int GetHashCode()
        {
            return (_mind != null ? _mind.GetHashCode() : 0);
        }
    }
}
