using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public GameObject prefab;
    public string type;
    //public bool isActive;

    public virtual void OnEquip(GameObject player)
    {

    }

    public virtual void OnUnEquip(GameObject player)
    {

    }
}