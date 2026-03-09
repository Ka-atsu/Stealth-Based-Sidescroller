using UnityEngine;
using System.Collections;

public class SmokeBombController : MonoBehaviour
{
    public GameObject smokeBombPrefab;
    public Transform smokeBombSpawnPoint;

    private bool isSmokeBombActive = false;
    private GameObject currentSmoke;

    public float smokeDuration = 5f;

    public void TriggerSmokeBomb()
    {
        if (isSmokeBombActive) return;

        isSmokeBombActive = true;

        if (smokeBombPrefab != null && smokeBombSpawnPoint != null)
        {
            currentSmoke = Instantiate(
                smokeBombPrefab,
                smokeBombSpawnPoint.position,
                Quaternion.identity
            );
        }

        Debug.Log("Smoke Bomb Triggered!");

        StartCoroutine(DisableSmokeBomb());
    }

    private IEnumerator DisableSmokeBomb()
    {
        yield return new WaitForSeconds(smokeDuration);

        if (currentSmoke != null)
        {
            ParticleSystem ps = currentSmoke.GetComponent<ParticleSystem>();

            if (ps != null)
            {
                ps.Stop(); // stop emitting new particles
                Destroy(currentSmoke, ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(currentSmoke);
            }
        }

        isSmokeBombActive = false;
        Debug.Log("Smoke Bomb Ended");
    }
}