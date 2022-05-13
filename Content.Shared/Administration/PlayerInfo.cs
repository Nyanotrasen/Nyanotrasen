﻿using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration
{
    [Serializable, NetSerializable]
    public record PlayerInfo(string Username, string CharacterName, string StartingJob, bool Antag, EntityUid EntityUid, NetUserId SessionId, bool Connected);
}
