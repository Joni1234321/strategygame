using UnityEngine;

[System.Serializable] public class TickController : MonoBehaviour
{
    public enum TickStatus
    {
        DoTick,
        DontDoTick,
    }

    [field: SerializeField] public float TimeSinceLastTick { get; private set; }

    public TickStatus TestTick()
    {
        if (TimeSinceLastTick >= Const.SECONDS_PER_TICK)
        {
            TimeSinceLastTick = 0.0F;
            return TickStatus.DoTick;
        }

        TimeSinceLastTick += Time.deltaTime;
        return TickStatus.DontDoTick;
    }
}