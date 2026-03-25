using UnityEngine;

public class PlayerDashAfterImage2D : MonoBehaviour
{
    private SpriteRenderer sr;
    private float lifeTime = 0.2f;
    private float timer;

    private Color startColor;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Setup(Sprite sprite, Vector3 worldScale, bool flipX, Color color)
    {
        sr.sprite = sprite;
        transform.localScale = worldScale;
        sr.flipX = flipX;

        startColor = color;
        sr.color = color;

        timer = lifeTime;
    }

    void Update()
    {
        timer -= Time.deltaTime;

        float t = timer / lifeTime;

        // Fade out
        sr.color = new Color(startColor.r, startColor.g, startColor.b, t * startColor.a);

        if (timer <= 0f)
            Destroy(gameObject);
    }
}