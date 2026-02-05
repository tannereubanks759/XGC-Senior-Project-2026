using UnityEngine;

public class tooltipShower : MonoBehaviour
{
    private static tooltipShower current;
    public tooltip tooltipR;
    public void Awake()
    {
        current = this; 
    }
    public static void Show(string description, string name = "", string price="")
    {
        //Debug.Log("Recieved Call");
        current.tooltipR.SetText(description, name, price);
        current.tooltipR.gameObject.SetActive(true);
        Debug.Log("Updated");
    }
    public static void Hide()
    {
        current.tooltipR.gameObject.SetActive(false);
    }
    
}
