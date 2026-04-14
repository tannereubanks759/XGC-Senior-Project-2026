using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverResize : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Size Settings")]
    [SerializeField] private Vector2 hoverSize = new Vector2(220f, 70f);
    [SerializeField] private float resizeSpeed = 10f;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hoverClip;

    private RectTransform rectTransform;
    private Vector2 normalSize;
    private Vector2 targetSize;
    private bool isHovered;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        normalSize = rectTransform.sizeDelta;
        targetSize = normalSize;
    }

    private void Update()
    {
        rectTransform.sizeDelta = Vector2.Lerp(
            rectTransform.sizeDelta,
            targetSize,
            Time.unscaledDeltaTime * resizeSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetSize = normalSize + new Vector2(20f, 10f);

        if (!isHovered)
        {
            isHovered = true;

            if (audioSource != null && hoverClip != null)
            {
                audioSource.PlayOneShot(hoverClip);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetSize = normalSize;
        isHovered = false;
    }
}