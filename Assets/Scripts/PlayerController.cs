using Unity.Entities;
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
}
