using Content.Shared.Actions;
using Content.Shared.Audio;
using Content.Shared.Maps;
using Content.Shared.Body.Components;
using Content.Server.Bible.Components;
using Content.Shared.Input;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Players;
using Robust.Server.Player;

namespace Content.Server.Abilities.Fart
{
    public sealed class FartSystem : EntitySystem
    {
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            CommandBinds.Builder
                .Bind(ContentKeyFunctions.Fart, InputCmdHandler.FromDelegate(HandleFart))
                .Register<FartSystem>();
        }

        private void HandleFart(ICommonSession? session)
        {
            Logger.Error("Handling fart...");
            if (session is not IPlayerSession playerSession)
                return;
            if (playerSession.AttachedEntity is not {Valid: true} plyEnt || !Exists(plyEnt))
                return;
            if (!TryComp<FarterComponent>(playerSession.AttachedEntity, out var farter))
                return;
            if (farter.Stream != null)
                farter.Stream.Stop();

            farter.Stream = SoundSystem.Play(Filter.Pvs(farter.Owner), farter.FartSound.GetSound(), farter.Owner, AudioHelpers.WithVariation(0.3f));
            if (TryComp<SharedBodyComponent>(farter.Owner, out var body))
            {
                foreach (var entity in Transform(farter.Owner).Coordinates.GetEntitiesInTile())
                {
                    if (HasComp<BibleComponent>(entity) && !_containerSystem.IsEntityInContainer(entity))
                    {
                        body.Gib();
                        return;
                    }
                }
            }
        }
    }

    public sealed class FartActionEvent : InstantActionEvent {}
}
