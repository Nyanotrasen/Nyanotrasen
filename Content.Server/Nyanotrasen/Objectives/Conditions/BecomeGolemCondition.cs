using Content.Server.Objectives.Interfaces;
using Content.Server.Soul;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class BecomeGolemCondition : IObjectiveCondition
    {
        private Mind.Mind? _mind;
        public IObjectiveCondition GetAssigned(Mind.Mind mind)
        {
            return new BecomeGolemCondition {_mind = mind};
        }

        public string Title => Loc.GetString("objective-condition-become-golem-title");

        public string Description => Loc.GetString("objective-condition-become-golem-description");

        public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ("Nyanotrasen/Mobs/Species/Golem/cult.rsi"), "full");

        public float Progress
        {
            get {
                var entMan = IoCManager.Resolve<IEntityManager>();

                return (entMan.HasComponent<GolemComponent>(_mind?.CurrentEntity) ? 1f : 0f);
            }
        }

        public float Difficulty => 3f;

        public bool Equals(IObjectiveCondition? other)
        {
            return other is BecomeGolemCondition condition && Equals(_mind, condition._mind);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((BecomeGolemCondition) obj);
        }

        public override int GetHashCode()
        {
            return (_mind != null ? _mind.GetHashCode() : 0);
        }
    }
}
