using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Content.Shared.GameTicking;
using Content.Shared.Damage;
using Content.Shared.Stacks;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Examine;
using Content.Shared.Cloning;
using Content.Shared.Atmos;
using Content.Shared.CCVar;
using Content.Shared.Humanoid.Markings;
using Content.Server.Cloning.Components;
using Content.Server.Mind.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Atmos.EntitySystems;
using Content.Server.EUI;
using Content.Server.Humanoid;
using Content.Server.MachineLinking.System;
using Content.Server.MachineLinking.Events;
using Content.Server.MobState;
using Content.Shared.Chemistry.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.Materials;
using Content.Server.Stack;
using Content.Server.Jobs;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Server.GameObjects;
using Robust.Server.Containers;
using Robust.Server.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Content.Shared.Preferences;

namespace Content.Server.Cloning
{
    public sealed class MetempsychoticMachineSystem : EntitySystem
    {
        public const string MetempsychoticHumanoidPool = "MetempsychoticHumanoidPool";
        public const string MetempsychoticNonHumanoidPool = "MetempsychoticNonhumanoidPool";

        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public string GetSpawnEntity(EntityUid uid, out SpeciesPrototype? species, int? karma = null, MetempsychoticMachineComponent? component = null)
        {
            if (!Resolve(uid, ref component))
            {
                Logger.Error("Tried to get a spawn target from someone that was not a metempsychotic machine...");
                species = null;
                return "MobHuman";
            }

            var chance = component.HumanoidBaseChance;

            if (karma != null)
            {
                chance -= ((1 - component.HumanoidBaseChance) * (float) karma);
            }

            if (_random.Prob(chance))
            {
                if (_prototypeManager.TryIndex<WeightedRandomPrototype>(MetempsychoticHumanoidPool, out var humanoidPool))
                {
                    if (_prototypeManager.TryIndex<SpeciesPrototype>(humanoidPool.Pick(), out var speciesPrototype))
                    {
                        species = speciesPrototype;
                        return speciesPrototype.Prototype;
                    } else
                    {
                        species = null;
                        Logger.Error("Could not index species for metempsychotic machine...");
                        return "MobHuman";
                    }
                }
            }

            species = null;

            if (!_prototypeManager.TryIndex<WeightedRandomPrototype>(MetempsychoticNonHumanoidPool, out var nonHumanoidPool))
            {
                Logger.Error("Could not index the pool of non humanoids for metempsychotic machine!");
                return "MobHuman";
            }

            return nonHumanoidPool.Pick();
        }
    }
}
