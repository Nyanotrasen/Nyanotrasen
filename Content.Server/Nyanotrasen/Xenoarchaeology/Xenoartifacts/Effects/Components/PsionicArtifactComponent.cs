namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
/// <summary>
///     Makes a couple people psionic.
/// </summary>
[RegisterComponent]
public sealed class PsionicArtifactComponent : Component
{

    [DataField("charges")]
    public int Charges = 1;

    /// <summary>
    /// How far away it will check for people
    /// If empty, picks a random one from its list
    /// </summary>
    [DataField("range")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Range = 5f;
}
