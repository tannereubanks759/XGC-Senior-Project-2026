using UnityEngine;
using System.Collections;

public class InstantBulletTrail : MonoBehaviour
{
    public LineRenderer lr;

    [Header("Timing")]
    public float dissipateTime = 0.35f; // how long the dissolve takes

    [Header("Shader Params")]
    public Color trailTint = Color.white;
    public float noiseStrength = 1f;
    public float noiseSpeed = 1f;
    public Vector2 customNoiseTiling = new Vector2(1, 1);

    private Material mat;
    private Vector3 startPos;
    private Vector3 endPos;

    public void Initialize(Vector3 start, Vector3 end)
    {
        startPos = start;
        endPos = end;

        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        // Instance the material so each tracer has its own animated dissolve
        mat = Instantiate(lr.material);
        lr.material = mat;

        // Set static shader params
        mat.SetColor("_TintColor", trailTint);
        mat.SetFloat("_NoiseStrength", noiseStrength);
        mat.SetFloat("_NoiseSpeed", noiseSpeed);
        mat.SetVector("_CustomNoiseTiling", customNoiseTiling);

        StartCoroutine(DissolveRoutine());
    }

    IEnumerator DissolveRoutine()
    {
        float t = 0f;

        while (t < dissipateTime)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / dissipateTime);

            // Shader fade parameter
            mat.SetFloat("_NoiseDissolve", progress);

            yield return null;
        }

        Destroy(gameObject);
    }
}
