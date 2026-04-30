using UnityEngine;

public class FloatingRaftNoCollider : MonoBehaviour
{
    [Header("Float Points")]
    public Transform frontLeftPoint;
    public Transform frontRightPoint;
    public Transform backLeftPoint;
    public Transform backRightPoint;

    [Header("Water Level")]
    public float baseWaterHeight = 0f;
    public float heightOffset = 0.35f;

    [Header("Wave Settings")]
    public float waveAmplitude = 0.4f;
    public float waveLength = 6f;
    public float waveSpeed = 1.5f;

    [Header("Secondary Wave")]
    public bool useSecondaryWave = true;
    public float secondaryAmplitude = 0.18f;
    public float secondaryWaveLength = 3f;
    public float secondaryWaveSpeed = 2.2f;

    [Header("Smoothing")]
    public float positionSmoothSpeed = 3f;
    public float rotationSmoothSpeed = 4f;

    [Header("Rotation Limits")]
    public float maxPitchAngle = 18f;
    public float maxRollAngle = 18f;

    private void Reset()
    {
        CreateDefaultFloatPoints();
    }

    private void FixedUpdate()
    {
        if (!HasFloatPoints())
            return;

        UpdateFloating();
    }

    private void UpdateFloating()
    {
        Vector3 frontLeftWaterPoint = GetWaterPoint(frontLeftPoint.position);
        Vector3 frontRightWaterPoint = GetWaterPoint(frontRightPoint.position);
        Vector3 backLeftWaterPoint = GetWaterPoint(backLeftPoint.position);
        Vector3 backRightWaterPoint = GetWaterPoint(backRightPoint.position);

        Vector3 averageWaterPoint =
            (frontLeftWaterPoint +
             frontRightWaterPoint +
             backLeftWaterPoint +
             backRightWaterPoint) / 4f;

        Vector3 targetPosition = new Vector3(
            transform.position.x,
            averageWaterPoint.y + heightOffset,
            transform.position.z
        );

        Vector3 frontCenter = (frontLeftWaterPoint + frontRightWaterPoint) / 2f;
        Vector3 backCenter = (backLeftWaterPoint + backRightWaterPoint) / 2f;
        Vector3 leftCenter = (frontLeftWaterPoint + backLeftWaterPoint) / 2f;
        Vector3 rightCenter = (frontRightWaterPoint + backRightWaterPoint) / 2f;

        Vector3 forwardDirection = frontCenter - backCenter;
        Vector3 rightDirection = rightCenter - leftCenter;

        Vector3 waterNormal = Vector3.Cross(forwardDirection, rightDirection).normalized;

        if (waterNormal.y < 0f)
        {
            waterNormal = -waterNormal;
        }

        Quaternion targetRotation = Quaternion.LookRotation(
            Vector3.ProjectOnPlane(transform.forward, waterNormal),
            waterNormal
        );

        targetRotation = ClampRaftRotation(targetRotation);

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            positionSmoothSpeed * Time.fixedDeltaTime
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSmoothSpeed * Time.fixedDeltaTime
        );
    }

    private Vector3 GetWaterPoint(Vector3 worldPosition)
    {
        float waterHeight = GetWaterHeight(worldPosition.x, worldPosition.z);

        return new Vector3(
            worldPosition.x,
            waterHeight,
            worldPosition.z
        );
    }

    private float GetWaterHeight(float x, float z)
    {
        float mainWave =
            Mathf.Sin((x + Time.time * waveSpeed) / waveLength * Mathf.PI * 2f) *
            waveAmplitude;

        float crossWave =
            Mathf.Sin((z + Time.time * waveSpeed * 0.75f) / waveLength * Mathf.PI * 2f) *
            waveAmplitude *
            0.5f;

        float finalHeight = baseWaterHeight + mainWave + crossWave;

        if (useSecondaryWave)
        {
            float secondaryWave =
                Mathf.Sin((x + z + Time.time * secondaryWaveSpeed) / secondaryWaveLength * Mathf.PI * 2f) *
                secondaryAmplitude;

            finalHeight += secondaryWave;
        }

        return finalHeight;
    }

    private Quaternion ClampRaftRotation(Quaternion targetRotation)
    {
        Vector3 euler = targetRotation.eulerAngles;

        float pitch = NormalizeAngle(euler.x);
        float yaw = euler.y;
        float roll = NormalizeAngle(euler.z);

        pitch = Mathf.Clamp(pitch, -maxPitchAngle, maxPitchAngle);
        roll = Mathf.Clamp(roll, -maxRollAngle, maxRollAngle);

        return Quaternion.Euler(pitch, yaw, roll);
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }

    private bool HasFloatPoints()
    {
        return frontLeftPoint != null &&
               frontRightPoint != null &&
               backLeftPoint != null &&
               backRightPoint != null;
    }

    private void CreateDefaultFloatPoints()
    {
        if (frontLeftPoint != null)
            return;

        GameObject frontLeft = new GameObject("FrontLeftPoint");
        GameObject frontRight = new GameObject("FrontRightPoint");
        GameObject backLeft = new GameObject("BackLeftPoint");
        GameObject backRight = new GameObject("BackRightPoint");

        frontLeft.transform.SetParent(transform);
        frontRight.transform.SetParent(transform);
        backLeft.transform.SetParent(transform);
        backRight.transform.SetParent(transform);

        frontLeft.transform.localPosition = new Vector3(-1f, 0f, 1.5f);
        frontRight.transform.localPosition = new Vector3(1f, 0f, 1.5f);
        backLeft.transform.localPosition = new Vector3(-1f, 0f, -1.5f);
        backRight.transform.localPosition = new Vector3(1f, 0f, -1.5f);

        frontLeftPoint = frontLeft.transform;
        frontRightPoint = frontRight.transform;
        backLeftPoint = backLeft.transform;
        backRightPoint = backRight.transform;
    }

    private void OnDrawGizmosSelected()
    {
        DrawPoint(frontLeftPoint);
        DrawPoint(frontRightPoint);
        DrawPoint(backLeftPoint);
        DrawPoint(backRightPoint);
    }

    private void DrawPoint(Transform point)
    {
        if (point == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(point.position, 0.12f);

        Vector3 waterPoint = GetWaterPoint(point.position);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(point.position, waterPoint);
        Gizmos.DrawSphere(waterPoint, 0.08f);
    }
}