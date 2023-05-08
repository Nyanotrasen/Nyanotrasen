using Content.Server.Objectives.Interfaces;
using Content.Shared.Abilities.Psionics;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class BecomePsionicCondition : IObjectiveCondition
    {
        private Mind.Mind? _mind;
        public IObjectiveCondition GetAssigned(Mind.Mind mind)
        {
            return new BecomePsionicCondition {_mind = mind};
        }

        public string Title => Loc.GetString("objective-condition-become-psionic-title");

        public string Description => Loc.GetString("objective-condition-become-psionic-description");

        public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ("Nyanotrasen/Icons/psi.rsi"), "psi");

        public float Progress
        {
            get {
                var entMan = IoCManager.Resolve<IEntityManager>();

                return (entMan.HasComponent<PsionicComponent>(_mind?.CurrentEntity) ? 1f : 0f);
            }
        }

        public float Difficulty => 2f;

        public bool Equals(IObjectiveCondition? other)
        {
            return other is BecomePsionicCondition condition && Equals(_mind, condition._mind);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((BecomePsionicCondition) obj);
        }

        public override int GetHashCode()
        {
            return (_mind != null ? _mind.GetHashCode() : 0);
        }
    }
}
