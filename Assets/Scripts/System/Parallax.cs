using UnityEngine;

public class Parallax : MonoBehaviour
{
    [SerializeField] private Transform cam;
    [SerializeField, Range(0f, 1f)] private float parallaxEffect = 0.5f;

    private Transform _tr;
    private float _startPosX;
    private float _spriteWidth;

    private void Awake()
    {
        _tr = transform;

        if (cam == null && Camera.main != null)
            cam = Camera.main.transform;
    }

    private void Start()
    {
        _startPosX = _tr.position.x;

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            _spriteWidth = sr.bounds.size.x;
        }
        else
        {
            Debug.LogError($"No SpriteRenderer found in children of {gameObject.name}", this);
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (cam == null || _spriteWidth <= 0f)
            return;

        float camX = cam.position.x;
        float parallaxOffset = camX * parallaxEffect;
        float relativeCamX = camX * (1f - parallaxEffect);

        Vector3 pos = _tr.position;
        pos.x = _startPosX + parallaxOffset;
        _tr.position = pos;

        while (relativeCamX > _startPosX + _spriteWidth)
        {
            _startPosX += _spriteWidth;
        }

        while (relativeCamX < _startPosX - _spriteWidth)
        {
            _startPosX -= _spriteWidth;
        }
    }
}