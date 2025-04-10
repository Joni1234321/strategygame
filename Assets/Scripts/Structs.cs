using UnityEngine;
using Unity.Mathematics;
using static Const;

public static class Util
{
    public static uint RandomDice(uint max) => (uint)UnityEngine.Random.Range(0, (int)max);
}

public static class Const
{
    public const uint UNITS_PER_UNITY_UNIT = 10U;
    public const uint TICKS_PER_SECOND = 16U;
    public const uint METERS_PER_UNIT = 10U;

    public const float UNITY_UNITS_PER_UNIT = 1.0F / UNITS_PER_UNITY_UNIT;
    public const float WORLD_COORD_Z = 0.0F;

    public const uint TICKS_PER_MINUTE = TICKS_PER_SECOND * 60U;
    public const float SECONDS_PER_TICK = 1.0F / TICKS_PER_SECOND;

    private const uint BULLET_SPEED_METER_REALISTIC = 400U;
    private const uint BULLET_SPEED_METER_LOW = 10U;
    private const float BULLET_SPEED_UNITS_PER_SECOND = (float)BULLET_SPEED_METER_LOW / METERS_PER_UNIT;
    public const float BULLET_SPEED_UNITS_PER_TICK = SECONDS_PER_TICK * BULLET_SPEED_UNITS_PER_SECOND;
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
    public PositionUnit(Vector2 worldPosition) => Units = new int2((int)(worldPosition.x * UNITS_PER_UNITY_UNIT), (int)(worldPosition.y * UNITS_PER_UNITY_UNIT));

    public Vector3 WorldPosition => new(Units.x * UNITY_UNITS_PER_UNIT, Units.y * UNITY_UNITS_PER_UNIT, WORLD_COORD_Z);
}