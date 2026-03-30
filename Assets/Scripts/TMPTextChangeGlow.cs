using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TMPTextIncreaseGlow : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float effectDuration = 0.35f;

    [Header("Face Color")]
    [SerializeField] private bool tintFaceYellowToo = true;
    [SerializeField] private Color glowFaceColor = Color.yellow;

    [Header("TMP Glow")]
    [SerializeField] private Color glowColor = Color.yellow;
    [SerializeField] private float targetGlowOffset = 0f;
    [SerializeField] private float targetGlowInner = 0.05f;
    [SerializeField] private float targetGlowOuter = 0.15f;
    [SerializeField] private float targetGlowPower = 0.9f;

    [Header("Size Pop")]
    [SerializeField] private bool animateFontSize = true;
    [SerializeField] private float sizeIncrease = 6f;

    private TextMeshProUGUI tmp;
    private Material instanceMaterial;
    private Coroutine effectRoutine;

    private string lastText;

    private Color originalFaceColor;
    private float originalFontSize;

    private Color originalGlowColor;
    private float originalGlowOffset;
    private float originalGlowInner;
    private float originalGlowOuter;
    private float originalGlowPower;

    private void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();

        instanceMaterial = Instantiate(tmp.fontSharedMaterial);
        tmp.fontSharedMaterial = instanceMaterial;

        originalFaceColor = tmp.faceColor;
        originalFontSize = tmp.fontSize;
        lastText = tmp.text;

        if (instanceMaterial.HasProperty(ShaderUtilities.ID_GlowColor))
            originalGlowColor = instanceMaterial.GetColor(ShaderUtilities.ID_GlowColor);

        if (instanceMaterial.HasProperty(ShaderUtilities.ID_GlowOffset))
            originalGlowOffset = instanceMaterial.GetFloat(ShaderUtilities.ID_GlowOffset);

        if (instanceMaterial.HasProperty(ShaderUtilities.ID_GlowInner))
            originalGlowInner = instanceMaterial.GetFloat(ShaderUtilities.ID_GlowInner);

        if (instanceMaterial.HasProperty(ShaderUtilities.ID_GlowOuter))
            originalGlowOuter = instanceMaterial.GetFloat(ShaderUtilities.ID_GlowOuter);

        if (instanceMaterial.HasProperty(ShaderUtilities.ID_GlowPower))
            originalGlowPower = instanceMaterial.GetFloat(ShaderUtilities.ID_GlowPower);
    }

    private void Update()
    {
        string currentText = tmp.text;

        if (currentText == lastText)
            return;

        bool shouldPlay = ShouldTriggerEffect(lastText, currentText);
        lastText = currentText;

        if (!shouldPlay)
            return;

        if (effectRoutine != null)
            StopCoroutine(effectRoutine);

        RestoreOriginals();
        effectRoutine = StartCoroutine(PlayEffect());
    }

    private bool ShouldTriggerEffect(string oldText, string newText)
    {
        if (!TryParseNumber(oldText, out float oldValue))
            return false;

        if (!TryParseNumber(newText, out float newValue))
            return false;

        return newValue > oldValue;
    }

    private bool TryParseNumber(string text, out float value)
    {
        text = text.Trim();

        return float.TryParse(
            text,
            NumberStyles.Float | NumberStyles.AllowThousands,
            CultureInfo.InvariantCulture,
            out value
        );
    }

    private IEnumerator PlayEffect()
    {
        float halfDuration = effectDuration * 0.5f;
        float timer = 0f;

        while (timer < halfDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / halfDuration);
            ApplyEffect(t);
            yield return null;
        }

        timer = 0f;

        while (timer < halfDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = 1f - Mathf.Clamp01(timer / halfDuration);
            ApplyEffect(t);
            yield return null;
        }

        RestoreOriginals();
        effectRoutine = null;
    }

    private void ApplyEffect(float t)
    {
        if (tintFaceYellowToo)
            tmp.faceColor = Color.Lerp(originalFaceColor, glowFaceColor, t);

        if (animateFontSize)
            tmp.fontSize = Mathf.Lerp(originalFontSize, originalFontSize + sizeIncrease, t);

        if (instanceMaterial.HasProperty(ShaderUtilities.ID_GlowColor))
            instanceMaterial.SetColor(
                ShaderUtilities.ID_GlowColor,
                Color.Lerp(originalGlowColor, glowColor, t)
            );

        if (instanceMaterial.HasProperty(ShaderUtilities.ID_GlowOffset))
            instanceMaterial.SetFloat(
                ShaderUtilities.ID_GlowOffset,
                Mathf.Lerp(originalGlowOffset, targetGlowOffset, t)
            );

        if (instanceMaterial.HasProperty(ShaderUtilities.ID_GlowInner))
            instanceMaterial.SetFloat(
                ShaderUtilities.ID_GlowInner,
                Mathf.Lerp(originalGlowInner, targetGlowInner, t)
            );

        if (instanceMaterial.HasProperty(ShaderUtilities.ID_GlowOuter))
            instanceMaterial.SetFloat(
                ShaderUtilities.ID_GlowOuter,
                Mathf.Lerp(originalGlowOuter, targetGlowOuter, t)
            );

        if (instanceMaterial.HasProperty(ShaderUtilities.ID_GlowPower))
            instanceMaterial.SetFloat(
                ShaderUtilities.ID_GlowPower,
                Mathf.Lerp(originalGlowPower, targetGlowPower, t)
            );
    }

    private void RestoreOriginals()
    {
        tmp.faceColor = originalFaceColor;
        tmp.fontSize = originalFontSize;

        if (instanceMaterial.HasProperty(ShaderUtilities.ID_GlowColor))
            instanceMaterial.SetColor(ShaderUtilities.ID_GlowColor, originalGlowColor);

        if (instanceMaterial.HasProperty(ShaderUtilities.ID_GlowOffset))
            instanceMaterial.SetFloat(ShaderUtilities.ID_GlowOffset, originalGlowOffset);

        if (instanceMaterial.HasProperty(ShaderUtilities.ID_GlowInner))
            instanceMaterial.SetFloat(ShaderUtilities.ID_GlowInner, originalGlowInner);

        if (instanceMaterial.HasProperty(ShaderUtilities.ID_GlowOuter))
            instanceMaterial.SetFloat(ShaderUtilities.ID_GlowOuter, originalGlowOuter);

        if (instanceMaterial.HasProperty(ShaderUtilities.ID_GlowPower))
            instanceMaterial.SetFloat(ShaderUtilities.ID_GlowPower, originalGlowPower);
    }
}