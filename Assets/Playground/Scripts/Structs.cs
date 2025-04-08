using static Const;

public static class Util
{
    public static uint RandomDice(uint max) => (uint)UnityEngine.Random.Range(0, (int)max);
}

public static class Const
{
    public const uint TICKS_PER_SECOND = 32U;
    public const uint TICKS_PER_MINUTE = TICKS_PER_SECOND * 60U;
    public const uint METERS_PER_UNIT = 10U;
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

[System.Serializable] public struct UnitRange
{
    public uint Range;

    public static implicit operator UnitRange(Meter meters) => new() { Range = meters.Meters / METERS_PER_UNIT };
}

[System.Serializable] public struct TickCooldown
{
    public uint Ticks;

    public static implicit operator TickCooldown(PerMinute perMinute) => new() { Ticks = TICKS_PER_MINUTE / perMinute.TimesPerMinute };
    public static implicit operator TickCooldown(PerSecond perSecond) => new() { Ticks = TICKS_PER_SECOND / perSecond.TimesPerSecond };
}