using Robust.Shared.Serialization;
using Content.Shared.DoAfter;

namespace Content.Shared.Item.PseudoItem
{
    [Serializable, NetSerializable]
    public sealed class PseudoItemInsertDoAfterEvent : SimpleDoAfterEvent
    {
    }
}
