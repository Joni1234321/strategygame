using UnityEngine;

public class UnitScript : MonoBehaviour
{
    [SerializeField] private SpriteRenderer icon;
    [SerializeField] private SpriteRenderer status;
    [SerializeField] private GameObject healthGroup;

    public void SetStatusColor(Color color) => status.color = color;
    public void SetUnit(Sprite unitIcon, Color teamColor)
    {
        icon.sprite = unitIcon;
        icon.color = teamColor;
    }
    public void SetHealth(int health)
    {
        for (int i = 0; i < healthGroup.transform.childCount; i++)
        {
            healthGroup.SetActive(health >= i);
        }        
    }
}
