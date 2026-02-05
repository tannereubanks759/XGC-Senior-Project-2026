using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class tooltip : MonoBehaviour
{
    [ExecuteInEditMode()]
    public TextMeshProUGUI nameField;

    public TextMeshProUGUI descriptionField;
    public TextMeshProUGUI priceField;
    public LayoutElement layoutelement;
    public int charLimit;
    public float maxWidth = 650f;
    public RectTransform rect;
    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }
    public void SetText(string description, string name = "", string price ="")
    {
        nameField.text = name;
        descriptionField.text = description;
        priceField.text = price;
        //int titleLength = nameField.text.Length;
        //int descriptionLength = descriptionField.text.Length;
        //layoutelement.enabled = (titleLength > charLimit || descriptionLength > charLimit) ? true : false;
        layoutelement.enabled = true;
        layoutelement.preferredWidth = maxWidth;
    }
    // Update is called once per frame
    void Update()
    {
        //Vector2 position = Input.mousePosition;
        //float x =  position.x/Screen.width;
        //float y = position.y/Screen.height;
        //rect.pivot = new Vector2(x,y);
        //transform.position = position;
        Vector2 mousePos = Input.mousePosition;
        rect.pivot = new Vector2(mousePos.x / Screen.width > 0.5f ? 1f : 0f, mousePos.y / Screen.height > 0.5f ? 1f : 0f);
        transform.position = mousePos;
    }
}
