﻿using Content.Server.Nutrition.EntitySystems;

namespace Content.Server.Nutrition.Components
{
    /// <summary>
    ///     A food item with this component will be forcefully fed to anyone
    /// </summary>
    [RegisterComponent, Friend(typeof(ForcefeedOnCollideSystem))]
    public sealed class ForcefeedOnCollideComponent : Component
    {
        /// <summary>
        ///     Since this component is primarily used by the pneumatic cannon, which adds this comp on throw start
        ///     and wants to remove it on throw end, this is set to false. However, you're free to change it if you want
        ///     something that can -always- be forcefed on collide, or something.
        /// </summary>
        [DataField("removeOnThrowEnd")]
        public bool RemoveOnThrowEnd = true;
    }
}
