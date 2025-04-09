using Unity.Entities;
using UnityEngine;

public class MilitaryScript : MonoBehaviour
{
    [SerializeField] private SpriteRenderer icon;
    [SerializeField] private SpriteRenderer status;
    [SerializeField] private SpriteRenderer selected;
    [SerializeField] private Transform healthGroup;

    public Entity Entity { get; set; }

    public void SetUnitName(Team team, MilitaryNodeType militaryNodeType) => gameObject.name = $"{team} | {militaryNodeType}";
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
    
    private void OnMouseDown()
    {
        Debug.Log($"[UNIT] Selected {Entity}");
        PlayerController.I.SelectedUnit = Entity;
        selected.enabled = true;
    }
}
