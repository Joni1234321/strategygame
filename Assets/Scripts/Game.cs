using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using static Util;

[ExecuteAlways]
public sealed class Game : MonoBehaviour
{
    // Game State
    public List<GameUnit> Units = new();
    public List<GameUnit> Enemies = new();
    [FormerlySerializedAs("Gameobjects")] public List<GameObject> GameObjects = new();
    public PositionUnit Spawn1 = new() { Units = new int2(1, -1) };

    // Game Defines
    public Color TeamColor = Color.blue;
    public Color EnemyColor = Color.red;
    public GameObject UnitPrefab;
    public List<GameUnit.Definition> Defines = new(Enum.GetNames(typeof(UnitName)).Length)
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

    private void OnEnable()
    {
        Spawn();
    }

    private void Update()
    {
        
        Tick();
    }

    private void UserInput()
    {
        
    }

    [ContextMenu("Spawn map")]
    private void Spawn()
    {
        Units.Clear();
        Enemies.Clear();

        foreach (GameObject go in GameObjects) DestroyImmediate(go);
        GameObjects.Clear();
        
        CreateUnit(UnitName.UnitInfantry, Spawn1, Units, TeamColor);
        CreateUnit(UnitName.UnitMortar, new PositionUnit { Units = new int2(2, 1) }, Units, TeamColor);

        CreateUnit(UnitName.UnitInfantry, new PositionUnit { Units = new int2(4, 1) }, Enemies, EnemyColor);
    }

    private void Tick()
    {
        for (int i = 0; i < Units.Count; i++)
        {
            GameUnit unit = Units[i];
            GameObject obj = GameObjects[i];

            UpdateUnit(unit);

            obj.transform.position = unit.Position.WorldPosition;
        }
    }
    private void CreateUnit(UnitName unitName, PositionUnit position, [NotNull] List<GameUnit> team, Color color)
    {
        GameUnit.Definition definition = Defines.Find(x => x.UnitName == unitName);
        team.Add(new GameUnit
        {
            ShootingCooldown = { Ticks = 0U },
            HealthLeft = definition.UnitStats.Health,
            Position = position
        });
        GameObject go = Instantiate(UnitPrefab, position.WorldPosition, Quaternion.identity, transform);
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = definition.Image;
        sr.color = color;
        GameObjects.Add(go);
    }
    private void UpdateUnit(GameUnit unit)
    {
        GameUnit.Stat stat = Defines[(int)unit.UnitName].UnitStats;

        // AI
        switch (unit.UnitAction)
        {
            case UnitAction.UnitAlert:
                Entity enemy = GetNearbyUnit(unit.Position, stat.RangeUnitsSquared, Enemies);
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
                if (!unit.TargetUnit.HasValue() || !TargetInRange(unit.Position, Enemies[unit.TargetUnit.Index].Position, stat.RangeUnitsSquared))
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
                    Shoot(unit, Enemies, unit.TargetUnit);
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