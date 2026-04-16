using UnityEngine;
using UnityEngine.UI;

public class CreditsScroll : MonoBehaviour
{
    public RectTransform content;
    public float scrollSpeed = 50f;

    void Update()
    {
        content.anchoredPosition += Vector2.up * scrollSpeed * Time.unscaledDeltaTime;
    }
    void OnEnable()
    {
        content.anchoredPosition = Vector2.zero;
    }
}