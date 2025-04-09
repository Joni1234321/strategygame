using Unity.Entities;

public static class Extensions
{
    public static bool HasValue(this Entity entity) => entity != Entity.Null;
    public static void Reset(this ref Entity entity) => entity = Entity.Null;
}