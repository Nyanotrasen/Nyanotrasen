namespace Content.Shared.Abilities.Psionics
{
    public sealed class PsionicAbilitiesSystem : EntitySystem
    {
        public void AddPsionics(EntityUid uid)
        {
            if (HasComp<PsionicComponent>(uid))
                return;

            AddComp<PsionicComponent>(uid);
            AddComp<PacificationPowerComponent>(uid);
        }
    }
}