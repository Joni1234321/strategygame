using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static Util;

[ExecuteAlways] public sealed class Game : MonoBehaviour
{
    // Game State
    public List<GameUnit> Units = new();
    public List<GameUnit> Enemies = new();
    public List<UnitScript> UnitScripts = new();
    public PositionUnit Spawn1 = new() { Units = new int2(1, -1) };

    // Game Defines
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
    
    // Base
    private void OnEnable()
    {
        Spawn();
    }
    private void Update()
    {
        Tick();
    }

    // Methods
    private void UserInput()
    {
    }
    [ContextMenu("Spawn map")]
    private void Spawn()
    {
        Debug.Log($"[GAME] {nameof(Spawn)}");
        Units.Clear();
        Enemies.Clear();

        while (transform.childCount > 0) transform.GetChild(0).gameObject.DestroyEither();
        UnitScripts.Clear();

        CreateUnit(UnitName.UnitInfantry, Spawn1, Team.BlueTeam);
        CreateUnit(UnitName.UnitMortar, new PositionUnit { Units = new int2(2, 1) }, Team.BlueTeam);

        CreateUnit(UnitName.UnitInfantry, new PositionUnit { Units = new int2(4, 1) }, Team.RedTeam);
    }
    private void Tick()
    {
        for (int i = 0; i < Units.Count; i++)
        {
            GameUnit unit = Units[i];
            UnitScript obj = UnitScripts[i];

            UpdateUnit(unit);

            obj.transform.position = unit.Position.WorldPosition;
        }
    }
    private void UpdateUnit(GameUnit unit)
    {
        GameUnit.Stat stat = Defines[(int)unit.UnitName].UnitStats;

        // AI
        switch (unit.UnitAction)
        {
            case UnitAction.UnitAlert:
                Entity enemy = GetNearbyUnit(Team.RedTeam, unit.Position, stat.RangeUnitsSquared);
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
                    Shoot(unit, unit.TargetUnit);
                }

                break;
            case UnitAction.UnitMovingAndFighting:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(unit), unit, null);
        }

        return;

        Entity GetNearbyUnit(Team team, PositionUnit from, RangeUnitsSquared within)
        {
            int index = GetTeam(team).FindIndex(unit => math.lengthsq(unit.Position.Units - from.Units) < within.DistanceSquared);
            return index == -1 ? Entity.Null : GetEntity(team, index);
        }
        void Shoot(GameUnit unit, Entity enemyEntity)
        {
            const uint CHANCE_TO_HIT = 10U;
            const uint CHANCE_TO_DODGE = 10U;
            if (RandomDice(CHANCE_TO_HIT) == 0U)
            {
                Team team = (Team)enemyEntity.Version;
                List<GameUnit> enemyTeam = GetTeam(team);
                GameUnit enemy = enemyTeam[enemyEntity.Index];
                if (RandomDice(CHANCE_TO_DODGE) < enemy.HealthLeft)
                {
                    enemy.HealthLeft--;
                    enemyTeam[enemyEntity.Index] = enemy;
                }
            }
        }

        
        static bool ReachedTarget(PositionUnit position, PositionUnit target) => math.all(position.Units == target.Units);
        static bool TargetInRange(PositionUnit position, PositionUnit target, RangeUnitsSquared range) => math.lengthsq(target.Units - position.Units) < range.DistanceSquared;
    }
    
    // Support
    private void CreateUnit(UnitName unitName, PositionUnit position, Team team)
    {
        Debug.Log($"[GAME] {nameof(CreateUnit)} {unitName} {team}");
        GameUnit.Definition definition = Defines.Find(x => x.UnitName == unitName);
        List<GameUnit> units = GetTeam(team);
        units.Add(new GameUnit
        {
            UnitName = unitName,
            ShootingCooldown = { Ticks = 0U },
            HealthLeft = definition.UnitStats.Health,
            TargetPosition = null,
            TargetUnit = Entity.Null,
            UnitAction = UnitAction.UnitAlert,
            Position = position,
        });

        GameObject go = Instantiate(UnitPrefab, position.WorldPosition, Quaternion.identity, transform);
        UnitScript unitScript = go.GetComponent<UnitScript>();
        unitScript.SetUnit(definition.Image, team.ToColor());
        unitScript.SetHealth(definition.UnitStats.Health);
        unitScript.SetStatusColor(UnitAction.UnitAlert.ToColor());
        Debug.Log(GetEntity(team, units.Count - 1).Index);
        Debug.Log(GetEntity(team, units.Count - 1));
        unitScript.Entity = GetEntity(team, units.Count - 1);
        UnitScripts.Add(unitScript);
    }
    private List<GameUnit> GetTeam(Team team) => team switch
    {
        Team.BlueTeam => Units,
        Team.RedTeam => Enemies,
        _ => throw new ArgumentOutOfRangeException(nameof(team), team, null)
    };
    private static Entity GetEntity(Team team, int index) => new() { Index = index, Version = (int)team };
}

public enum Team
{
    BlueTeam,
    RedTeam,
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