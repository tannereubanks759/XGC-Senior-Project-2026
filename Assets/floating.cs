using UnityEngine;

public class floating : MonoBehaviour
{
    public float Speed;
    public float height;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private Vector3 startPos;
    private void Start()
    {
        startPos = transform.position;
    }
    private void Update()
    {
        float yPos = startPos.y + Mathf.Sin(Time.time * Speed) *height;
        transform.position = new Vector3(transform.position.x, yPos, transform.position.z);
    }
}
