using UnityEngine;

public class tooltipShower : MonoBehaviour
{
    private static tooltipShower current;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public tooltip tooltipR;
    public void Awake()
    {
        current = this; 
    }
    public static void Show(string description, string name = "")
    {
        current.tooltipR.SetText(description, name);
        current.tooltipR.gameObject.SetActive(true);
    }
    public static void Hide()
    {
        current.tooltipR.gameObject.SetActive(false);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
