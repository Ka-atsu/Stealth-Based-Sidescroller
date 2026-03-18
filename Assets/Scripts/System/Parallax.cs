using System.Collections.Generic;
using UnityEngine;

public class Parallax : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        public GameObject layerObject;

        [Range(0f, 1.5f)]
        public float parallaxSpeed = 0.5f;

        [HideInInspector] public List<Transform> tiles = new List<Transform>();
        [HideInInspector] public float tileWidth;
    }

    [SerializeField] private ParallaxLayer[] layers;

    private Camera mainCamera;
    private float screenHalfWidth;
    private float lastCameraX;

    private void Start()
    {
        mainCamera = GetComponent<Camera>();

        if (mainCamera == null)
        {
            Debug.LogError("Parallax must be attached to the Camera.");
            return;
        }

        if (!mainCamera.orthographic)
        {
            Debug.LogWarning("This parallax tiler is intended for an orthographic camera.");
        }

        float screenHeight = mainCamera.orthographicSize * 2f;
        float screenWidth = screenHeight * mainCamera.aspect;
        screenHalfWidth = screenWidth * 0.5f;

        lastCameraX = transform.position.x;

        foreach (ParallaxLayer layer in layers)
        {
            InitializeLayer(layer);
        }
    }

    private void LateUpdate()
    {
        float cameraX = transform.position.x;
        float deltaX = cameraX - lastCameraX;

        foreach (ParallaxLayer layer in layers)
        {
            MoveLayer(layer, deltaX);
            RecycleLayer(layer);
        }

        lastCameraX = cameraX;
    }

    private void InitializeLayer(ParallaxLayer layer)
    {
        if (layer.layerObject == null)
            return;

        SpriteRenderer sourceRenderer = layer.layerObject.GetComponent<SpriteRenderer>();
        if (sourceRenderer == null)
        {
            Debug.LogWarning($"'{layer.layerObject.name}' has no SpriteRenderer.");
            return;
        }

        layer.tileWidth = sourceRenderer.bounds.size.x;

        if (layer.tileWidth <= 0f)
        {
            Debug.LogWarning($"'{layer.layerObject.name}' has invalid tile width.");
            return;
        }

        int tilesNeeded = Mathf.CeilToInt((screenHalfWidth * 2f) / layer.tileWidth) + 2;

        Quaternion startRot = layer.layerObject.transform.rotation;
        Vector3 rootPos = layer.layerObject.transform.position;

        Sprite sprite = sourceRenderer.sprite;
        int sortingLayerID = sourceRenderer.sortingLayerID;
        int sortingOrder = sourceRenderer.sortingOrder;
        Material sharedMaterial = sourceRenderer.sharedMaterial;
        Color color = sourceRenderer.color;
        string originalName = layer.layerObject.name;

        layer.tiles.Clear();

        for (int i = 0; i < tilesNeeded; i++)
        {
            GameObject tile = new GameObject($"{originalName}_{i}");
            tile.transform.SetParent(layer.layerObject.transform, false);

            tile.transform.localPosition = new Vector3(i * layer.tileWidth, 0f, 0f);
            tile.transform.localRotation = Quaternion.identity;
            tile.transform.localScale = Vector3.one;

            SpriteRenderer tileRenderer = tile.AddComponent<SpriteRenderer>();
            tileRenderer.sprite = sprite;
            tileRenderer.sortingLayerID = sortingLayerID;
            tileRenderer.sortingOrder = sortingOrder;
            tileRenderer.sharedMaterial = sharedMaterial;
            tileRenderer.color = color;

            layer.tiles.Add(tile.transform);
        }

        layer.layerObject.transform.position = rootPos;
        layer.layerObject.transform.rotation = startRot;

        Destroy(sourceRenderer);
    }

    private void MoveLayer(ParallaxLayer layer, float cameraDeltaX)
    {
        if (layer.layerObject == null)
            return;

        Vector3 pos = layer.layerObject.transform.position;

        pos.x -= cameraDeltaX * layer.parallaxSpeed;

        float pixelsPerUnit = 64f;
        pos.x = Mathf.Round(pos.x * pixelsPerUnit) / pixelsPerUnit;

        layer.layerObject.transform.position = pos;
    }

    private void RecycleLayer(ParallaxLayer layer)
    {
        if (layer.tiles == null || layer.tiles.Count < 2)
            return;

        float cameraX = transform.position.x;

        Transform firstTile = layer.tiles[0];
        Transform lastTile = layer.tiles[layer.tiles.Count - 1];
        float halfWidth = layer.tileWidth * 0.5f;

        if (cameraX + screenHalfWidth > lastTile.position.x + halfWidth)
        {
            firstTile.localPosition = new Vector3(
                lastTile.localPosition.x + layer.tileWidth,
                firstTile.localPosition.y,
                firstTile.localPosition.z
            );

            layer.tiles.RemoveAt(0);
            layer.tiles.Add(firstTile);
        }
        else if (cameraX - screenHalfWidth < firstTile.position.x - halfWidth)
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
}