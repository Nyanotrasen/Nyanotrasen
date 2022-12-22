using Robust.Shared.Serialization;

namespace Content.Shared.Forensics
{
    [Serializable, NetSerializable]
    public enum ForensicPadVisuals : byte
    {
        IsUsed,
    }

    [Serializable, NetSerializable]
    public enum ForensicPadVisualLayers : byte
    {
        Prints,
    }
}
