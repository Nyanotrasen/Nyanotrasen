using Robust.Shared.Serialization;

namespace Content.Shared.CharacterAppearance
{
    [Serializable, NetSerializable]
    public enum HumanoidVisualLayers : byte
    {
        TailBehind,
        Hair,
        FacialHair,
        Chest,
        Head,
        Snout,
        Frills,
        Horns,
        EarsInner,
        EarsOuter,
        Eyes,
        RArm,
        LArm,
        RHand,
        LHand,
        RLeg,
        LLeg,
        RFoot,
        LFoot,
        TailFront,
        FelinidTailFront,
        Handcuffs,
        StencilMask,
        Fire,
    }
}
