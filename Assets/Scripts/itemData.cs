using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public GameObject prefab;
    //public bool isActive;

    public virtual void OnEquip()
    {

    }

    public virtual void OnUnEquip()
    {

    }
}