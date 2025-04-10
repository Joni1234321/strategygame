using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class MilitaryScript : MonoBehaviour
{
    [SerializeField] private SpriteRenderer icon;
    [SerializeField] private SpriteRenderer status;
    [SerializeField] private SpriteRenderer selected;
    [SerializeField] private Transform cooldown;
    [SerializeField] private SpriteRenderer cooldownRenderer;
    [SerializeField] private Transform healthGroup;

    public Entity Entity { get; set; }

    public void SetStatusColor(Color color) => status.color = color;
    public void SetUnit(Sprite unitIcon, Color teamColor)
    {
        icon.sprite = unitIcon;
        icon.color = teamColor;
    }
    public void SetHealth(uint health)
    {
        for (int i = 0; i < healthGroup.transform.childCount; i++) healthGroup.transform.GetChild(i).gameObject.SetActive(health > i);        
    }
    public void SetCooldown(Color color, float cooldownPercentage)
    {
        cooldownRenderer.color = color;
        Vector3 localScale = cooldown.transform.localScale;
        localScale.x = 1 - math.clamp(cooldownPercentage, 0.0F, 1.0F);
        cooldown.transform.localScale = localScale;
    }
    
    private void OnMouseDown()
    {
        Debug.Log($"[UNIT] Selected {Entity}");
        PlayerController.I.SelectedUnit = Entity;
        selected.enabled = true;
    }
}
