using Content.Shared.Abilities.Psionics;
using Content.Shared.Bed.Sleep;
using Content.Shared.Drugs;

namespace Content.Server.Psionics
{
    public struct ReceptiveTelepathicEntityEnumerator : IDisposable
    {
        private IEntityManager _entityManager;

        private EntityQueryEnumerator<PsionicComponent> _psionicEnumerator;
        private EntityQueryEnumerator<SleepingComponent> _sleepingEnumerator;
        private EntityQueryEnumerator<SeeingRainbowsComponent> _hallucinatingEnumerator;

        private EntityQuery<PsionicsDisabledComponent> _disabled;
        private EntityQuery<PsionicInsulationComponent> _insulated;

        private bool _getDreamers;

        public ReceptiveTelepathicEntityEnumerator(IEntityManager entityManager, bool getDreamers)
        {
            _entityManager = entityManager;
            _getDreamers = getDreamers;

            _psionicEnumerator = _entityManager.EntityQueryEnumerator<PsionicComponent>();
            _sleepingEnumerator = _entityManager.EntityQueryEnumerator<SleepingComponent>();
            _hallucinatingEnumerator = _entityManager.EntityQueryEnumerator<SeeingRainbowsComponent>();

            _disabled = _entityManager.GetEntityQuery<PsionicsDisabledComponent>();
            _insulated = _entityManager.GetEntityQuery<PsionicInsulationComponent>();
        }

        public bool MoveNext(out EntityUid uid, out bool isDreamer)
        {
            while (true)
            {
                EntityUid entity;

                if (_psionicEnumerator.MoveNext(out entity, out var _))
                {
                    isDreamer = false;
                }
                else if (_getDreamers &&
                    (_sleepingEnumerator.MoveNext(out entity, out var _) ||
                     _hallucinatingEnumerator.MoveNext(out entity, out var _)))
                {
                    isDreamer = true;
                }
                else
                {
                    isDreamer = false;
                    uid = default;
                    return false;
                }

                if (_disabled.HasComponent(entity) || _insulated.HasComponent(entity))
                    continue;

                uid = entity;
                return true;
            }
        }

        public void Dispose()
        {
            _psionicEnumerator.Dispose();
            _sleepingEnumerator.Dispose();
            _hallucinatingEnumerator.Dispose();
        }
    }
}
