using UnityEngine;

public class DodgeDash : MonoBehaviour
{
    [Header("Refs")]
    public Rigidbody rb;
    public Camera cam;
    public FirstPersonController controller;

    [Header("Tuning (in m/s change)")]
    public float dashSpeedChange = 12f;   
    public float dodgeSpeedChange = 8f;     
    public float lockDuration = 0.15f;   
    public float maxHorizontalSpeed = 14f; 

    Coroutine lockRoutine;

    void Reset()
    {
        rb = GetComponent<Rigidbody>();
        cam = Camera.main;
        controller = GetComponentInParent<FirstPersonController>();
    }

    void Start()
    {
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    public void Dash()
    {
        Vector3 dir = cam ? cam.transform.forward : transform.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-4f) return;
        dir.Normalize();

        ApplyHorizontalVelocityChange(dir, dashSpeedChange);
        LockController(lockDuration);
    }

    public void Dodge(Vector3 direction)
    {
        if (controller != null && (!controller.isGrounded || rb.linearVelocity.magnitude <= 0.1f))
            return;

        Vector3 dir = direction; 
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-4f) return;
        dir.Normalize();

        ApplyHorizontalVelocityChange(dir, dodgeSpeedChange);
        LockController(lockDuration);
    }

    void ApplyHorizontalVelocityChange(Vector3 dir, float speedChange)
    {
        Vector3 v = rb.linearVelocity;                 
        Vector3 vH = new Vector3(v.x, 0f, v.z);

        Vector3 vHNew = Vector3.ClampMagnitude(vH + dir * speedChange, maxHorizontalSpeed);
        Vector3 dv = vHNew - vH;

        rb.AddForce(new Vector3(dv.x, 0f, dv.z), ForceMode.VelocityChange);
    }

    void LockController(float seconds)
    {
        if (controller == null) return;
        if (lockRoutine != null) StopCoroutine(lockRoutine);
        lockRoutine = StartCoroutine(LockRoutine(seconds));
    }

    System.Collections.IEnumerator LockRoutine(float seconds)
    {
        bool prev = controller.playerCanMove;
        controller.playerCanMove = false;          
        float t = 0f;
        while (t < seconds)
        {
            yield return new WaitForFixedUpdate();
            t += Time.fixedDeltaTime;
        }
        controller.playerCanMove = prev;
        lockRoutine = null;
    }
}
