using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using static Util;

[RequireComponent(typeof(PlayerController))] [RequireComponent(typeof(TickController))] [ExecuteAlways]
public sealed class Game : MonoBehaviour
{
    // Game Defines
    public Transform BulletList;
    public GameObject UnitPrefab, BulletPrefab;
    public List<MilitaryNode.Definition> Defines;
    public List<MilitaryNode.ImageDefinition> ImageDefinitions;

    // Controllers
    private PlayerController playerController;
    private TickController tickController;

    // Game State
    public List<MilitaryNode> Friendlies = new(), Enemies = new();
    public List<MilitaryScript> FriendliesScripts = new(), EnemiesScripts = new();
    public List<Bullet> Bullets = new();

    // Base
    private void OnEnable()
    {
        playerController = GetComponent<PlayerController>();
        tickController = GetComponent<TickController>();
        Spawn();
    }
    private void Update()
    {
        UserInput();
        if (tickController.TestTick() is TickController.TickStatus.DoTick) Tick();
    }

    // Methods
    private void UserInput()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame && playerController.SelectedUnit.HasValue())
        {
            UnityPosition unityPosition = playerController.GetMousePosition();
            MilitaryNode militaryNode = GetMilitaryNode(playerController.SelectedUnit);
            militaryNode.TargetPosition = new Position(unityPosition);
            SetMilitaryNode(playerController.SelectedUnit, militaryNode);
            Debug.Log($"[USER] Moving to {unityPosition.WorldPosition}");
        }
    }
    [ContextMenu("Reset Defines")]
    private void ResetDefines()
    {
        Defines = new List<MilitaryNode.Definition>(Enum.GetNames(typeof(MilitaryNodeType)).Length)
        {
            new()
            {
                MilitaryNodeType = MilitaryNodeType.UnitInfantry,
                MilitaryNodeStats = new MilitaryNode.Stat
                {
                    Health = 10U,
                    RangeUnitsSquared = new Meters { MetersValue = 500U },
                    TicksBetweenShots = new PerSecond { TimesPerSecond = 5U },
                    TicksBetweenReloads = new Seconds { SecondsValue = 5U },
                    ProjectileVelocity = new MetersPerSecond { MetersPerSecondValue = 880U },
                    ProjectileType = ProjectileType.ProjectileDirect,
                    GunBehaviour = GunBehaviour.GunBurst,
                    MagazineAmmunition = new Ammunition { Ammo = 30U },
                    Magazines = new Magazines { Mags = 20U },
                }
            },
            new()
            {
                MilitaryNodeType = MilitaryNodeType.UnitMortar,
                MilitaryNodeStats = new MilitaryNode.Stat
                {
                    Health = 1U,
                    RangeUnitsSquared = new Meters { MetersValue = 700U },
                    TicksBetweenShots = new PerMinute { TimesPerMinute = 2U },
                    TicksBetweenReloads = new PerMinute { TimesPerMinute = 18U },
                    ProjectileVelocity = new MetersPerSecond { MetersPerSecondValue = 70U },
                    ProjectileType = ProjectileType.ProjectileIndirect,
                    GunBehaviour = GunBehaviour.GunSingle,
                    MagazineAmmunition = new Ammunition { Ammo = 1U },
                    Magazines = new Magazines { Mags = 20U },
                }
            },
        };
    }
    [ContextMenu("Reset State")]
    private void Reset()
    {
        Debug.Log($"[GAME] {nameof(Spawn)}");
        Friendlies.Clear();
        Enemies.Clear();

        while (transform.childCount > 0) transform.GetChild(0).gameObject.UniversalDestroy();
        FriendliesScripts.Clear();
        EnemiesScripts.Clear();

        Bullets.Clear();
    }
    [ContextMenu("Spawn State")]
    private void Spawn()
    {
        Reset();

        BulletList = new GameObject("bullet list").transform;
        BulletList.SetParent(transform);

        CreateUnit(MilitaryNodeType.UnitInfantry, new Position(new UnityPosition(0, 0)), Team.BlueTeam);
        CreateUnit(MilitaryNodeType.UnitMortar, new Position(new UnityPosition(0, 3)), Team.BlueTeam);

        CreateUnit(MilitaryNodeType.UnitInfantry, new Position(new UnityPosition(4, 3)), Team.RedTeam);
    }
    private void Tick()
    {
        for (int i = 0; i < Friendlies.Count; i++)
        {
            MilitaryScript militaryScript = FriendliesScripts[i];
            MilitaryNode newMilitaryNode = UpdateUnit(militaryScript.Entity);
            MilitaryNode.Stat stat = Defines[(int)newMilitaryNode.MilitaryNodeType].MilitaryNodeStats;

            SetMilitaryNode(militaryScript.Entity, newMilitaryNode);

            militaryScript.SetCooldown(newMilitaryNode.GunWaitingState.ToColor(), (float)newMilitaryNode.GunCooldownTicks.Ticks / GetGunCooldown(newMilitaryNode.GunWaitingState, stat).Ticks);
            militaryScript.transform.position = newMilitaryNode.Position.WorldPosition;
        }

        for (int i = Bullets.Count - 1; i >= 0; i--)
        {
            if (Bullets[i].Progress >= 1.0F)
            {
                Bullets[i].Transform.gameObject.UniversalDestroy();
                Bullets.RemoveAt(i);
            }
        }

        for (int i = 0; i < Bullets.Count; i++)
        {
            Bullet bullet = Bullets[i];
            bullet.Tick();
            Bullets[i] = bullet;
        }
    }
    private MilitaryNode UpdateUnit(Entity militaryNodeEntity)
    {
        MilitaryNode militaryNode = GetMilitaryNode(militaryNodeEntity);
        MilitaryNode.Stat defines = Defines[(int)militaryNode.MilitaryNodeType].MilitaryNodeStats;
        Debug.Log($"[MILITARY] {GetMilitaryNodeName(militaryNodeEntity)} B {militaryNode.MilitaryNodeAction}");

        // AI
        switch (militaryNode.MilitaryNodeAction)
        {
            case MilitaryNodeAction.NodeAlert:
                Entity enemy = GetNearbyUnit(Team.RedTeam, militaryNode.Position, defines.RangeUnitsSquared);
                if (enemy.HasValue())
                {
                    militaryNode.TargetUnit = enemy;
                    militaryNode.MilitaryNodeAction = MilitaryNodeAction.NodeFighting;
                }
                else if (militaryNode.TargetPosition.HasValue)
                {
                    militaryNode.MilitaryNodeAction = MilitaryNodeAction.NodeMoving;
                }

                break;
            case MilitaryNodeAction.NodeMoving:
                if (militaryNode.TargetPosition == null || ReachedTarget(militaryNode.Position, militaryNode.TargetPosition.Value))
                    militaryNode.MilitaryNodeAction = MilitaryNodeAction.NodeAlert;

                break;
            case MilitaryNodeAction.NodeFighting:
                if (!militaryNode.TargetUnit.HasValue() || !TargetInRange(militaryNode.Position, Enemies[militaryNode.TargetUnit.Index].Position, defines.RangeUnitsSquared))
                {
                    militaryNode.TargetUnit.Reset();
                    militaryNode.MilitaryNodeAction = MilitaryNodeAction.NodeAlert;
                    break;
                }

                if (militaryNode.GunCooldownTicks.Status is CooldownStatus.CooldownWaiting)
                {
                    militaryNode.GunCooldownTicks.Ticks--;
                }
                else
                {
                    ShootAt(militaryNode.TargetUnit);
                    militaryNode.BurstAmmunitionRemaining.Ammo--;
                    militaryNode.MagazineAmmunitionRemaining.Ammo--;
                    if (militaryNode.MagazineAmmunitionRemaining.Ammo == 0)
                    {
                        militaryNode.GunWaitingState = GunWaitingState.GunWaitingForReload;
                        militaryNode.BurstAmmunitionRemaining.Ammo = (uint)defines.GunBehaviour;
                        militaryNode.MagazineAmmunitionRemaining = defines.MagazineAmmunition;
                    }
                    else if (militaryNode.BurstAmmunitionRemaining.Ammo == 0)
                    {
                        militaryNode.GunWaitingState = GunWaitingState.GunWaitingForBurst;
                        militaryNode.BurstAmmunitionRemaining.Ammo = (uint)defines.GunBehaviour;
                    }
                    else
                    {
                        militaryNode.GunWaitingState = GunWaitingState.GunWaitingForShot;
                    }

                    militaryNode.GunCooldownTicks = GetGunCooldown(militaryNode.GunWaitingState, defines);
                }

                break;
            case MilitaryNodeAction.NodeMovingAndFighting:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(militaryNode), militaryNode, null);
        }

        Debug.Log($"[MILITARY] {GetMilitaryNodeName(militaryNodeEntity)} A {militaryNode.MilitaryNodeAction}");
        return militaryNode;

        Entity GetNearbyUnit(Team team, Position from, RangeUnitsSquared within)
        {
            int index = GetTeamList(team).FindIndex(unit => math.lengthsq(unit.Position.GamePosition - from.GamePosition) < within.DistanceSquared);
            return index == -1 ? Entity.Null : GetEntity(team, index);
        }
        void ShootAt(Entity enemyEntity)
        {
            const uint CHANCE_TO_HIT = 10U;
            const uint CHANCE_TO_DODGE = 10U;

            MilitaryNode enemyMilitaryNode = GetMilitaryNode(enemyEntity);

            if (RandomDice(CHANCE_TO_HIT) == 0U)
            {
                if (RandomDice(CHANCE_TO_DODGE) < enemyMilitaryNode.HealthRemaining)
                {
                    const uint DAMAGE = 1;
                    enemyMilitaryNode.HealthRemaining -= math.min(enemyMilitaryNode.HealthRemaining, DAMAGE);
                    SetMilitaryNode(enemyEntity, enemyMilitaryNode);
                    EnemiesScripts[enemyEntity.Index].SetHealth(enemyMilitaryNode.HealthRemaining);

                    Debug.Log($"[MILITARY] {GetMilitaryNodeName(militaryNodeEntity)} hit {GetMilitaryNodeName(enemyEntity)}. Health left: {enemyMilitaryNode.HealthRemaining}");
                }
            }

            int2 diff = enemyMilitaryNode.Position.GamePosition - militaryNode.Position.GamePosition;
            float distance = math.length(diff);
            Bullet bullet = new()
            {
                Transform = Instantiate(BulletPrefab, BulletList).transform,
                To = enemyMilitaryNode.Position,
                From = militaryNode.Position,
                BulletVelocityWorldPerTick = defines.ProjectileVelocity.DistancePerTick / distance,
                Progress = 0.0F,
            };

            float angle = math.acos(diff.x / distance) * (diff.y < 0 ? -1 : 1);
            bullet.Transform.rotation = quaternion.Euler(0, 0, math.PIHALF + angle);
            Bullets.Add(bullet);
        }

        static bool ReachedTarget(Position position, Position target) => math.all(position.GamePosition == target.GamePosition);
        static bool TargetInRange(Position position, Position target, RangeUnitsSquared range) =>
            math.lengthsq(target.GamePosition - position.GamePosition) < range.DistanceSquared;
    }

    // Support
    private void CreateUnit(MilitaryNodeType militaryNodeType, Position position, Team team)
    {
        Debug.Log($"[GAME] {nameof(CreateUnit)} {militaryNodeType} {team}");
        MilitaryNode.Stat stat = Defines.Find(x => x.MilitaryNodeType == militaryNodeType).MilitaryNodeStats;
        Sprite image = ImageDefinitions.Find(x => x.MilitaryNodeType == militaryNodeType).Image;
        List<MilitaryNode> units = GetTeamList(team);
        units.Add(new MilitaryNode
        {
            MilitaryNodeType = militaryNodeType,
            Position = position,
            GunWaitingState = GunWaitingState.GunWaitingForShot,
            GunCooldownTicks = { Ticks = 0U },
            HealthRemaining = stat.Health,
            TargetPosition = null,
            TargetUnit = Entity.Null,
            MilitaryNodeAction = MilitaryNodeAction.NodeAlert,
            BurstAmmunitionRemaining = new Ammunition { Ammo = (uint)stat.GunBehaviour },
            MagazineAmmunitionRemaining = stat.MagazineAmmunition,
            MagazinesRemaining = stat.Magazines
        });

        GameObject go = Instantiate(UnitPrefab, position.WorldPosition, Quaternion.identity, transform);
        MilitaryScript militaryScript = go.GetComponent<MilitaryScript>();
        militaryScript.SetUnit(image, team.ToColor());
        militaryScript.SetHealth(stat.Health);
        militaryScript.SetStatusColor(MilitaryNodeAction.NodeAlert.ToColor());
        militaryScript.Entity = GetEntity(team, units.Count - 1);
        militaryScript.name = GetMilitaryNodeName(militaryScript.Entity);
        GetTeamScripts(team).Add(militaryScript);
    }
    private List<MilitaryNode> GetTeamList(Team team) => team switch
    {
        Team.BlueTeam => Friendlies,
        Team.RedTeam => Enemies,
        _ => throw new ArgumentOutOfRangeException(nameof(team), team, null)
    };
    private List<MilitaryNode> GetTeamList(Entity entity) => GetTeamList(GetTeam(entity));
    private List<MilitaryScript> GetTeamScripts(Team team) => team switch
    {
        Team.BlueTeam => FriendliesScripts,
        Team.RedTeam => EnemiesScripts,
        _ => throw new ArgumentOutOfRangeException(nameof(team), team, null)
    };
    private MilitaryNode GetMilitaryNode(Entity entity) => GetTeamList(entity)[entity.Index];
    private void SetMilitaryNode(Entity entity, in MilitaryNode military) => GetTeamList(entity)[entity.Index] = military;
    private static Entity GetEntity(Team team, int index) => new() { Index = index, Version = (int)team };
    private static Team GetTeam(Entity entity) => (Team)entity.Version;
    private string GetMilitaryNodeName(Entity entity) => $"{GetTeam(entity)} | {GetTeamList(entity)[entity.Index].MilitaryNodeType}";
    private CooldownTicks GetGunCooldown(GunWaitingState gunWaitingState, MilitaryNode.Stat stats) => gunWaitingState switch
    {
        GunWaitingState.GunWaitingForShot => stats.TicksBetweenShots,
        GunWaitingState.GunWaitingForBurst => stats.TicksBetweenBursts,
        GunWaitingState.GunWaitingForReload => stats.TicksBetweenReloads,
        _ => throw new ArgumentOutOfRangeException(nameof(gunWaitingState), gunWaitingState, null)
    };
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
public enum MilitaryNodeAction
{
    NodeAlert,
    NodeMoving,
    NodeFighting,
    NodeMovingAndFighting,
}
public enum GunBehaviour
{
    GunSingle = 1,
    GunBurst = 3,
    GunAuto = short.MaxValue,
}
public enum GunWaitingState
{
    GunWaitingForShot,
    GunWaitingForBurst,
    GunWaitingForReload,
}

[Serializable] public struct MilitaryNode
{
    public MilitaryNodeType MilitaryNodeType;

    public Position Position;
    public uint HealthRemaining;

    public Position? TargetPosition;
    public Entity TargetUnit;
    public MilitaryNodeAction MilitaryNodeAction;

    public GunWaitingState GunWaitingState;
    public CooldownTicks GunCooldownTicks;
    public Ammunition BurstAmmunitionRemaining;
    public Ammunition MagazineAmmunitionRemaining;
    public Magazines MagazinesRemaining;

    [Serializable] public struct Stat
    {
        public uint Health;

        public CooldownTicks TicksBetweenShots;
        public CooldownTicks TicksBetweenReloads;
        public RangeUnitsSquared RangeUnitsSquared;
        public Velocity ProjectileVelocity;
        public ProjectileType ProjectileType;
        public GunBehaviour GunBehaviour;
        public Ammunition MagazineAmmunition;
        public Magazines Magazines;

        public CooldownTicks TicksBetweenBursts => new() { Ticks = TicksBetweenShots.Ticks * 5U };
    }

    [Serializable] public struct Definition
    {
        public MilitaryNodeType MilitaryNodeType;
        public Stat MilitaryNodeStats;
    }

    [Serializable] public struct ImageDefinition
    {
        public MilitaryNodeType MilitaryNodeType;
        public Sprite Image;
    }
}

[Serializable] public struct Bullet
{
    public Transform Transform;
    public Position From, To;
    public float BulletVelocityWorldPerTick;
    public float Progress;

    public void Tick()
    {
        Progress += BulletVelocityWorldPerTick;
        int2 gamePosition = new(math.lerp(From.GamePosition, To.GamePosition, Progress));
        Vector3 transformPosition = new Position(gamePosition).WorldPosition;

        Transform.position = transformPosition;
    }
}