using UnityEngine;
using UnityEngine.UI;

public class CompassUI : MonoBehaviour
{
    [Header("Heading Source")]
    public Transform headingSource;                 // Main Camera or player root
    public bool useVelocityWhenMoving = false;      // optional: use velocity for heading
    public float velocityMinSpeed = 0.2f;

    [Header("UI")]
    public RectTransform viewport;                  // must have RectMask2D or Mask
    public RectTransform stripRoot;                 // slides horizontally
    public RectTransform stripL;                    // left tile  (−W)
    public RectTransform stripC;                    // center tile (0)
    public RectTransform stripR;                    // right tile  (+W)

    [Header("Layout")]
    [Tooltip("Pixel width of ONE full 0–360° strip. If 0, auto-read from stripC.rect.width.")]
    public float stripWidthPixels = 0f;
    [Tooltip("Which heading sits at the viewport center. 0 = North centered; 180 if your artwork starts at left edge.")]
    [Range(0f, 360f)] public float centerOffsetDegrees = 180f;
    [Range(0f, 30f)] public float smooth = 12f;     // 0 = instant

    float _ppd;                 // pixels per degree
    float _uCur;                // smoothed, continuous “compass coordinate”
    Rigidbody _rb;

    void Awake()
    {
        if (!headingSource && Camera.main) headingSource = Camera.main.transform;
        if (headingSource) _rb = headingSource.GetComponent<Rigidbody>();

        if (!viewport || !stripRoot || !stripC || !stripL || !stripR) return;

        // Determine W (one full strip width)
        if (stripWidthPixels <= 0f) stripWidthPixels = stripC.rect.width > 0f ? stripC.rect.width : 2048f;
        _ppd = stripWidthPixels / 360f;

        // Ensure anchors for sliding: left-center works best
        SetupRT(stripRoot, 0f);
        SetupRT(stripL, 0f);
        SetupRT(stripC, 0f);
        SetupRT(stripR, 0f);

        // Place the three tiles so they tile seamlessly
        stripL.anchoredPosition = new Vector2(-stripWidthPixels, 0f);
        stripC.anchoredPosition = new Vector2(0f, 0f);
        stripR.anchoredPosition = new Vector2(stripWidthPixels, 0f);

        // Sanity: viewport must be narrower than W so seams never show
        if (viewport.rect.width >= stripWidthPixels - 0.5f)
            Debug.LogWarning("[Compass] Viewport width >= strip width. Reduce viewport width so seams stay outside the mask.");
    }

    void LateUpdate()
    {
        if (!headingSource || !viewport || !stripRoot) return;

        // --- 1) Get heading on XZ ---
        Vector3 fwd = (useVelocityWhenMoving && _rb && _rb.linearVelocity.sqrMagnitude > velocityMinSpeed * velocityMinSpeed)
                      ? _rb.linearVelocity : headingSource.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-6f) return;
        fwd.Normalize();

        float yaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg; // 0=+Z
        if (yaw < 0f) yaw += 360f;

        // --- 2) Convert to continuous compass coordinate (pixels) with center offset ---
        // u = yaw*ppd - centerPx. When u increases to the right, the stripRoot moves left by u.
        float uTarget = yaw * _ppd - (centerOffsetDegrees * _ppd);

        // Smooth in "unwrapped" space to avoid pops at 360->0
        _uCur = (smooth <= 0f)
            ? uTarget
            : Mathf.Lerp(_uCur, uTarget, 1f - Mathf.Exp(-smooth * Time.deltaTime));

        // --- 3) Wrap AFTER smoothing and drive the root ---
        // Keep root shift in [0, W) to avoid large values, but movement stays continuous because _uCur is smooth.
        float W = stripWidthPixels;
        float baseU = Mathf.Repeat(_uCur, W);      // [0, W)
        float rootX = -baseU;                      // center of viewport corresponds to u = _uCur

        stripRoot.anchoredPosition = new Vector2(rootX, stripRoot.anchoredPosition.y);

        // --- 4) Keep tiles positioned at exact multiples around root (no runtime recycling needed) ---
        // (Already placed in Awake; only root moves.)
        // If you ever re-size W at runtime, re-assign stripL/C/R anchoredPosition accordingly.
    }

    static void SetupRT(RectTransform rt, float pivotX)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(pivotX, 0.5f);
    }
}
