using RayFire;
using UnityEngine;

public class DemoRayfireOnStart : MonoBehaviour
{
    public RayfireRigid rayfire;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rayfire.Demolish();
    }

}
