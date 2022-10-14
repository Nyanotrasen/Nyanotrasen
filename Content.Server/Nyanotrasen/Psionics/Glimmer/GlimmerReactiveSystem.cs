using Content.Shared.GameTicking;
using Content.Shared.Psionics.Glimmer;

namespace Content.Server.Psionics.Glimmer
{
    public sealed class GlimmerReactiveSystem : EntitySystem
    {
        [Dependency] private readonly SharedGlimmerSystem _sharedGlimmerSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

        public float Accumulator = 0;
        public const float UpdateFrequency = 15f;
        public GlimmerTier LastGlimmerTier = GlimmerTier.Minimal;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);

            SubscribeLocalEvent<SharedGlimmerReactiveComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<SharedGlimmerReactiveComponent, ComponentRemove>(OnComponentRemove);
        }

        /// <summary>
        /// Track when the component comes online so it can be given the
        /// current status of the glimmer level, if it wasn't around when an
        /// update went out.
        /// </summary>
        private void OnComponentInit(EntityUid uid, SharedGlimmerReactiveComponent component, ComponentInit args)
        {
            _appearanceSystem.SetData(uid, GlimmerReactiveVisuals.GlimmerTier, (int) _sharedGlimmerSystem.GetGlimmerTier());
        }

        /// <summary>
        /// Reset the glimmer level appearance data if the component's removed,
        /// just in case some objects can temporarily become reactive to the
        /// glimmer.
        /// </summary>
        private void OnComponentRemove(EntityUid uid, SharedGlimmerReactiveComponent component, ComponentRemove args)
        {
            _appearanceSystem.SetData(uid, GlimmerReactiveVisuals.GlimmerTier, 0);
        }

        private void Reset(RoundRestartCleanupEvent args)
        {
            Accumulator = 0;
            LastGlimmerTier = GlimmerTier.Minimal;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            Accumulator += frameTime;

            if (Accumulator > UpdateFrequency)
            {
                var currentGlimmerTier = _sharedGlimmerSystem.GetGlimmerTier();
                if (currentGlimmerTier != LastGlimmerTier) {
                    var ev = new GlimmerTierChangedEvent(LastGlimmerTier, currentGlimmerTier, currentGlimmerTier > LastGlimmerTier);

                    foreach (var reactive in EntityQuery<SharedGlimmerReactiveComponent>())
                    {
                        _appearanceSystem.SetData(reactive.Owner, GlimmerReactiveVisuals.GlimmerTier, (int) currentGlimmerTier);
                        RaiseLocalEvent(reactive.Owner, ev);
                    }

                    LastGlimmerTier = currentGlimmerTier;
                }
                Accumulator = 0;
            }
        }
    }

    /// <summary>
    /// This event is fired when the broader glimmer tier has changed,
    /// not on every single adjustment to the glimmer count.
    ///
    /// <see cref="SharedGlimmerSystem.GetGlimmerTier"/> has the exact
    /// values corresponding to tiers.
    /// </summary>
    public class GlimmerTierChangedEvent : EntityEventArgs
    {
        /// <summary>
        /// What was the last glimmer level before this event fired?
        /// </summary>
        public readonly GlimmerTier LastLevel;

        /// <summary>
        /// What is the current glimmer level?
        /// </summary>
        public readonly GlimmerTier CurrentLevel;

        /// <summary>
        /// Does this event signify an increase in the glimmer?
        /// It decreased, if this value is false.
        /// </summary>
        public readonly bool LevelIncreased;

        public GlimmerTierChangedEvent(GlimmerTier lastLevel, GlimmerTier currentLevel, bool levelIncreased)
        {
            LastLevel = lastLevel;
            CurrentLevel = currentLevel;
            LevelIncreased = levelIncreased;
        }
    }
}

