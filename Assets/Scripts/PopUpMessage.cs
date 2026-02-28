using System.Collections;
using TMPro;
using UnityEngine;

public class PopUpMessage : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TMP_Text messageText;

    [Header("Timing")]
    [SerializeField] private float fadeInTime = 0.35f;
    [SerializeField] private float holdTime = 1.5f;
    [SerializeField] private float fadeOutTime = 0.35f;

    [Header("Behavior")]
    [SerializeField] private bool startHidden = true;

    private Coroutine _routine;

    private void Awake()
    {
        if (messageText == null)
            messageText = GetComponentInChildren<TMP_Text>();

        if (startHidden && messageText != null)
            SetAlpha(0f);
    }

    public void ShowMessage(string text)
    {
        if (messageText == null) return;

        messageText.text = text;

        if (_routine != null)
            StopCoroutine(_routine);

        _routine = StartCoroutine(PopupRoutine());
    }

    private IEnumerator PopupRoutine()
    {
        //Ensure it starts invisible (nice if called back-to-back).
        SetAlpha(0f);

        //Fade in
        yield return FadeTo(1f, Mathf.Max(0.0001f, fadeInTime));

        //Hold
        if (holdTime > 0f)
            yield return new WaitForSeconds(holdTime);

        //Fade out
        yield return FadeTo(0f, Mathf.Max(0.0001f, fadeOutTime));

        _routine = null;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = messageText.alpha;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime; 
            float a = Mathf.Lerp(startAlpha, targetAlpha, t / duration);
            messageText.alpha = a;
            yield return null;
        }

        messageText.alpha = targetAlpha;
    }

    private void SetAlpha(float a)
    {
        messageText.alpha = a;
    }
}