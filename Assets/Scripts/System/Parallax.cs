using System.Collections.Generic;
using UnityEngine;

public class Parallax : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        [Header("Root")]
        public Transform layerRoot;

        [Range(0f, 1.5f)]
        public float parallaxSpeed = 0.5f;

        [Header("Tiles (left to right)")]
        public List<Transform> tiles = new List<Transform>();

        [HideInInspector] public float tileWidth;
    }

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform trackingTarget;

    [Header("Layers")]
    [SerializeField] private ParallaxLayer[] layers;

    [Header("Pixel Snap")]
    [SerializeField] private bool usePixelSnap = true;
    [SerializeField] private float pixelsPerUnit = 64f;

    private float screenHalfWidth;
    private float lastTrackingX;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("Parallax requires a Camera reference.");
            enabled = false;
            return;
        }

        if (trackingTarget == null)
            trackingTarget = mainCamera.transform;

        if (!mainCamera.orthographic)
            Debug.LogWarning("This parallax setup is intended for an orthographic camera.");

        float screenHeight = mainCamera.orthographicSize * 2f;
        float screenWidth = screenHeight * mainCamera.aspect;
        screenHalfWidth = screenWidth * 0.5f;

        lastTrackingX = trackingTarget.position.x;

        foreach (ParallaxLayer layer in layers)
            InitializeLayer(layer);
    }

    private void LateUpdate()
    {
        if (trackingTarget == null)
            return;

        float trackingX = trackingTarget.position.x;
        float deltaX = trackingX - lastTrackingX;

        foreach (ParallaxLayer layer in layers)
        {
            MoveLayer(layer, deltaX);
            RecycleLayer(layer, trackingX);
        }

        lastTrackingX = trackingX;
    }

    private void InitializeLayer(ParallaxLayer layer)
    {
        if (layer.layerRoot == null)
        {
            Debug.LogWarning("Parallax layer is missing a root transform.");
            return;
        }

        if (layer.tiles == null || layer.tiles.Count < 2)
        {
            Debug.LogWarning($"Layer '{layer.layerRoot.name}' needs at least 2 tiles.");
            return;
        }

        SpriteRenderer sr = layer.tiles[0] != null ? layer.tiles[0].GetComponent<SpriteRenderer>() : null;
        if (sr == null)
        {
            Debug.LogWarning($"First tile on '{layer.layerRoot.name}' has no SpriteRenderer.");
            return;
        }

        layer.tileWidth = sr.bounds.size.x;

        if (layer.tileWidth <= 0f)
        {
            Debug.LogWarning($"Layer '{layer.layerRoot.name}' has invalid tile width.");
            return;
        }

        SortTilesLeftToRight(layer.tiles);
    }

    private void MoveLayer(ParallaxLayer layer, float trackingDeltaX)
    {
        if (layer.layerRoot == null)
            return;

        Vector3 pos = layer.layerRoot.position;
        pos.x -= trackingDeltaX * layer.parallaxSpeed;

        if (usePixelSnap && pixelsPerUnit > 0f)
            pos.x = Mathf.Round(pos.x * pixelsPerUnit) / pixelsPerUnit;

        layer.layerRoot.position = pos;
    }

    private void RecycleLayer(ParallaxLayer layer, float trackingX)
    {
        if (layer.tiles == null || layer.tiles.Count < 2)
            return;

        Transform firstTile = layer.tiles[0];
        Transform lastTile = layer.tiles[layer.tiles.Count - 1];

        if (firstTile == null || lastTile == null)
            return;

        float halfWidth = layer.tileWidth * 0.5f;

        if (trackingX + screenHalfWidth > lastTile.position.x + halfWidth)
        {
            firstTile.localPosition = new Vector3(
                lastTile.localPosition.x + layer.tileWidth,
                firstTile.localPosition.y,
                firstTile.localPosition.z
            );

            layer.tiles.RemoveAt(0);
            layer.tiles.Add(firstTile);
        }
        else if (trackingX - screenHalfWidth < firstTile.position.x - halfWidth)
        {
            lastTile.localPosition = new Vector3(
                firstTile.localPosition.x - layer.tileWidth,
                lastTile.localPosition.y,
                lastTile.localPosition.z
            );

            layer.tiles.RemoveAt(layer.tiles.Count - 1);
            layer.tiles.Insert(0, lastTile);
        }
    }

    private void SortTilesLeftToRight(List<Transform> tiles)
    {
        tiles.Sort((a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            return a.position.x.CompareTo(b.position.x);
        });
    }
}