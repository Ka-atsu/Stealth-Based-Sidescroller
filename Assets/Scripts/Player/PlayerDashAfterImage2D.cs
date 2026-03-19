using UnityEngine;

public class PlayerDashAfterImage2D : MonoBehaviour
{
    public float life = 0.12f;

    SpriteRenderer sr;
    Color startColor;
    float timer;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Setup(Sprite sprite, Vector3 scale, bool flipX, Color color)
    {
        sr.sprite = sprite;
        transform.localScale = scale;
        sr.flipX = flipX;
        sr.color = color;

        startColor = color;
        timer = life;
    }

    void Update()
    {
        timer -= Time.deltaTime;

        float t = Mathf.Clamp01(timer / life);
        Color c = startColor;
        c.a = startColor.a * t;
        sr.color = c;

        if (timer <= 0f)
            Destroy(gameObject);
    }
}