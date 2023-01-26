using Content.Client.EntityHealthBar.UI;
using Content.Shared.Damage;
using Content.Shared.GameTicking;
using Content.Shared.Mobs.Components;
using Content.Shared.Examine;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using JetBrains.Annotations;

namespace Content.Client.EntityHealthBar
{
    [UsedImplicitly]
    public sealed class EntityHealthBarSystem : EntitySystem
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IEntityManager _entities = default!;
        [Dependency] private readonly SharedTransformSystem _transformSys = default!;

        private readonly Dictionary<EntityUid, EntityHealthBarGui> _guis = new();
        private EntityUid? _attachedEntity;
        private bool _enabled;

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value)
                {
                    return;
                }

                _enabled = value;

                foreach (var gui in _guis.Values)
                {
                    gui.SetVisibility(value);
                }
            }
        }

        /// <summary>
        /// Occlude other bars based on line-of-sight
        /// </summary>
        public bool CheckLOS = false;
        /// <summary>
        /// If this is not null, we'll only use this damage container.
        /// </summary>
        public string? DamageContainer;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<PlayerAttachSysMessage>(HandlePlayerAttached);
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            foreach (var gui in _guis.Values)
            {
                gui.Dispose();
            }

            _guis.Clear();
            _attachedEntity = default;
        }

        private void HandlePlayerAttached(PlayerAttachSysMessage message)
        {
            _attachedEntity = message.AttachedEntity;
        }


        // Yeah it kind of sucks that this iterates even when we don't have the comp
        // This problem has not been solved by anything else in the codebase right now...
        public override void FrameUpdate(float frameTime)
        {
            base.Update(frameTime);

            if (!_enabled)
            {
                return;
            }

            if (_attachedEntity is not {} ent || Deleted(ent))
            {
                return;
            }

            var viewBox = _eyeManager.GetWorldViewport().Enlarged(2.0f);
            var ourXform = Transform(_attachedEntity.Value);

            foreach (var (mobState, damageable) in EntityManager.EntityQuery<MobStateComponent, DamageableComponent>())
            {
                var entity = mobState.Owner;

                if (DamageContainer != null && damageable.DamageContainerID != DamageContainer)
                    continue;

                if (Transform(ent).MapID != Transform(entity).MapID ||
                    !viewBox.Contains(_transformSys.GetWorldPosition(entity)))
                {
                    RemoveGUIFromEntity(entity);
                    continue;
                }

                var distance = (Transform(entity).Coordinates.Position - viewBox.Center).Length;

                if (CheckLOS && !ExamineSystemShared.InRangeUnOccluded(ourXform.MapPosition, Transform(entity).MapPosition, distance, e => e == _attachedEntity.Value, entMan: _entities))
                {
                    RemoveGUIFromEntity(entity);
                    continue;
                }

                if (!_guis.ContainsKey(entity))
                {
                    var gui = new EntityHealthBarGui(entity);
                    _guis.Add(entity, gui);
                }
            }
        }

        private void RemoveGUIFromEntity(EntityUid entity)
        {
            if (_guis.TryGetValue(entity, out var oldGui))
            {
                _guis.Remove(entity);
                oldGui.Dispose();
            }
        }
    }
}
