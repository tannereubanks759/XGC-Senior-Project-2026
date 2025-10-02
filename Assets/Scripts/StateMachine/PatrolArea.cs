/*
 * PatrolArea.cs
 * 
 * This script defines a patrol area for enemies in the game.
 * It provides a method to get a random valid point within the area
 * that is also on the NavMesh, which enemies can use for patrolling.
 * 
 * By: Matthew Bolger
*/

using UnityEngine;
using UnityEngine.AI;

// Defines a circular area that enemies can patrol within.
public class PatrolArea : MonoBehaviour
{
    [Header("Patrol Settings")]
    [Tooltip("The radius which marks the borders of where enemies will patrol around")]
    // Radius of the patrol area around this GameObject's position.
    [SerializeField] private float patrolRadius = 5f;

    // Returns a random point within the patrol radius that is valid on the NavMesh.
    public Vector3 GetRandomPoint()
    {
        // Pick a random point inside a unit circle and scale by radius.
        Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;

        // Convert 2D circle to 3D world coordinates (Y stays the same as the object's position).
        Vector3 randomPoint = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

        // Snap the random point to the NavMesh to ensure the enemy can reach it.
        if (NavMesh.SamplePosition(randomPoint, out var hit, 1f, NavMesh.AllAreas))
        {
            //Debug.Log(hit.position);
            return hit.position;
        }

        //Debug.Log(transform.position);
        // If unable to find a valid NavMesh point, fallback to the patrol area's center.
        return transform.position;
    }

    // Draws the patrol area in the editor for visualization.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, patrolRadius);
    }
}
