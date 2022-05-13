﻿namespace Content.Server.Electrocution
{
    /// <summary>
    /// Component for virtual electrocution entities (representing an in-progress shock).
    /// </summary>
    [RegisterComponent]
    [Friend(typeof(ElectrocutionSystem))]
    public sealed class ElectrocutionComponent : Component
    {
        [DataField("timeLeft")] public float TimeLeft { get; set; }
        [DataField("electrocuting")] public EntityUid Electrocuting { get; set; }
        [DataField("accumDamage")] public float AccumulatedDamage { get; set; }

    }
}
