using UnityEngine;
using UnityEngine.AI;

public class PatrolArea : MonoBehaviour
{
    [Header("Patrol Settings")]
    public float patrolRadius = 5f;

    // Get a random point inside the patrol area and on the NavMesh
    public Vector3 GetRandomPoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
        Vector3 randomPoint = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

        // Snap to NavMesh
        if (NavMesh.SamplePosition(randomPoint, out var hit, 1f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return transform.position; // fallback
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, patrolRadius);
    }
}
