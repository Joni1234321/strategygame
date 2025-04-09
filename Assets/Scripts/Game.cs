using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using static Util;

[ExecuteAlways] public sealed class Game : MonoBehaviour
{
    // Controllers
    PlayerController playerController;
    
    // Game State
    public List<MilitaryNode> Friendlies = new(), Enemies = new();
    public List<MilitaryScript> FriendliesScripts = new(), EnemiesScripts = new();
    public PositionUnit Spawn1 = new() { Units = new int2(1, -1) };

    // Game Defines
    public GameObject UnitPrefab;
    public List<MilitaryNode.Definition> Defines = new(Enum.GetNames(typeof(MilitaryNodeType)).Length)
    {
        new MilitaryNode.Definition
        {
            MilitaryNodeType = MilitaryNodeType.UnitInfantry,
            Image = null,
            UnitStats = new MilitaryNode.Stat
            {
                Health = 10U,
                RangeUnitsSquared = new Meter { Meters = 450U },
                TicksBetweenShots = new PerSecond { TimesPerSecond = 1U },
                ProjectileType = ProjectileType.ProjectileDirect,
            }
        },
        new MilitaryNode.Definition
        {
            MilitaryNodeType = MilitaryNodeType.UnitMortar,
            Image = null,
            UnitStats = new MilitaryNode.Stat
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
        playerController = GetComponent<PlayerController>();
        Spawn();
    }
    private void Update()
    {
        UserInput();
        Tick();
    }

    // Methods
    private void UserInput()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame && playerController.SelectedUnit.HasValue())
        {
            Vector2 worldPos = playerController.MainCamera.ScreenToWorldPoint(Input.mousePosition);
            MilitaryNode militaryNode = GetMilitaryUnit(playerController.SelectedUnit);
            militaryNode.TargetPosition = new(worldPos); 
            SetMilitaryNode(playerController.SelectedUnit, militaryNode);
            Debug.Log($"[USER] Moving to {worldPos}");
        }

    }
    [ContextMenu("Spawn map")]
    private void Spawn()
    {
        Debug.Log($"[GAME] {nameof(Spawn)}");
        Friendlies.Clear();
        Enemies.Clear();

        while (transform.childCount > 0) transform.GetChild(0).gameObject.DestroyEither();
        FriendliesScripts.Clear();
        EnemiesScripts.Clear();

        CreateUnit(MilitaryNodeType.UnitInfantry, Spawn1, Team.BlueTeam);
        CreateUnit(MilitaryNodeType.UnitMortar, new PositionUnit { Units = new int2(2, 1) }, Team.BlueTeam);

        CreateUnit(MilitaryNodeType.UnitInfantry, new PositionUnit { Units = new int2(4, 1) }, Team.RedTeam);
    }
    private void Tick()
    {
        for (int i = 0; i < Friendlies.Count; i++)
        {
            MilitaryNode militaryNode = Friendlies[i];
            MilitaryScript obj = FriendliesScripts[i];

            SetMilitaryNode(obj.Entity, UpdateUnit(militaryNode));

            obj.transform.position = militaryNode.Position.WorldPosition;
        }
    }
    private MilitaryNode UpdateUnit(MilitaryNode militaryNode)
    {
        MilitaryNode.Stat stat = Defines[(int)militaryNode.MilitaryNodeType].UnitStats;
        Debug.Log($"[MILITARY] B {militaryNode.UnitAction}");

        // AI
        switch (militaryNode.UnitAction)
        {
            case UnitAction.UnitAlert:
                Entity enemy = GetNearbyUnit(Team.RedTeam, militaryNode.Position, stat.RangeUnitsSquared);
                if (enemy.HasValue())
                {
                    militaryNode.TargetUnit = enemy;
                    militaryNode.UnitAction = UnitAction.UnitFighting;
                }
                else if (militaryNode.TargetPosition.HasValue)
                {
                    militaryNode.UnitAction = UnitAction.UnitMoving;
                }

                break;
            case UnitAction.UnitMoving:
                if (militaryNode.TargetPosition == null || ReachedTarget(militaryNode.Position, militaryNode.TargetPosition.Value)) militaryNode.UnitAction = UnitAction.UnitAlert;

                break;
            case UnitAction.UnitFighting:
                if (!militaryNode.TargetUnit.HasValue() || !TargetInRange(militaryNode.Position, Enemies[militaryNode.TargetUnit.Index].Position, stat.RangeUnitsSquared))
                {
                    militaryNode.TargetUnit.Reset();
                    militaryNode.UnitAction = UnitAction.UnitAlert;
                    break;
                }

                if (militaryNode.ShootingCooldown.Status is TickCooldown.CooldownStatus.CooldownWaiting)
                {
                    militaryNode.ShootingCooldown.Ticks--;
                }
                else
                {
                    ShootAt(militaryNode.TargetUnit);
                    militaryNode.ShootingCooldown = stat.TicksBetweenShots;
                }

                break;
            case UnitAction.UnitMovingAndFighting:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(militaryNode), militaryNode, null);
        }

        
        Debug.Log($"[MILITARY] A {militaryNode.UnitAction}");
        return militaryNode;

        Entity GetNearbyUnit(Team team, PositionUnit from, RangeUnitsSquared within)
        {
            int index = GetTeam(team).FindIndex(unit => math.lengthsq(unit.Position.Units - from.Units) < within.DistanceSquared);
            return index == -1 ? Entity.Null : GetEntity(team, index);
        }
        void ShootAt(Entity enemyEntity)
        {
            const uint CHANCE_TO_HIT = 10U;
            const uint CHANCE_TO_DODGE = 10U;
            if (RandomDice(CHANCE_TO_HIT) == 0U)
            {
                MilitaryNode enemy = GetMilitaryUnit(enemyEntity);
                if (RandomDice(CHANCE_TO_DODGE) < enemy.HealthLeft)
                {
                    const uint DAMAGE = 1;
                    enemy.HealthLeft -= math.min(enemy.HealthLeft, DAMAGE);
                    SetMilitaryNode(enemyEntity, enemy);
                    EnemiesScripts[enemyEntity.Index].SetHealth(enemy.HealthLeft);

                    Debug.Log($"[MILITARY] {militaryNode} hit {enemy}. Health left: {enemy.HealthLeft}");
                }
            }
        }

        
        static bool ReachedTarget(PositionUnit position, PositionUnit target) => math.all(position.Units == target.Units);
        static bool TargetInRange(PositionUnit position, PositionUnit target, RangeUnitsSquared range) => math.lengthsq(target.Units - position.Units) < range.DistanceSquared;
    }
    
    // Support
    private void CreateUnit(MilitaryNodeType militaryNodeType, PositionUnit position, Team team)
    {
        Debug.Log($"[GAME] {nameof(CreateUnit)} {militaryNodeType} {team}");
        MilitaryNode.Definition definition = Defines.Find(x => x.MilitaryNodeType == militaryNodeType);
        List<MilitaryNode> units = GetTeam(team);
        units.Add(new MilitaryNode
        {
            MilitaryNodeType = militaryNodeType,
            ShootingCooldown = { Ticks = 0U },
            HealthLeft = definition.UnitStats.Health,
            TargetPosition = null,
            TargetUnit = Entity.Null,
            UnitAction = UnitAction.UnitAlert,
            Position = position,
        });

        GameObject go = Instantiate(UnitPrefab, position.WorldPosition, Quaternion.identity, transform);
        MilitaryScript militaryScript = go.GetComponent<MilitaryScript>();
        militaryScript.SetUnit(definition.Image, team.ToColor());
        militaryScript.SetHealth(definition.UnitStats.Health);
        militaryScript.SetStatusColor(UnitAction.UnitAlert.ToColor());
        militaryScript.Entity = GetEntity(team, units.Count - 1);
        militaryScript.SetUnitName(team, militaryNodeType);
        GetTeamScripts(team).Add(militaryScript);
    }
    private List<MilitaryNode> GetTeam(Team team) => team switch
    {
        Team.BlueTeam => Friendlies,
        Team.RedTeam => Enemies,
        _ => throw new ArgumentOutOfRangeException(nameof(team), team, null)
    };
    private List<MilitaryScript> GetTeamScripts(Team team) => team switch
    {
        Team.BlueTeam => FriendliesScripts,
        Team.RedTeam => EnemiesScripts,
        _ => throw new ArgumentOutOfRangeException(nameof(team), team, null)
    };
    private MilitaryNode GetMilitaryUnit(Entity entity) => GetTeam((Team)entity.Version)[entity.Index];
    private void SetMilitaryNode(Entity entity, in MilitaryNode military) => GetTeam((Team)entity.Version)[entity.Index] = military;
    private static Entity GetEntity(Team team, int index) => new() { Index = index, Version = (int)team };
}

public enum Team
{
    BlueTeam = 1,
    RedTeam = 2,
}

public enum ProjectileType
{
    ProjectileDirect,
    ProjectileIndirect
}

public enum MilitaryNodeType
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


[Serializable] public struct MilitaryNode
{
    [FormerlySerializedAs("MilitaryUnitType")] [FormerlySerializedAs("UnitName")] public MilitaryNodeType MilitaryNodeType;

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
        [FormerlySerializedAs("MilitaryUnitType")] [FormerlySerializedAs("UnitName")] public MilitaryNodeType MilitaryNodeType;
        public Sprite Image;
        public Stat UnitStats;
    }
}