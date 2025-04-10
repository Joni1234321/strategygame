using System;
using UnityEngine;
using Unity.Mathematics;
using static Const;

public static class Util
{
    public static uint RandomDice(uint max) => (uint)UnityEngine.Random.Range(0, (int)max);
}

public static class Const
{
    public const float WORLD_COORD_Z = 0.0F;

    private const bool FAST = true;
    public const uint TICKS_PER_SECOND = FAST ? 64U : 16U;

    public const uint METERS_PER_DISTANCE = 1U;
    public const uint DISTANCES_PER_UNITY_UNIT = 10U;
    public const uint METERS_PER_UNITY_UNIT = METERS_PER_DISTANCE * DISTANCES_PER_UNITY_UNIT;

    public const float UNITY_UNITS_PER_DISTANCE = 1.0F / DISTANCES_PER_UNITY_UNIT;
    public const float DISTANCES_PER_METER = 1.0F / METERS_PER_DISTANCE;


    public const uint TICKS_PER_MINUTE = TICKS_PER_SECOND * 60U;
    public const float SECONDS_PER_TICK = 1.0F / TICKS_PER_SECOND;

    public const float METERS_PER_SECOND_TO_DISTANCES_PER_TICK = DISTANCES_PER_METER / TICKS_PER_SECOND;
}

public struct PerMinute
{
    public uint TimesPerMinute;
}

public struct PerSecond
{
    public uint TimesPerSecond;
}

public struct MetersPerSecond
{
    public uint MetersPerSecondValue;
}

public struct Meter
{
    public uint Meters;
}

[Serializable] public struct Ammunition
{
    public uint Ammo;
}
[Serializable] public struct Magazines
{
    public uint Mags;
}

[Serializable] public struct RangeUnitsSquared
{
    public uint DistanceSquared;

    public static implicit operator RangeUnitsSquared(Meter meters) => new() { DistanceSquared = math.square(meters.Meters / METERS_PER_DISTANCE) };
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

[System.Serializable] public struct Velocity
{
    public float DistancePerTick;
    
    public static implicit operator Velocity(MetersPerSecond metersPerSecond) => new() { DistancePerTick = metersPerSecond.MetersPerSecondValue * METERS_PER_SECOND_TO_DISTANCES_PER_TICK };
}
 
public struct UnityPosition
{
    public float2 WorldPosition;
    public UnityPosition(float2 worldPosition) => WorldPosition = worldPosition;
    public UnityPosition(float x, float y) => WorldPosition = new float2(x, y);
    
    public static implicit operator Vector3(UnityPosition worldPosition) => new(worldPosition.WorldPosition.x, worldPosition.WorldPosition.y, WORLD_COORD_Z);
}
public struct Position
{
    public int2 GamePosition { get; private set; }

    public Position(int2 gamePosition) => GamePosition = gamePosition;
    public Position(int x, int y) => GamePosition = new int2(x, y);
    public Position(UnityPosition worldPosition) => GamePosition = new int2(worldPosition.WorldPosition * DISTANCES_PER_UNITY_UNIT);

    public UnityPosition WorldPosition => new(new float2(GamePosition) * UNITY_UNITS_PER_DISTANCE);
}