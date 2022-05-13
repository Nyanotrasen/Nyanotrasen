using Content.Server.Nutrition.EntitySystems;

namespace Content.Server.Nutrition.Components
{
    /// <summary>
    ///     A disposable, single-use smokable.
    /// </summary>
    [RegisterComponent, Friend(typeof(SmokingSystem))]
    public sealed class CigarComponent : Component
    {
    }
}
