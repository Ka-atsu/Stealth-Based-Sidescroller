using UnityEngine;

public static class NoiseSystem
{
    public static void MakeNoise(Vector2 position, float radius, NoiseType type)
    {
        Collider2D[] listeners = Physics2D.OverlapCircleAll(position, radius);

        foreach (var col in listeners)
        {
            EnemyHearing hearing = col.GetComponent<EnemyHearing>();

            if (hearing != null)
                hearing.HearNoise(position, type);
        }
    }
}