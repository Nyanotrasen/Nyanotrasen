using Content.Server.Maps;
using Content.Server.Players;
using Content.Server.Popups;
using Content.Server.Stunnable;
using Content.Shared.Examine;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using static Content.Shared.Examine.ExamineSystemShared;

namespace Content.Server.Fugitive
{
    public sealed class FugitiveSpawnerSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly StunSystem _stun = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly TileSystem _tile = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FugitiveSpawnerComponent, PlayerAttachedEvent>(OnPlayerAttached);
        }

        private void OnPlayerAttached(EntityUid uid, FugitiveSpawnerComponent component, PlayerAttachedEvent args)
        {
            var xform = Transform(uid);
            var fugitive = Spawn(component.Prototype, xform.Coordinates);

            if (TryComp<FugitiveCountdownComponent>(fugitive, out var cd))
                cd.AnnounceTime = _timing.CurTime + cd.AnnounceCD;

            _popupSystem.PopupEntity(Loc.GetString("fugitive-spawn", ("name", fugitive)), fugitive,
                Filter.Pvs(fugitive).RemoveWhereAttachedEntity(entity => !ExamineSystemShared.InRangeUnOccluded(
                    fugitive, entity, ExamineRange, null)), true,
                Shared.Popups.PopupType.LargeCaution);

            _stun.TryParalyze(fugitive, TimeSpan.FromSeconds(2), false);
            _audioSystem.PlayPvs(component.SpawnSoundPath, uid, AudioParams.Default.WithVolume(-6f));

            if (!_mapManager.TryGetGrid(xform.GridUid, out var map))
                return;
            var currentTile = map.GetTileRef(xform.Coordinates);
            _tile.PryTile(currentTile);

            args.Player.ContentData()?.Mind?.TransferTo(fugitive, true);
        }
    }
}
