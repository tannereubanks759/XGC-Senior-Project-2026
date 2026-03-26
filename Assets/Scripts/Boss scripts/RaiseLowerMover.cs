using System.Collections;
using UnityEngine;

public class RaiseLowerMover : MonoBehaviour
{
    [Header("Positions")]
    [Tooltip("Local position when lowered.")]
    public Vector3 bottomLocalPos = new Vector3(0f, 0f, 0f);

    [Tooltip("Local position when raised.")]
    public Vector3 topLocalPos = new Vector3(0f, 2f, 0f);

    [Header("Movement")]
    [Min(0.01f)]
    public float moveTime = 0.5f;

    [Tooltip("Optional easing for the movement.")]
    public AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("State")]
    public bool isRaised;

    public GameObject InkAttacks;

    Coroutine _moveRoutine;

    void Reset()
    {
        isRaised = false;
    }

    void Start()
    {
        // Initialize state based on current position (with a small tolerance)
        isRaised = (Vector3.Distance(transform.localPosition, topLocalPos) <= 0.001f);
        InkAttacks.SetActive(false);
    }


    // Call this to move from bottom -> top
    public void Raise()
    {
        if (isRaised) return;
        InkAttacks.SetActive(true);
        StartMove(topLocalPos, true);
    }

    // Call this to move from top -> bottom
    public void Lower()
    {
        if (!isRaised) return;
        InkAttacks.SetActive(false);
        StartMove(bottomLocalPos, true);
    }

    void StartMove(Vector3 targetLocalPos, bool finalRaisedState)
    {
        if (_moveRoutine != null)
            StopCoroutine(_moveRoutine);

        _moveRoutine = StartCoroutine(MoveToLocal(targetLocalPos, finalRaisedState));
    }

    IEnumerator MoveToLocal(Vector3 targetLocalPos, bool finalRaisedState)
    {
        Vector3 start = transform.localPosition;
        float t = 0f;

        while (t < moveTime)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / moveTime);
            float k = ease != null ? ease.Evaluate(u) : u;

            transform.localPosition = Vector3.LerpUnclamped(start, targetLocalPos, k);
            yield return null;
        }

        transform.localPosition = targetLocalPos;
        isRaised = finalRaisedState;

        _moveRoutine = null;
    }
}