using UnityEngine;

public class itemDataAssigner : MonoBehaviour
{
    public ItemData itemData;

    [System.NonSerialized] public bool wasOwned; 

    public int CurrentPrice => wasOwned ? 0 : (itemData != null ? itemData.price : 0);

    private void Awake()
    {
        wasOwned = false; 
    }
}