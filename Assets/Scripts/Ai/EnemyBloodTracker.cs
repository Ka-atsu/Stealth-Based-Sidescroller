using System.Collections.Generic;
using UnityEngine;

public class EnemyBloodTracker : MonoBehaviour
{
    public float bloodDetectRadius = 5f;
    public float minDistanceBetweenBloodPoints = 0.3f;
    public float maxDistanceFromEnemy = 15f;
    public BloodTrailManager bloodTrailManager;

    private Queue<int> queuedBloodIds = new Queue<int>();
    private HashSet<int> queuedBloodSet = new HashSet<int>();
    private HashSet<int> visitedBloodSet = new HashSet<int>();

    void Start()
    {
        if (bloodTrailManager == null)
            bloodTrailManager = FindFirstObjectByType<BloodTrailManager>();
    }

    void Update()
    {
        CleanupTrackedBlood();
    }

    public void DetectNearbyBlood()
    {
        if (bloodTrailManager == null) return;

        foreach (BloodTrailPoint point in bloodTrailManager.bloodTrailPoints)
        {
            if (Vector2.Distance(transform.position, point.position) > bloodDetectRadius)
                continue;

            if (queuedBloodSet.Contains(point.id) || visitedBloodSet.Contains(point.id))
                continue;

            if (HasQueuedBloodCloseTo(point.position))
                continue;

            RegisterBloodPoint(point.id);
        }
    }

    private bool HasQueuedBloodCloseTo(Vector3 position)
    {
        foreach (int id in queuedBloodIds)
        {
            BloodTrailPoint point = bloodTrailManager.GetBloodPointById(id);
            if (point == null) continue;

            if (Vector3.Distance(point.position, position) < minDistanceBetweenBloodPoints)
                return true;
        }

        return false;
    }

    private void RegisterBloodPoint(int bloodId)
    {
        queuedBloodIds.Enqueue(bloodId);
        queuedBloodSet.Add(bloodId);

        BloodTrailPoint point = bloodTrailManager.GetBloodPointById(bloodId);
    }

    private void CleanupTrackedBlood()
    {
        if (bloodTrailManager == null) return;

        Queue<int> rebuiltQueue = new Queue<int>();
        queuedBloodSet.Clear();

        foreach (int id in queuedBloodIds)
        {
            BloodTrailPoint point = bloodTrailManager.GetBloodPointById(id);

            if (point == null)
                continue;

            if (Vector2.Distance(transform.position, point.position) > maxDistanceFromEnemy)
                continue;

            rebuiltQueue.Enqueue(id);
            queuedBloodSet.Add(id);
        }

        queuedBloodIds = rebuiltQueue;

        visitedBloodSet.RemoveWhere(id => !bloodTrailManager.IsBloodValid(id));
    }

    public bool HasBloodTarget()
    {
        CleanupTrackedBlood();
        return queuedBloodIds.Count > 0;
    }

    public Vector3 GetBloodTargetPosition()
    {
        CleanupTrackedBlood();

        if (queuedBloodIds.Count == 0)
            return transform.position;

        BloodTrailPoint point = bloodTrailManager.GetBloodPointById(queuedBloodIds.Peek());

        if (point == null)
            return transform.position;

        return point.position;
    }

    public void MoveToNextBloodTarget()
    {
        CleanupTrackedBlood();

        if (queuedBloodIds.Count == 0)
        { 
            return;
        }

        int reachedId = queuedBloodIds.Dequeue();
        queuedBloodSet.Remove(reachedId);
        visitedBloodSet.Add(reachedId);

        BloodTrailPoint point = bloodTrailManager.GetBloodPointById(reachedId);
    }
}