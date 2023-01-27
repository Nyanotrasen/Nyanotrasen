using Content.Shared.Interaction;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Throwing;
using Content.Shared.Toggleable;
using Content.Shared.Soul;
using Content.Shared.Borgs;
using Content.Shared.Dataset;
using Content.Shared.Mobs;
using Content.Shared.Administration.Logs;
using Content.Shared.Humanoid;
using Content.Server.Borgs;
using Content.Server.Speech;
using Content.Server.Abilities.Psionics;
using Content.Server.Players;
using Robust.Shared.Random;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Soul
{
    public sealed class GolemSystem : EntitySystem
    {
        [Dependency] private readonly ItemSlotsSystem _slotsSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly ThrowingSystem _throwing = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypes = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly LawsSystem _laws = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;

        private const string CrystalSlot = "crystal_slot";

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SoulCrystalComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<GolemComponent, DispelledEvent>(OnDispelled);
            SubscribeLocalEvent<GolemComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<GolemComponent, GolemInstallRequestMessage>(OnInstallRequest);
            SubscribeLocalEvent<GolemComponent, GolemNameChangedMessage>(OnNameChanged);
            SubscribeLocalEvent<GolemComponent, GolemMasterNameChangedMessage>(OnMasterNameChanged);
            SubscribeLocalEvent<GolemComponent, AccentGetEvent>(OnGetAccent); // TODO: Deduplicate
        }

        private void OnAfterInteract(EntityUid uid, SoulCrystalComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;

            if (!TryComp<GolemComponent>(args.Target, out var golem))
                return;

            if (HasComp<ActorComponent>(args.Target))
                return;

            if (!TryComp<ActorComponent>(args.User, out var userActor))
                return;

            if (!HasComp<HumanoidAppearanceComponent>(args.User))
                return;

            if (!_uiSystem.TryGetUi(args.Target.Value, GolemUiKey.Key, out var ui))
                return;

            golem.PotentialCrystal = uid;

            _uiSystem.TryOpen(args.Target.Value, GolemUiKey.Key, userActor!.PlayerSession);

            string golemName = "golem";
            if (_prototypes.TryIndex<DatasetPrototype>("names_golem", out var names))
                golemName = _robustRandom.Pick(names.Values);

            var state = new GolemBoundUserInterfaceState(golemName, MetaData(args.User).EntityName);
            _uiSystem.SetUiState(ui, state);
        }

        private void OnDispelled(EntityUid uid, GolemComponent component, DispelledEvent args)
        {
            _slotsSystem.SetLock(uid, CrystalSlot, false);
            _slotsSystem.TryEject(uid, CrystalSlot, null, out var item);
            _slotsSystem.SetLock(uid, CrystalSlot, true);

            if (item == null)
                return;

            args.Handled = true;

            Vector2 direction = (_robustRandom.Next(-30, 30), _robustRandom.Next(-30, 30));
            _throwing.TryThrow(item.Value, direction, _robustRandom.Next(1, 10));

            if (TryComp<AppearanceComponent>(uid, out var appearance))
                _appearance.SetData(uid, ToggleVisuals.Toggled, false, appearance);

            if (!TryComp<ActorComponent>(uid, out var actor))
                return;

            MetaData(uid).EntityName = Loc.GetString("golem-base-name");
            MetaData(uid).EntityDescription = Loc.GetString("golem-base-desc");
            actor.PlayerSession.ContentData()?.Mind?.TransferTo(item);
            Dirty(uid);
            Dirty(MetaData(uid));
        }

        private void OnMobStateChanged(EntityUid uid, GolemComponent component, MobStateChangedEvent args)
        {
            if (args.NewMobState != MobState.Dead)
                return;

            QueueDel(uid);
            var ev = new DispelledEvent();
            RaiseLocalEvent(uid, ev, false);

            Spawn("Ash", Transform(uid).Coordinates);
            _audioSystem.PlayPvs(component.DeathSound, uid);
        }

        private void OnInstallRequest(EntityUid uid, GolemComponent component, GolemInstallRequestMessage args)
        {
            if (component.PotentialCrystal == null)
                return;

            if (args.Session.AttachedEntity == null)
                return;

            if (!TryComp<ItemSlotsComponent>(uid, out var slots))
                return;

            if (!TryComp<ActorComponent>(component.PotentialCrystal, out var actor))
                return;

            if (!_slotsSystem.TryGetSlot(uid, CrystalSlot, out var crystalSlot, slots)) // does it not have a crystal slot?
                return;

            if (_slotsSystem.GetItemOrNull(uid, CrystalSlot, slots) != null) // is the crystal slot occupied?
                return;

            // Toggle the lock and insert the crystal.
            _slotsSystem.SetLock(uid, CrystalSlot, false, slots);
            var success = _slotsSystem.TryInsert(uid, CrystalSlot, component.PotentialCrystal.Value, args.Session.AttachedEntity.Value, slots);
            _slotsSystem.SetLock(uid, CrystalSlot, true, slots);

            if (!success)
                return;

            _uiSystem.TryCloseAll(uid);

            if (component.GolemName != null && component.GolemName != "")
            {
                MetaData(uid).EntityName = component.GolemName;
            } else
            {
                if (_prototypes.TryIndex<DatasetPrototype>("names_golem", out var names))
                    MetaData(uid).EntityName = _robustRandom.Pick(names.Values);
            }
            MetaData(uid).EntityDescription = Loc.GetString("golem-installed-desc");

            if (TryComp<LawsComponent>(uid, out var laws))
            {
                string master;
                if (component.Master != null && component.Master != "")
                {
                    master = component.Master;
                } else
                {
                    master = MetaData(args.Session.AttachedEntity.Value).EntityName;
                }

                _laws.ClearLaws(uid, laws);
                _laws.AddLaw(uid, Loc.GetString("golem-law", ("master", master)), component: laws);
            }

            actor.PlayerSession.ContentData()?.Mind?.TransferTo(uid);

            if (TryComp<AppearanceComponent>(uid, out var appearance))
                _appearance.SetData(uid, ToggleVisuals.Toggled, true, appearance);

            _adminLogger.Add(Shared.Database.LogType.Action, Shared.Database.LogImpact.High, $"{ToPrettyString(args.Session.AttachedEntity.Value):player} created a golem named {ToPrettyString(uid):target} obeying a master named {(component.Master)}");

            component.PotentialCrystal = null;
            component.Master = null;
            component.GolemName = null;
            Dirty(uid);
            Dirty(MetaData(uid));
        }

        private void OnNameChanged(EntityUid uid, GolemComponent golemComponent, GolemNameChangedMessage args)
        {
            golemComponent.GolemName = args.Name;
        }

        private void OnMasterNameChanged(EntityUid uid, GolemComponent golemComponent, GolemMasterNameChangedMessage args)
        {
            golemComponent.Master = args.MasterName;
        }

        // todo deduplicate
        private void OnGetAccent(EntityUid uid, GolemComponent component, AccentGetEvent args)
        {
            args.Message = args.Message.ToUpper();
        }
    }
}
