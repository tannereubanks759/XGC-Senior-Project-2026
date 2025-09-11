using UnityEngine;
using System.Collections;

public class DodgeDash : MonoBehaviour
{
    [Header("Refs")]
    public Rigidbody rb;
    public Camera cam;
    public FirstPersonController controller;

    [Header("Tuning (Δspeed in m/s)")]
    public float dashSpeedChange = 12f;     //desired delta-v along dash dir
    public float dodgeSpeedChange = 8f;     //desired delta-v along dodge dir
    public float lockDuration = 0.15f;      //how long to lock player input
    public float maxHorizontalSpeed = 14f;  //hard cap on horizontal speed

    [Header("Acceleration (timing & feel)")]
    public float dashDuration = 0.18f;      //seconds to deliver dash Δv
    public float dodgeDuration = 0.14f;     //seconds to deliver dodge Δv

    //Maps 0→1 time to 0→1 of the total Δv delivered. EaseInOut is a nice default.
    public AnimationCurve speedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine lockRoutine;
    private Coroutine burstRoutine;

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

        StartBurst(dir, dashSpeedChange, dashDuration);
        LockController(lockDuration);
    }

    public void Dodge(Vector3 direction)
    {
        //ground-locked dodge (adjust if you want air dodges)
        if (controller != null && (!controller.isGrounded || GetVel().magnitude <= 0.1f))
            return;

        Vector3 dir = direction;
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-4f) return;
        dir.Normalize();

        StartBurst(dir, dodgeSpeedChange, dodgeDuration);
        LockController(lockDuration);
    }

    void StartBurst(Vector3 dir, float speedChange, float duration)
    {
        if (burstRoutine != null) StopCoroutine(burstRoutine);
        burstRoutine = StartCoroutine(AccelerateBurst(dir, speedChange, duration));
    }

    IEnumerator AccelerateBurst(Vector3 dir, float speedChange, float duration)
    {
        duration = Mathf.Max(0.0001f, duration);
        float t = 0f;

        //Initial along-direction speed and target along-direction speed
        Vector3 v0 = GetVel();
        Vector3 v0H = new Vector3(v0.x, 0f, v0.z);
        float startAlong = Vector3.Dot(v0H, dir);
        float targetAlong = Mathf.Clamp(startAlong + speedChange, -maxHorizontalSpeed, maxHorizontalSpeed);

        float prevK = speedCurve.Evaluate(0f);

        while (t < duration)
        {
            yield return new WaitForFixedUpdate();
            float dt = Time.fixedDeltaTime;
            t = Mathf.Min(t + dt, duration);

            float k = speedCurve.Evaluate(t / duration);         //0→1 progress of total Δv
            float desiredDeltaThisStep = (targetAlong - startAlong) * (k - prevK); //v for this tick
            prevK = k;

            //Current along-speed may have changed due to other forces — clamp to avoid overshoot.
            Vector3 vCur = GetVel();
            Vector3 vCurH = new Vector3(vCur.x, 0f, vCur.z);
            float curAlong = Vector3.Dot(vCurH, dir);
            float remain = targetAlong - curAlong;

            float stepDeltaV = 0f;
            if (Mathf.Sign(desiredDeltaThisStep) == Mathf.Sign(remain))
                stepDeltaV = Mathf.Clamp(desiredDeltaThisStep, -Mathf.Abs(remain), Mathf.Abs(remain));

            //Convert Δv over dt to acceleration (m/s^2) and apply horizontally
            if (dt > 0f && stepDeltaV != 0f)
            {
                Vector3 a = (dir * stepDeltaV) / dt; // a = Δv / Δt
                //Only horizontal acceleration
                a.y = 0f;
                rb.AddForce(a, ForceMode.Acceleration);
            }

            //Safety clamp for total horizontal speed
            LimitHorizontalSpeed();
        }

        burstRoutine = null;
    }

    void LimitHorizontalSpeed()
    {
        Vector3 v = GetVel();
        Vector3 vH = new Vector3(v.x, 0f, v.z);
        float h = vH.magnitude;
        if (h > maxHorizontalSpeed)
        {
            Vector3 vHClamped = vH.normalized * maxHorizontalSpeed;
            SetVel(new Vector3(vHClamped.x, v.y, vHClamped.z));
        }
    }

    void LockController(float seconds)
    {
        if (controller == null) return;
        if (lockRoutine != null) StopCoroutine(lockRoutine);
        lockRoutine = StartCoroutine(LockRoutine(seconds));
    }

    IEnumerator LockRoutine(float seconds)
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

    Vector3 GetVel()
    {
        return rb.linearVelocity;
    }
    void SetVel(Vector3 v)
    {
        rb.linearVelocity = v; 
    }
}
