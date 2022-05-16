using Content.Server.Weapon.Melee.Components;
using Content.Server.Damage.Components;
using Content.Server.Wieldable.Components;
using Content.Server.Nutrition.Components;
using Content.Server.Botany.Components;
using Content.Server.Botany;
using Content.Shared.Item;
using Robust.Shared.Prototypes;

namespace Content.Server.Research.SophicScribe
{
    public sealed partial class SophicScribeSystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        public void AssembleReport(EntityUid item, EntityUid scribe, SophicScribeComponent? scribeComponent = null)
        {
            if (!Resolve(scribe, ref scribeComponent))
                return;

            scribeComponent.SpeechQueue.Enqueue(Loc.GetString("sophic-entity-name", ("item", item)));

            if (TryComp<SharedItemComponent>(item, out var itemComp))
                scribeComponent.SpeechQueue.Enqueue(AssembleReport(itemComp));

            if (TryComp<MeleeWeaponComponent>(item, out var melee))
                scribeComponent.SpeechQueue.Enqueue(AssembleReport(melee));

            if (TryComp<FoodComponent>(item, out var food))
                scribeComponent.SpeechQueue.Enqueue(AssembleReport(food));

            if (TryComp<ProduceComponent>(item, out var produce))
                scribeComponent.SpeechQueue.Enqueue(AssembleReport(produce));
        }
        private string AssembleReport(MeleeWeaponComponent comp)
        {
            var report = "It's a melee weapon. ";

            if (comp.Range != 1f)
                report += ("It has a range of " + comp.Range.ToString() + " meters. ");
            else
                report += "It has a range of one meter. ";

            foreach (var type in comp.Damage.DamageDict)
            {
                report += ("It deals " + type.Value + " " + type.Key.ToLower() + " damage. ");
            }

            if (comp.Damage.DamageDict.Count > 1)
                report += ("In total, it deals " + comp.Damage.Total + " damage. ");

            if (TryComp<IncreaseDamageOnWieldComponent>(comp.Owner, out var wielded))
            {
                if (wielded.Modifiers.Coefficients.Values.Count > 0)
                {
                    foreach (var coefficient in wielded.Modifiers.Coefficients)
                    {
                        report += ("Wielding it will increase " + coefficient.Key.ToLower() + " damage by " + (coefficient.Value * 100).ToString() + "%. ");
                    }
                }

                if (wielded.Modifiers.FlatReduction.Values.Count > 0)
                {
                    foreach (var reduction in wielded.Modifiers.FlatReduction)
                    {
                        report += ("Wielding it will increase " + reduction.Key.ToLower() + " damage by " + Math.Abs(reduction.Value) + ". ");
                    }
                }
            }

            if (TryComp<DamageOtherOnHitComponent>(comp.Owner, out var thrown))
            {
                report += ("It can be thrown to deal a total of " +  thrown.Damage.Total + " damage. ");
            }

            return report;
        }

        private string AssembleReport(SharedItemComponent item)
        {
            string report = ("It has a size of " + item.Size.ToString() + ". ");

            var slot = item.SlotFlags.ToString().ToLower();

            if (slot != "preventequip")
                report += "It can be equipped on the " + slot + ". ";

            return report;
        }

        private string AssembleReport(FoodComponent food)
        {
            string report = ("It's edible.");

            return report;
        }

        private string AssembleReport(ProduceComponent produce)
        {
            string report = ("It can be grown in hydroponics. ");

            if (produce.SeedId != null && _prototypeManager.TryIndex<SeedPrototype>(produce.SeedId, out var seedPrototype))
            {
                report += "It comes from a " + seedPrototype.DisplayName + ". ";
            }

            return report;
        }
    }
}
