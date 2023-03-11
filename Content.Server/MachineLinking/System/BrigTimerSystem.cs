using Content.Shared.Doors.Components;
using Content.Shared.MachineLinking;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.MachineLinking;

public sealed class BrigTimerSystem : EntitySystem
{
    private readonly Dictionary<EntityUid, float> _timers = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (entityUid, length) in _timers)
        {
            if (length <= 0)
            {
                if (EntityManager.TryGetComponent(entityUid, out BrigTimerComponent? brigTimer) &&
                    EntityManager.TryGetComponent(brigTimer.Door, out DoorComponent? door))
                {
                    door.State = DoorState.Open;
                }

                _timers.Remove(entityUid);
            }
            else
            {
                _timers[entityUid] -= frameTime;
            }
        }
    }

    public void StartTimer(EntityUid uid, float length)
    {
        if (!_timers.ContainsKey(uid))
        {
            _timers.Add(uid, length);
        }
    }

    public void UseTimer(EntityUid uid)
    {
        if (EntityManager.TryGetComponent(uid, out BrigTimerComponent? brigTimer))
        {
            StartTimer(uid, brigTimer.Length);
        }
    }
}
