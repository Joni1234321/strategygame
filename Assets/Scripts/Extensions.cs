using System;
using Unity.Entities;
using UnityEngine;

public static class Extensions
{
    public static bool HasValue(this Entity entity) => entity != Entity.Null;
    public static void Reset(this ref Entity entity) => entity = Entity.Null;

    public static void DestroyEither(this GameObject gameObject)
    {
        if (Application.isEditor) UnityEngine.Object.DestroyImmediate(gameObject);
        else UnityEngine.Object.Destroy(gameObject);
    }

    private const float SATURATION = 1.0F;
    private const float VALUE = 0.4f;
    private static Color GetColor(float hue) => Color.HSVToRGB(hue, SATURATION, VALUE);
    public static Color ToColor(this UnitAction unitAction) => unitAction switch
    {
        UnitAction.UnitAlert => GetColor(0.4F),
        UnitAction.UnitMoving => GetColor(0.6F),
        UnitAction.UnitFighting => GetColor(0.8F),
        UnitAction.UnitMovingAndFighting => GetColor(1.0F),
        _ => throw new ArgumentOutOfRangeException(nameof(unitAction), unitAction, null)
    };
}