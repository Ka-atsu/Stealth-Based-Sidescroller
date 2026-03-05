using UnityEngine;

public class PlayerNoiseEmitter2D : MonoBehaviour
{
    [Header("Stealth")]
    public bool isHidden = false;

    public void Emit(float radius, NoiseType type)
    {
        if (isHidden) return;
        NoiseSystem.MakeNoise(transform.position, radius, type);
    }
}