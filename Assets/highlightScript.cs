using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class highlightScript : MonoBehaviour
{ 
    public string slotTypeTag;

   
    private Image highlightImg;
    void Awake()
    {
        highlightImg= gameObject.GetComponent<Image>();
        SetHighlight(false);

    }

    public void SetHighlight(bool on)
    {
        Color c = highlightImg.color;
        c.a = on ? 1f : 0f;
        highlightImg.color = c;
    }

    public void OnDragTypeSelected(string draggedType)
    {
        
        SetHighlight(string.Equals(slotTypeTag, draggedType, System.StringComparison.OrdinalIgnoreCase));
    }

    public void OnDragEnded()
    {
        SetHighlight(false);
    }

}

