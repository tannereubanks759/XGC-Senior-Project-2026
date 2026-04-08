using UnityEngine;

public class MenuSelector : MonoBehaviour
{
    public RectTransform arrow;

    public void OnButtonHover(RectTransform buttonRect)
    {
        arrow.position = new Vector3(buttonRect.position.x - 80f, buttonRect.position.y, 0);
        arrow.gameObject.SetActive(true);
    }

    public void OnButtonExit()
    {
        arrow.gameObject.SetActive(false);
    }
}
