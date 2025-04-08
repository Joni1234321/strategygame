using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static Util;

public sealed class Game : MonoBehaviour
{
    public List<GameUnit> units = new();
    public List<GameUnit.Definition> defines = new()
    {
        new GameUnit.Definition
        {
            UnitName = UnitName.UnitInfantry,
            Image = null,
            UnitStats = new GameUnit.Stat
            {
                Health = 10U,
                Range = new Meter { Meters = 450U },
                TicksBetweenShots = new PerSecond { TimesPerSecond = 1U },
                ProjectileType = ProjectileType.ProjectileDirect,
            }
        },
        new GameUnit.Definition
        {
            UnitName = UnitName.UnitMortar,
            Image = null,
            UnitStats = new GameUnit.Stat
            {
                Health = 1U,
                Range = new Meter { Meters = 700U },
                TicksBetweenShots = new PerMinute { TimesPerMinute = 2U },
                ProjectileType = ProjectileType.ProjectileIndirect,
            }
        },
    };

    private void Awake()
    {
    }

    private uint i = 0;
    private void Update()
    {
        var pers = new PerSecond { TimesPerSecond = i++ };
        var perm = new PerMinute { TimesPerMinute = i++ };
        TickCooldown t = pers;
        TickCooldown t2 = perm;
        Debug.Log($"[TEST] {pers.TimesPerSecond}hz = {t.Ticks} | {perm.TimesPerMinute}hz.m {t2.Ticks}");

        Tick();
    }

    void Tick()
    {
        foreach (var unit in units)
        {
            UpdateUnit(unit);
        }
    }
    public void BuyUnit(UnitName unitName, float2 position)
    {
        GameUnit.Definition definition = defines.Find(x => x.UnitName == unitName);
        units.Add(new GameUnit
        {
            ShootingCounter = 0U,
            Health = definition.UnitStats.Health,
            Position = position
        });
    }

    void UpdateUnit(GameUnit unit)
    {
        // Shooting
        if (unit.ShootingCounter > 0U)
        {
            unit.ShootingCounter--;
        }
        else
        {
            GameUnit? target = null;
            if (target != null)
            {
                Shoot(unit, target.Value);
            }
        }

        return;

        void Shoot(GameUnit unit, GameUnit target)
        {
            const uint CHANCE_TO_HIT = 10U;
            const uint CHANCE_TO_DODGE = 10U;
            if (RandomDice(CHANCE_TO_HIT) == 0U)
            {
                if (RandomDice(CHANCE_TO_DODGE) < target.Health)
                {
                    target.Health--;
                }
            }
        }
    }
}

public enum ProjectileType
{
    ProjectileDirect,
    ProjectileIndirect
}

public enum UnitName
{
    UnitInfantry,
    UnitMortar
}

[Serializable] public struct GameUnit
{
    public float2 Position;
    public uint Health;
    public uint ShootingCounter;

    [Serializable] public struct Stat
    {
        public uint Health;

        public TickCooldown TicksBetweenShots;
        public UnitRange Range;
        public ProjectileType ProjectileType;
    }

    [Serializable] public struct Definition
    {
        public UnitName UnitName;
        public Texture2D Image;
        public Stat UnitStats;
    }
}