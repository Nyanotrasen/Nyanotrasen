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

        public bool CheckLOS = false;
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
                    !viewBox.Contains(Transform(entity).WorldPosition))
                {
                    if (_guis.TryGetValue(entity, out var oldGui))
                    {
                        _guis.Remove(entity);
                        oldGui.Dispose();
                    }

                    continue;
                }

                var distance = (Transform(entity).Coordinates.Position - viewBox.Center).Length;


                if (CheckLOS && !ExamineSystemShared.InRangeUnOccluded(ourXform.MapPosition, Transform(entity).MapPosition, distance, e => e == _attachedEntity.Value, entMan: _entities))
                {
                    if (_guis.TryGetValue(entity, out var oldGui))
                    {
                        _guis.Remove(entity);
                        oldGui.Dispose();
                    }

                    continue;
                }

                if (_guis.ContainsKey(entity))
                {
                    continue;
                }

                var gui = new EntityHealthBarGui(entity);
                _guis.Add(entity, gui);
            }
        }
    }
}
