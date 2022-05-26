using Content.Server.Chemistry.EntitySystems;

namespace Content.Server.Chemistry.AutoRegenReagent
{
    public sealed class AutoRegenReagentSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<AutoRegenReagentComponent, ComponentInit>(OnInit);
        }

        private void OnInit(EntityUid uid, AutoRegenReagentComponent component, ComponentInit args)
        {
            if (component.SolutionName == null)
                return;
            if (_solutionSystem.TryGetSolution(uid, component.SolutionName, out var solution))
                component.Solution = solution;
        }
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var autoComp in EntityQuery<AutoRegenReagentComponent>())
            {
                if (autoComp.Solution == null)
                    return;
                autoComp.Accumulator += frameTime;
                if (autoComp.Accumulator < 1f)
                    continue;
                autoComp.Accumulator -= 1f;

                _solutionSystem.TryAddReagent(autoComp.Owner, autoComp.Solution, autoComp.Reagent, autoComp.unitsPerSecond, out var accepted);
            }
        }
    }
}