using System;
using Unity.Entities;
using UnityEngine;

public static class Extensions
{
    public static bool HasValue(this Entity entity) => entity != Entity.Null;
    public static void Reset(this ref Entity entity) => entity = Entity.Null;

    public static void UniversalDestroy(this GameObject gameObject)
    {
        if (Application.isEditor) UnityEngine.Object.DestroyImmediate(gameObject);
        else UnityEngine.Object.Destroy(gameObject);
    }

    private const float SATURATION = 1.0F;
    private const float VALUE = 0.4f;
    private static Color GetColor(float hue) => Color.HSVToRGB(hue, SATURATION, VALUE);
    public static Color ToColor(this MilitaryNodeAction militaryNodeAction) => militaryNodeAction switch
    {
        MilitaryNodeAction.NodeAlert => GetColor(0.4F),
        MilitaryNodeAction.NodeMoving => GetColor(0.6F),
        MilitaryNodeAction.NodeFighting => GetColor(0.8F),
        MilitaryNodeAction.NodeMovingAndFighting => GetColor(1.0F),
        _ => throw new ArgumentOutOfRangeException(nameof(militaryNodeAction), militaryNodeAction, null)
    };

    public static Color ToColor(this Team team) => team switch
    {
        Team.BlueTeam => Color.blue,
        Team.RedTeam => Color.red,
        _ => throw new ArgumentOutOfRangeException(nameof(team), team, null)
    };
    
    public static Color ToColor(this GunWaitingState gunWaitingState) => gunWaitingState switch
    {
        GunWaitingState.GunWaitingForShot => GetColor(0.4F),
        GunWaitingState.GunWaitingForBurst => GetColor(0.6F),
        GunWaitingState.GunWaitingForReload => GetColor(0.8F),
        _ => throw new ArgumentOutOfRangeException(nameof(gunWaitingState), gunWaitingState, null)
    };
}