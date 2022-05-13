﻿using Content.Shared.Standing;


namespace Content.Shared.MobState.State
{
    /// <summary>
    ///     The standard state an entity is in; no negative effects.
    /// </summary>
    public abstract class SharedNormalMobState : BaseMobState
    {
        protected override DamageState DamageState => DamageState.Alive;

        public override void EnterState(EntityUid uid, IEntityManager entityManager)
        {
            base.EnterState(uid, entityManager);
            EntitySystem.Get<StandingStateSystem>().Stand(uid);

            if (entityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
            {
                appearance.SetData(DamageStateVisuals.State, DamageState.Alive);
            }
        }
    }
}
