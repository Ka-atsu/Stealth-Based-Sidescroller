using UnityEngine;
using Unity.Cinemachine;

public class CameraImpulseSource : MonoBehaviour
{
    public static CameraImpulseSource Instance;

    private CinemachineImpulseSource impulseSource;

    private void Awake()
    {
        Instance = this;
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    public void Shake(Vector2 direction, float force)
    {
        if (impulseSource == null) return;

        direction.y *= 0.3f; // 🔥 reduces vertical shake (feels better)
        direction.Normalize();

        Vector3 impulse = new Vector3(direction.x, direction.y, 0f) * force;
        impulseSource.GenerateImpulse(impulse);
    }

    public void Shake(float force)
    {
        Shake(Vector2.up, force);
    }
}