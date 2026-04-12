using UnityEngine;
using TMPro;

public class TutorialMessage : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;

    private void Awake()
    {
        if (messageText == null)
            messageText = GetComponentInChildren<TMP_Text>();
        SetAlpha(0f);
    }

    public void Show(string text)
    {
        messageText.text = text;
        SetAlpha(1f);
    }

    public void Clear()
    {
        SetAlpha(0f);
    }

    private void SetAlpha(float a) => messageText.alpha = a;
}

