using UnityEngine;

public class ExplosionTriggerForwarder : MonoBehaviour
{
    public ExplosiveBarrel owner;

    private void OnTriggerEnter(Collider other)
    {
        if (owner != null)
            owner.HandleExplosionTriggerEnter(other);
    }
}