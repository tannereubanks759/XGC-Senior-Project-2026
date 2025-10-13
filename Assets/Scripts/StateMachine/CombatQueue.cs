using UnityEngine;
using System.Collections.Generic;

public class CombatQueue : MonoBehaviour
{
    [Header("Queue Containers")]
    [Tooltip("The container for the enemies waiting to attack")]
    public List<BasicSkeleton> basicSkeletonQueue;
    [Tooltip("The container for the enemies currently attacking")]
    public List<BasicSkeleton> basicSkeletonsAttacking;

    [Header("Queue Points")]
    [Tooltip("The points around the player in which the queued enemies will idle")]
    public GameObject[] queuePoints;

    private void Start()
    {
        basicSkeletonQueue = new List<BasicSkeleton>();
        basicSkeletonsAttacking = new List<BasicSkeleton>();
    }

    private void FixedUpdate()
    {
        if (basicSkeletonsAttacking.Count <= 2 && basicSkeletonQueue.Count != 0)
        {
            // Gets the last index in the List
            var tempIndex = basicSkeletonsAttacking.Count;

            var tempSkel = basicSkeletonQueue[0];

            basicSkeletonsAttacking[tempIndex] = tempSkel;

            basicSkeletonQueue.RemoveAt(0);
        }

        for (int i = 0; i < basicSkeletonQueue.Count; i++)
        {
            basicSkeletonQueue[i].Agent.destination = queuePoints[i].transform.position;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            var basicSkel = other.GetComponent<BasicSkeleton>();
            if (basicSkel)
            {
                basicSkeletonQueue.Add(basicSkel);
                basicSkel.isInQueue = true;
            }
        }
    }

    public void RemoveAttackingEnemy(BasicSkeleton basicSkeleton)
    {
        basicSkeletonsAttacking.Remove(basicSkeleton);
    }
}