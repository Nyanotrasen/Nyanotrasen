using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.Actions.Nutrition.Drink;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.Hands;
using Content.Server.AI.Utility.Considerations.Nutrition.Drink;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Nutrition;

namespace Content.Server.AI.Utility.ExpandableActions.Nutrition
{
    public sealed class PickUpNearbyDrinkExp : ExpandableUtilityAction
    {
        public override float Bonus => UtilityAction.NeedsBonus;

        protected override IEnumerable<Func<float>> GetCommonConsiderations(Blackboard context)
        {
            var considerationsManager = IoCManager.Resolve<ConsiderationsManager>();
            return new[]
            {
                considerationsManager.Get<ThirstCon>().PresetCurve(context, PresetCurve.Nutrition),
                considerationsManager.Get<FreeHandCon>().BoolCurve(context),
            };
        }

        public override IEnumerable<UtilityAction> GetActions(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();

            foreach (var entity in context.GetState<NearbyDrinkState>().GetValue())
            {
                yield return new PickUpDrink() {Owner = owner, Target = entity, Bonus = Bonus};
            }
        }
    }
}
