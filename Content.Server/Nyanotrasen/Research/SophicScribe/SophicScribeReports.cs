using Content.Server.Weapon.Melee.Components;
using Content.Server.Damage.Components;
using Content.Server.Wieldable.Components;
using Content.Shared.Damage;

namespace Content.Server.Research.SophicScribe
{
    public sealed partial class SophicScribeSystem
    {
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
    }
}
