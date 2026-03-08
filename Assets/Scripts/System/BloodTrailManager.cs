using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BloodTrailPoint
{
    public int id;
    public Vector3 position;
    public float expireTime;

    public BloodTrailPoint(int id, Vector3 position, float expireTime)
    {
        this.id = id;
        this.position = position;
        this.expireTime = expireTime;
    }
}

public class BloodTrailManager : MonoBehaviour
{
    public Transform player;
    public float trailUpdateInterval = 0.5f;
    public float minMoveDistance = 0.3f;
    public float bloodLifetime = 5f;

    public List<BloodTrailPoint> bloodTrailPoints = new List<BloodTrailPoint>();

    private float timeSinceLastUpdate = 0f;
    private Vector3 lastRecordedPosition;
    private PlayerBleeding playerBleeding;
    private int nextBloodId = 0;

    void Start()
    {
        if (player != null)
        {
            playerBleeding = player.GetComponent<PlayerBleeding>();
            lastRecordedPosition = player.position;
        }

        bloodTrailPoints.Clear();
    }

    void Update()
    {
        CleanupExpiredBlood();

        if (player == null || playerBleeding == null) return;
        if (!playerBleeding.IsBleeding) return;

        timeSinceLastUpdate += Time.deltaTime;

        if (timeSinceLastUpdate >= trailUpdateInterval)
        {
            float dist = Vector3.Distance(player.position, lastRecordedPosition);

            if (dist >= minMoveDistance)
            {
                AddBloodPoint(player.position);
                lastRecordedPosition = player.position;
            }

            timeSinceLastUpdate = 0f;
        }
    }

    private void AddBloodPoint(Vector3 position)
    {
        BloodTrailPoint point = new BloodTrailPoint(
            nextBloodId++,
            position,
            Time.time + bloodLifetime
        );

        bloodTrailPoints.Add(point);
    }

    private void CleanupExpiredBlood()
    {
        for (int i = bloodTrailPoints.Count - 1; i >= 0; i--)
        {
            if (Time.time >= bloodTrailPoints[i].expireTime)
            {
                bloodTrailPoints.RemoveAt(i);
            }
        }
    }

    public BloodTrailPoint GetBloodPointById(int id)
    {
        for (int i = 0; i < bloodTrailPoints.Count; i++)
        {
            if (bloodTrailPoints[i].id == id)
                return bloodTrailPoints[i];
        }

        return null;
    }

    public bool IsBloodValid(int id)
    {
        return GetBloodPointById(id) != null;
    }
}