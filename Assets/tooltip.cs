using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class tooltip : MonoBehaviour
{
    [ExecuteInEditMode()]
    public TextMeshProUGUI nameField;

    public TextMeshProUGUI descriptionField;
    public LayoutElement layoutelement;
    public int charLimit;
    public RectTransform rect;
    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }
    public void SetText(string description, string name = "")
    {
        nameField.text = name;
        descriptionField.text = description;
        int titleLength = nameField.text.Length;
        int descriptionLength = descriptionField.text.Length;
        layoutelement.enabled = (titleLength > charLimit || descriptionLength > charLimit) ? true : false;
    }
    // Update is called once per frame
    void Update()
    {
        Vector2 position = Input.mousePosition;
        //float x =  position.x/Screen.width;
        //float y = position.y/Screen.height;
        //rect.pivot = new Vector2(x,y);
        transform.position = position;
    }
}
