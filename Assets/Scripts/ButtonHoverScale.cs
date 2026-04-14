using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Scale Settings")]
    [SerializeField] private Vector3 hoverScale = new Vector3(1.08f, 1.08f, 1.08f);
    [SerializeField] private float scaleSpeed = 10f;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hoverClip;

    private RectTransform rectTransform;
    private Vector3 normalScale;
    private Vector3 targetScale;
    private bool isHovered;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        normalScale = rectTransform.localScale;
        targetScale = normalScale;
    }

    private void Update()
    {
        rectTransform.localScale = Vector3.Lerp(
            rectTransform.localScale,
            targetScale,
            Time.unscaledDeltaTime * scaleSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = hoverScale;

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
        targetScale = normalScale;
        isHovered = false;
    }
}