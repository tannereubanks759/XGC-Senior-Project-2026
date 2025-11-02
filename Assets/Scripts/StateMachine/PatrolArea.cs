/*
 * PatrolArea.cs
 * 
 * This script defines a patrol area for enemies in the game.
 * It provides a method to get a random valid point within the area
 * that is also on the NavMesh, which enemies can use for patrolling.
 * 
 * By: Matthew Bolger
*/
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Defines a circular area that enemies can patrol within.
public class PatrolArea : MonoBehaviour
{
    [Header("Patrol Settings")]
    [Tooltip("The radius which marks the borders of where enemies will patrol around")]
    // Radius of the patrol area around this GameObject's position.
    public float patrolRadius = 5f;
    public List<GameObject> enemies = new();

    private void Start()
    {
        enemies = GetNearby(this.transform.position, patrolRadius);
    }

    // Returns a random point within the patrol radius that is valid on the NavMesh.
    public Vector3 GetRandomPoint(int recurIndex, BaseEnemyAI enemy)
    {
        // Pick a random point inside a unit circle and scale by radius.
        Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;

        // Convert 2D circle to 3D world coordinates (Y stays the same as the object's position).
        Vector3 randomPoint = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

        // Snap the random point to the NavMesh to ensure the enemy can reach it.
        if (NavMesh.SamplePosition(randomPoint, out var hit, 1f, NavMesh.AllAreas))
        {
            if (Vector3.Distance(hit.position, enemy.transform.position) > 5f)
            {
                return hit.position;
            }
        }

        if (recurIndex > 0) return GetRandomPoint(recurIndex - 1, enemy);

        //Debug.Log(transform.position);
        // If unable to find a valid NavMesh point, fallback to the patrol area's center.
        return transform.position;
    }

    public void PlayerSeen()
    {
        foreach (var e in enemies)
        {
            var beAI = e.GetComponent<BaseEnemyAI>();
            beAI.usingLoS = false;

            beAI.fieldOfView = 360f;
            beAI.detectionRadius = 25f;
        }
    }

    public List<GameObject> GetNearby(Vector3 origin, float radius)
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, patrolRadius);
        if (cols.Length > 0)
        {
            List<GameObject> nearby = new();
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i].GetComponent<BasicSkeleton>() != null)
                {
                    nearby.Add(cols[i].gameObject);
                }
            }
            return nearby;
        }
        return null;
    }

    // Draws the patrol area in the editor for visualization.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, patrolRadius);
    }
}
