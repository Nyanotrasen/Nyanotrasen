using System.Linq;
using Content.Shared.Interaction;
using Content.Shared.Research.Prototypes;
using Content.Shared.Abilities.Psionics;
using Content.Server.Chat.Systems;
using Content.Server.Chat.Managers;
using Content.Server.Botany;
using Content.Server.Psionics;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Server.GameObjects;

namespace Content.Server.Research.Oracle
{
    public sealed class OracleSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly PsionicsSystem _psionicsSystem = default!;

        [ViewVariables(VVAccess.ReadWrite)]
        public readonly IReadOnlyList<string> DemandMessages = new[]
        {
            "oracle-demand-1",
            "oracle-demand-2",
            "oracle-demand-3",
            "oracle-demand-4",
            "oracle-demand-5",
            "oracle-demand-6",
            "oracle-demand-7",
            "oracle-demand-8",
            "oracle-demand-9",
            "oracle-demand-10",
            "oracle-demand-11",
            "oracle-demand-12"
        };

        public readonly IReadOnlyList<String> RejectMessages = new[]
        {
            "ἄγνοια",
            "υλικό",
            "ἀγνωσία",
            "γήινος",
            "σάκλας"
        };

        public readonly IReadOnlyList<String> BlacklistedProtos = new[]
        {
            "MobTomatoKiller",
            "Drone",
            "QSI",
            "BluespaceBeaker",
            "BackpackOfHolding",
            "SatchelOfHolding",
            "DuffelbagOfHolding",
            "TrashBagOfHolding",
            "BluespaceCrystal",
            "InsulativeHeadcage",
            "CrystalNormality",
            "BodyBag_Folded",
            "BodyBag",
            "LockboxDecloner",
            "MopBucket",
            "FoodOnionRed",
            "FoodGatfruit",
            "TargetHuman",
            "TargetSyndicate",
            "TargetClown",
            "PowerCellSmallPrinted",
            "PowerCellMediumPrinted",
            "PowerCellHighPrinted",
        };


        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var oracle in EntityQuery<OracleComponent>())
            {
                oracle.Accumulator += frameTime;
                oracle.BarkAccumulator += frameTime;
                if (oracle.BarkAccumulator >= oracle.BarkTime.TotalSeconds)
                {
                    oracle.BarkAccumulator = 0;
                    string message = Loc.GetString(_random.Pick(DemandMessages), ("item", oracle.DesiredPrototype.Name)).ToUpper();
                    _chat.TrySendInGameICMessage(oracle.Owner, message, InGameICChatType.Speak, false);
                }

                if (oracle.Accumulator >= oracle.ResetTime.TotalSeconds)
                {
                    oracle.LastDesiredPrototype = oracle.DesiredPrototype;
                    NextItem(oracle);
                }
            }
        }
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<OracleComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<OracleComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<OracleComponent, InteractUsingEvent>(OnInteractUsing);
        }

        private void OnInit(EntityUid uid, OracleComponent component, ComponentInit args)
        {
            NextItem(component);
        }

        private void OnInteractHand(EntityUid uid, OracleComponent component, InteractHandEvent args)
        {
            if (!HasComp<PotentialPsionicComponent>(args.User) || HasComp<PsionicInsulationComponent>(args.User))
                return;

            if (!TryComp<ActorComponent>(args.User, out var actor))
                return;

            var message = Loc.GetString("oracle-current-item", ("item", component.DesiredPrototype.Name));

            var messageWrap = Loc.GetString("chat-manager-send-telepathic-chat-wrap-message",
                ("telepathicChannelName", Loc.GetString("chat-manager-telepathic-channel-name")), ("message", message));

            _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Telepathic,
                message, messageWrap, uid, false, actor.PlayerSession.ConnectedClient, Color.PaleVioletRed);

            if (component.LastDesiredPrototype != null)
            {
                var message2 = Loc.GetString("oracle-previous-item", ("item", component.LastDesiredPrototype.Name));
                var messageWrap2 = Loc.GetString("chat-manager-send-telepathic-chat-wrap-message",
                    ("telepathicChannelName", Loc.GetString("chat-manager-telepathic-channel-name")), ("message", message2));

                _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Telepathic,
                    message2, messageWrap2, uid, false, actor.PlayerSession.ConnectedClient, Color.PaleVioletRed);
            }
        }
        private void OnInteractUsing(EntityUid uid, OracleComponent component, InteractUsingEvent args)
        {
            if (!TryComp<MetaDataComponent>(args.Used, out var meta))
                return;

            if (meta.EntityPrototype == null)
                return;

            var validItem = false;

            var nextItem = true;

            if (meta.EntityPrototype.ID.TrimEnd('1') == component.DesiredPrototype.ID.TrimEnd('1'))
                validItem = true;

            if (component.LastDesiredPrototype != null && meta.EntityPrototype.ID.TrimEnd('1') == component.LastDesiredPrototype.ID.TrimEnd('1'))
            {
                nextItem = false;
                validItem = true;
                component.LastDesiredPrototype = null;
            }

            /// The trim helps with stacks and singles
            if (!validItem)
            {
                _chat.TrySendInGameICMessage(uid, _random.Pick(RejectMessages), InGameICChatType.Speak, true);
                return;
            }

            EntityManager.QueueDeleteEntity(args.Used);

            EntityManager.SpawnEntity("ResearchDisk5000", Transform(args.User).Coordinates);

            if (TryComp<PotentialPsionicComponent>(args.User, out var potential))
                _psionicsSystem.RollPsionics(args.User, potential);

            int i = _random.Next(1, 4);

            while (i != 0)
            {
                EntityManager.SpawnEntity("MaterialBluespace", Transform(args.User).Coordinates);
                i--;
            }

            if (nextItem)
                NextItem(component);
        }

        private void NextItem(OracleComponent component)
        {
            component.Accumulator = 0;
            component.BarkAccumulator = 0;
            var protoString = GetDesiredItem();
            if (_prototypeManager.TryIndex<EntityPrototype>(protoString, out var proto))
                component.DesiredPrototype = proto;
            else
                Logger.Error("Orcale can't index prototype " + protoString);
        }

        private string GetDesiredItem()
        {
            var allRecipes = _prototypeManager.EnumeratePrototypes<LatheRecipePrototype>().Select(x => x.Result).ToList();
            var allPlants = _prototypeManager.EnumeratePrototypes<SeedPrototype>().Select(x => x.ProductPrototypes[0]).ToList();
            var allProtos = allRecipes.Concat(allPlants).ToList();
            foreach (var proto in BlacklistedProtos)
                allProtos.Remove(proto);
            return _random.Pick((allProtos));
        }
    }
}
