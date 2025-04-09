using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static Util;

public sealed class Game : MonoBehaviour
{
    public List<GameUnit> units = new();
    public List<GameUnit> enemies = new();
    public GameObject unitPrefab;
    public List<GameObject> gameobjects = new();
    public List<GameUnit.Definition> defines = new(Enum.GetNames(typeof(UnitName)).Length)
    {
        new GameUnit.Definition
        {
            UnitName = UnitName.UnitInfantry,
            Image = null,
            UnitStats = new GameUnit.Stat
            {
                Health = 10U,
                RangeUnitsSquared = new Meter { Meters = 450U },
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
                RangeUnitsSquared = new Meter { Meters = 700U },
                TicksBetweenShots = new PerMinute { TimesPerMinute = 2U },
                ProjectileType = ProjectileType.ProjectileIndirect,
            }
        },
    };

    public PositionUnit Spawn1 = new() { Units = new int2(1, -1) };
    private void Awake()
    {
        CreateUnit(UnitName.UnitInfantry, Spawn1, units);
        CreateUnit(UnitName.UnitMortar, new PositionUnit { Units = new int2(2, 1) }, units);

        CreateUnit(UnitName.UnitInfantry, new PositionUnit { Units = new int2(4, 1) }, enemies);
    }

    private void Update()
    {
        Tick();
    }

    void Tick()
    {
        for (int i = 0; i < units.Count; i++)
        {
            GameUnit unit = units[i];
            GameObject obj = gameobjects[i];

            UpdateUnit(unit);

            obj.transform.position = unit.Position.WorldPosition;
        }
    }
    public void CreateUnit(UnitName unitName, PositionUnit position, [NotNull] List<GameUnit> team)
    {
        GameUnit.Definition definition = defines.Find(x => x.UnitName == unitName);
        team.Add(new GameUnit
        {
            ShootingCooldown = { Ticks = 0U },
            HealthLeft = definition.UnitStats.Health,
            Position = position
        });
        var go = Instantiate(unitPrefab, position.WorldPosition, Quaternion.identity, transform);
        go.GetComponent<SpriteRenderer>().sprite = definition.Image;
        gameobjects.Add(go);
    }

    void UpdateUnit(GameUnit unit)
    {
        GameUnit.Stat stat = defines[(int)unit.UnitName].UnitStats;

        // AI
        switch (unit.UnitAction)
        {
            case UnitAction.UnitAlert:
                Entity enemy = GetNearbyUnit(unit.Position, stat.RangeUnitsSquared, enemies);
                if (enemy.HasValue())
                {
                    unit.TargetUnit = enemy;
                    unit.UnitAction = UnitAction.UnitFighting;
                }
                else if (unit.TargetPosition != null)
                {
                    unit.UnitAction = UnitAction.UnitMoving;
                }

                break;
            case UnitAction.UnitMoving:
                if (unit.TargetPosition == null || ReachedTarget(unit.Position, unit.TargetPosition.Value)) unit.UnitAction = UnitAction.UnitAlert;

                break;
            case UnitAction.UnitFighting:
                if (!unit.TargetUnit.HasValue() || !TargetInRange(unit.Position, enemies[unit.TargetUnit.Index].Position, stat.RangeUnitsSquared))
                {
                    unit.TargetUnit.Reset();
                    unit.UnitAction = UnitAction.UnitAlert;
                    break;
                }

                if (unit.ShootingCooldown.Status is TickCooldown.CooldownStatus.CooldownFinished)
                {
                    unit.ShootingCooldown.Ticks--;
                }
                else
                {
                    Shoot(unit, enemies, unit.TargetUnit);
                }

                break;
            case UnitAction.UnitMovingAndFighting:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(unit), unit, null);
        }

        return;

        static bool ReachedTarget(PositionUnit position, PositionUnit target) => math.all(position.Units == target.Units);
        static bool TargetInRange(PositionUnit position, PositionUnit target, RangeUnitsSquared range) => math.lengthsq(target.Units - position.Units) < range.DistanceSquared;
        static Entity GetNearbyUnit(PositionUnit position, RangeUnitsSquared range, List<GameUnit> enemies)
        {
            int index = enemies.FindIndex(enemy => math.lengthsq(enemy.Position.Units - position.Units) < range.DistanceSquared);
            return index == -1 ? Entity.Null : new Entity { Index = index };
        }
        static void Shoot(GameUnit unit, List<GameUnit> enemies, Entity enemyEntity)
        {
            const uint CHANCE_TO_HIT = 10U;
            const uint CHANCE_TO_DODGE = 10U;
            if (RandomDice(CHANCE_TO_HIT) == 0U)
            {
                GameUnit enemy = enemies[enemyEntity.Index];
                if (RandomDice(CHANCE_TO_DODGE) < enemy.HealthLeft)
                {
                    enemy.HealthLeft--;
                    enemies[enemyEntity.Index] = enemy;
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

public enum UnitAction
{
    UnitAlert,
    UnitMoving,
    UnitFighting,
    UnitMovingAndFighting,
}


[Serializable] public struct GameUnit
{
    public UnitName UnitName;

    public PositionUnit Position;
    public uint HealthLeft;

    public PositionUnit? TargetPosition;
    public Entity TargetUnit;
    public UnitAction UnitAction;
    public TickCooldown ShootingCooldown;

    [Serializable] public struct Stat
    {
        public uint Health;

        public TickCooldown TicksBetweenShots;
        public RangeUnitsSquared RangeUnitsSquared;
        public ProjectileType ProjectileType;
    }

    [Serializable] public struct Definition
    {
        public UnitName UnitName;
        public Sprite Image;
        public Stat UnitStats;
    }
}