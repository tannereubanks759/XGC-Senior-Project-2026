using UnityEngine;
using UnityEngine.UI;

public class ChargeSphereUI : MonoBehaviour
{
    public Image fillImage;
    public chargeBaseScript chargeSource;

    public Color colorEmpty = new Color(0.2f, 0.4f, 1f, 0.8f);
    public Color colorFull = new Color(1f, 0.5f, 0.1f, 0.95f);
    public float fillSmoothSpeed = 4f;

    private float _displayFill;
    private float _targetFill;

    public void OnChargeChanged(float currentCharge, float maxCharge)
    {
        _targetFill = Mathf.Clamp01(currentCharge / maxCharge);
    }

    private void Update()
    {
        _displayFill = Mathf.Lerp(_displayFill, _targetFill, Time.deltaTime * fillSmoothSpeed);
        ApplyFill(_displayFill);
    }

    private void ApplyFill(float t)
    {
        if (fillImage == null) return;
        fillImage.fillAmount = t;
        fillImage.color = Color.Lerp(colorEmpty, colorFull, t);
    }
}