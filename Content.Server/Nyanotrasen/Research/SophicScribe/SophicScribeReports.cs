using Content.Server.Weapon.Melee.Components;
using Content.Server.Damage.Components;
using Content.Server.Wieldable.Components;
using Content.Server.Nutrition.Components;
using Content.Server.Botany.Components;
using Content.Server.Botany;
using Content.Server.Storage.Components;
using Content.Server.OfHolding;
using Content.Server.QSI;
using Content.Server.ShrinkRay;
using Content.Server.Research.Disk;
using Content.Server.Bible.Components;
using Content.Server.Mail.Components;
using Content.Server.Forensics;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Fluids.Components;
using Content.Server.Armor;
using Content.Shared.Item;
using Content.Shared.Clothing;
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

            if (TryComp<ItemComponent>(item, out var itemComp))
                scribeComponent.SpeechQueue.Enqueue(AssembleReport(itemComp));

            if (TryComp<ClothingSpeedModifierComponent>(item, out var clothingMvMod))
                scribeComponent.SpeechQueue.Enqueue(AssembleReport(clothingMvMod));

            if (TryComp<MeleeChemicalInjectorComponent>(item, out var injector))
                scribeComponent.SpeechQueue.Enqueue(AssembleReport(injector));

            if (TryComp<ArmorComponent>(item, out var armor))
                scribeComponent.SpeechQueue.Enqueue(AssembleReport(armor));

            if (TryComp<FoodComponent>(item, out var food))
                scribeComponent.SpeechQueue.Enqueue(AssembleReport(food));

            if (TryComp<ProduceComponent>(item, out var produce))
                scribeComponent.SpeechQueue.Enqueue(AssembleReport(produce));

            if (TryComp<ServerStorageComponent>(item, out var storage))
                scribeComponent.SpeechQueue.Enqueue(AssembleReport(storage));

            if (TryComp<OfHoldingComponent>(item, out var ofHolding))
                scribeComponent.SpeechQueue.Enqueue(AssembleReport(ofHolding));

            if (TryComp<QuantumSpinInverterComponent>(item, out var qsi))
                scribeComponent.SpeechQueue.Enqueue(AssembleReport(qsi));

            if (TryComp<ShrinkRayComponent>(item, out var shrinkRay))
                scribeComponent.SpeechQueue.Enqueue(AssembleReport(shrinkRay));

            if (TryComp<ResearchDiskComponent>(item, out var disk))
                scribeComponent.SpeechQueue.Enqueue(AssembleReport(disk));

            if (TryComp<BibleComponent>(item, out var bible))
                scribeComponent.SpeechQueue.Enqueue(AssembleReport(bible));

            if (TryComp<SummonableComponent>(item, out var summonable))
                scribeComponent.SpeechQueue.Enqueue(AssembleReport(summonable));

            if (TryComp<MailComponent>(item, out var mail))
                scribeComponent.SpeechQueue.Enqueue(AssembleReport(mail));

            if (TryComp<ForensicScannerComponent>(item, out var fscanner))
                scribeComponent.SpeechQueue.Enqueue(AssembleReport(fscanner));

            if (TryComp<ForensicPadComponent>(item, out var fpad))
                scribeComponent.SpeechQueue.Enqueue(AssembleReport(fpad));

            if (TryComp<SolutionContainerManagerComponent>(item, out var solutionCntr))
                scribeComponent.SpeechQueue.Enqueue(AssembleReport(solutionCntr));
        }

        private string AssembleReport(MeleeChemicalInjectorComponent injector)
        {
            var report = "It can inject chemicals into other things it hits in melee. ";
            report += ("It injects " + injector.TransferAmount.ToString() + "u per hit with an effeciency of " + (injector.TransferEfficiency * 100) + "%. ");
            return report;
        }

        private string AssembleReport(ArmorComponent armor)
        {
            var report = "It provides damage protection to its wearer. ";

            foreach (var coefficient in armor.Modifiers.Coefficients)
            {
                var reduction = Math.Round((1f - coefficient.Value) * 100);
                report += (coefficient.Key + " damage is reduced by " + reduction + "%. ");
            }
            foreach (var flatReduction in armor.Modifiers.FlatReduction)
            {
                report += (flatReduction.Key + " damage is reduced by a flat value of " + flatReduction.Value + " points. ");
            }

            return report;
        }

        private string AssembleReport(ItemComponent item)
        {
            string report = ("It has a size of " + item.Size.ToString() + ". ");

            // var slot = item.SlotFlags.ToString().ToLower();

            // if (slot != "preventequip")
            //     if (slot != "outerclothing")
            //         report += "It can be equipped on the " + slot + ". ";
            //     else
            //         report += "It can be worn over other clothes. ";

            return report;
        }

        private string AssembleReport(ClothingSpeedModifierComponent clothingMv)
        {
            string report = "It may affect your movement speed when worn. ";
            var walkMod = Math.Round((1f -clothingMv.WalkModifier) * 100);
            var sprintMod = Math.Round((1f -clothingMv.SprintModifier) * 100);
            if (walkMod != 0)
                report += "It will slow down walking speed by " + walkMod + "%. ";
            if (sprintMod != 0)
                report += "It will slow down running speed by " + sprintMod + "%. ";

            return report;
        }

        private string AssembleReport(FoodComponent food)
        {
            string report = ("It's edible. ");

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

        private string AssembleReport(ServerStorageComponent storage)
        {
            string report = ("It can store items with a combined size of " + storage.StorageCapacityMax + ". ");

            if (storage.Whitelist != null)
                report += "Only certain kinds of items can be stored inside. ";

            if (storage.Blacklist != null)
                report += "Certain kinds of items cannot be stored inside. ";

            return report;
        }

        private string AssembleReport(OfHoldingComponent ofHolding)
        {
            string report = ("It has a bluespace pocket dimension inside. Putting another pocket dimension inside will create one of the densest objects known to the stars. ");
            return report;
        }

        private string AssembleReport(QuantumSpinInverterComponent qsi)
        {
            string report = "It can be bonded with another one of its kind. After it is bonded, activating it at any time will teleport its user to the location of its partner, using it up in the process. ";
            return report;
        }

        private string AssembleReport(ShrinkRayComponent shrinkRay)
        {
            var scale = Math.Round(((shrinkRay.ScaleFactor.X + shrinkRay.ScaleFactor.Y) / 2) * 100);
            string report = ("It's able to scale its target to " + scale + "% of that target's original size. ");
            return report;
        }

        private string AssembleReport(ResearchDiskComponent disk)
        {
            string report = ("If inserted into the research server, it will provide the server with an additional " + disk.Points + " research points. ");
            return report;
        }

        private string AssembleReport(BibleComponent bible)
        {
            string report = "People with the right religious training can wield this to perform miracles. ";
            report += ("Attempting to use it without religious training will deal a total of " + bible.DamageOnUntrainedUse.Total + " damage. ");
            if (bible.Damage.Total <= 0)
                report += ("Otherwise, it will heal its target for " + Math.Abs((float) bible.Damage.Total) + " total damage. ");
            else
                report += ("Otherwise, it will damage its target for " + bible.Damage.Total + " total damage. ");

            if (bible.FailChance > 0)
            {
                report += ("If the target is not a familiar and is not wearing anything on their head, there is a " + (bible.FailChance * 100) + "% chance to instead deal " + bible.DamageOnFail.Total + " total damage. ");
            }

            return report;
        }

        private string AssembleReport(SummonableComponent summonable)
        {
            var report = "It has special summoning capabilities. ";

            if (summonable.RequiresBibleUser)
                report += "Its user requires religious training to use them. ";

            if (summonable.SpecialItemPrototype != null &&_prototypeManager.TryIndex<EntityPrototype>(summonable.SpecialItemPrototype, out var summoned))
            {
                report += "Specifically, it can summon " + summoned.Name + ". ";
            }

            return report;
        }

        private string AssembleReport(MailComponent mail)
        {
            var report = "It can be unlocked by its intended recipient. It checks that the name, job, and accesses match. ";

            if (mail.Locked)
                report += "When unlocked by its recipient, cargo will receive " + mail.Bounty + " points. ";

            return report;
        }

        private string AssembleReport(ForensicScannerComponent component)
        {
            var report = "It can be used to scan for traces of fingerprints and glove fibres. Using a forensic pad on it will quickly check against the last scanned item. ";
            return report;
        }

        private string AssembleReport(ForensicPadComponent component)
        {
            var report = "It can be used once to collect a fingerprint sample from someone's hands, or a fibre sample from gloves. It can be used on a forensic scanner to quickly check its sample against the last thing that scanner scanned. ";
            return report;
        }

        private string AssembleReport(SolutionContainerManagerComponent solutionCntr)
        {
            var report = "It can hold liquid reagents. ";
            if (solutionCntr.Solutions.Count == 1)
            {
                report += "It only has one solution. ";
            }
            else
            {
                report += "It has " + solutionCntr.Solutions.Count + " different solutions. ";
            }

            if (HasComp<RefillableSolutionComponent>(solutionCntr.Owner))
                report += "It can be refilled by hand with more reagents. ";

            if (HasComp<DrawableSolutionComponent>(solutionCntr.Owner))
                report += "It can be drawn from using a syringe. ";

            if (HasComp<InjectableSolutionComponent>(solutionCntr.Owner))
                report += "It can be injected into using a syringe. ";

            if (HasComp<SolutionTransferComponent>(solutionCntr.Owner))
                report += "It can be poured by hand into another solution container. ";

            if (HasComp<SpillableComponent>(solutionCntr.Owner))
                report += "It can be spilled out onto the ground. ";

            if (HasComp<DrainableSolutionComponent>(solutionCntr.Owner))
                report += "It can be easily drained out into another container. ";

            if (HasComp<DrinkComponent>(solutionCntr.Owner))
                report += "It can be drank. ";

            return report;
        }
    }
}
