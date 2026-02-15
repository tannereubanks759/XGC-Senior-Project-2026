using UnityEngine;

public class RotateWithHead : MonoBehaviour
{
    public GameObject player;
    public Vector3 playerOffset = new Vector3(0f, 1f, 0f);
    public float xOffset = 0f; // optional manual correction

    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");
    }

    void Update()
    {
        if (player == null) return;

        Vector3 targetWorld = player.transform.position + playerOffset;

        // Direction in local/parent space
        Vector3 localDir;
        if (transform.parent != null)
            localDir = transform.parent.InverseTransformPoint(targetWorld) - transform.localPosition;
        else
            localDir = targetWorld - transform.position;

        // Flip sign so higher player => looks upward
        float xAngle = -Mathf.Atan2(localDir.y, localDir.z) * Mathf.Rad2Deg + xOffset;

        Vector3 localEuler = transform.localEulerAngles;
        transform.localRotation = Quaternion.Euler(xAngle, localEuler.y, localEuler.z);
    }
}
