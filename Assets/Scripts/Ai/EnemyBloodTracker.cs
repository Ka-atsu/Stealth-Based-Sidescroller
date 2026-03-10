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

        // Already following a trail, do not rebuild it every frame
        if (queuedBloodIds.Count > 0)
            return;

        BloodTrailPoint startPoint = FindNearestNearbyBloodPoint();

        if (startPoint == null)
            return;

        BuildTrailFrom(startPoint.id);
    }

    private BloodTrailPoint FindNearestNearbyBloodPoint()
    {
        BloodTrailPoint bestPoint = null;
        float bestDistance = Mathf.Infinity;

        foreach (BloodTrailPoint point in bloodTrailManager.bloodTrailPoints)
        {
            if (visitedBloodSet.Contains(point.id))
                continue;

            float dist = Vector2.Distance(transform.position, point.position);

            if (dist > bloodDetectRadius)
                continue;

            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestPoint = point;
            }
        }

        return bestPoint;
    }

    private void BuildTrailFrom(int startBloodId)
    {
        queuedBloodIds.Clear();
        queuedBloodSet.Clear();

        List<BloodTrailPoint> orderedPoints = new List<BloodTrailPoint>(bloodTrailManager.bloodTrailPoints);

        // Assumes lower id = older blood, higher id = newer blood
        orderedPoints.Sort((a, b) => a.id.CompareTo(b.id));

        Vector3 lastQueuedPosition = Vector3.zero;
        bool hasLastQueued = false;

        foreach (BloodTrailPoint point in orderedPoints)
        {
            if (point.id < startBloodId)
                continue;

            if (visitedBloodSet.Contains(point.id))
                continue;

            if (hasLastQueued &&
                Vector3.Distance(lastQueuedPosition, point.position) < minDistanceBetweenBloodPoints)
                continue;

            queuedBloodIds.Enqueue(point.id);
            queuedBloodSet.Add(point.id);

            lastQueuedPosition = point.position;
            hasLastQueued = true;
        }
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
            return;

        int reachedId = queuedBloodIds.Dequeue();
        queuedBloodSet.Remove(reachedId);
        visitedBloodSet.Add(reachedId);
    }
}