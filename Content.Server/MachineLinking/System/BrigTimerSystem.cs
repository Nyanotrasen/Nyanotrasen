using Content.Shared.Doors.Components;
using Content.Shared.MachineLinking;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.MachineLinking;

public sealed class BrigTimerSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private readonly Dictionary<EntityUid, float> _timers = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (uid, length) in _timers)
        {
            if (length <= 0)
            {
                var entity = _entityManager.GetEntity(uid);
                if (entity.TryGetComponent(out BrigTimerComponent brigTimer) &&
                    EntityManager.TryGetComponent(brgTimer.Door, out DoorComponent door))
                {
                    door.State = DoorState.Open;
                }

                _timers.Remove(uid);
            }
            else
            {
                _timers[uid] -= frameTime;
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
        var entity = _entityManager.GetEntity(uid);
        if (entity.TryGetComponent(out BrigTimerComponent brigTimer))
        {
            StartTimer(uid, brigTimer.Length);
        }
    }
}
