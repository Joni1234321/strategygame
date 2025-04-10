using UnityEngine;
using Unity.Mathematics;
using static Const;

public static class Util
{
    public static uint RandomDice(uint max) => (uint)UnityEngine.Random.Range(0, (int)max);
}

public static class Const
{
    public const uint TICKS_PER_SECOND = 16U;
    public const uint TICKS_PER_MINUTE = TICKS_PER_SECOND * 60U;
    public const uint METERS_PER_UNIT = 10U;

    public const float WORLD_COORD_Z = 0.0F;
}

public struct PerMinute
{
    public uint TimesPerMinute;
}

public struct PerSecond
{
    public uint TimesPerSecond;
}

public struct Meter
{
    public uint Meters;
}

[System.Serializable] public struct RangeUnitsSquared
{
    public uint DistanceSquared;

    public static implicit operator RangeUnitsSquared(Meter meters) => new() { DistanceSquared = math.square(meters.Meters / METERS_PER_UNIT) };
}

[System.Serializable] public struct CooldownTicks
{
    public uint Ticks;

    public static implicit operator CooldownTicks(PerMinute perMinute) => new() { Ticks = TICKS_PER_MINUTE / perMinute.TimesPerMinute };
    public static implicit operator CooldownTicks(PerSecond perSecond) => new() { Ticks = TICKS_PER_SECOND / perSecond.TimesPerSecond };

    public CooldownStatus Status => Ticks > 0U ? CooldownStatus.CooldownWaiting : CooldownStatus.CooldownFinished;

    public enum CooldownStatus
    {
        CooldownFinished,
        CooldownWaiting
    }
}

public struct MovementSpeedUnits
{
    public uint Units;
}

public struct PositionUnit
{
    public int2 Units;

    public PositionUnit(int x, int y) => Units = new int2(x, y);
    public PositionUnit(Vector2 worldPosition) => Units = new int2((int)worldPosition.x,  (int)worldPosition.y);

    public Vector3 WorldPosition => new(Units.x, Units.y, WORLD_COORD_Z);
}