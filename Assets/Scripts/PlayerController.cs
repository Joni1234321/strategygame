using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[ExecuteAlways] public sealed class PlayerController : Singleton<PlayerController>
{
    public Camera MainCamera;
    public Entity SelectedUnit;
    
    protected override void OnEnable()
    {
        base.OnEnable();
        
        MainCamera = Camera.main;
    }

    public UnityPosition GetMousePosition()
    {
        Vector2 worldPosition = MainCamera.ScreenToWorldPoint(Input.mousePosition);
        return new UnityPosition() { WorldPosition = new float2(worldPosition.x, worldPosition.y) };
    }
}
