using System.Collections.Generic;
using UnityEngine;

public class Parallax : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        public GameObject layerObject;
        [HideInInspector] public List<Transform> tiles = new List<Transform>();
        [HideInInspector] public float tileWidth;
    }

    [SerializeField] private ParallaxLayer[] layers;

    private Camera mainCamera;
    private float screenHalfWidth;

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

        foreach (ParallaxLayer layer in layers)
        {
            InitializeLayer(layer);
        }
    }

    private void LateUpdate()
    {
        foreach (ParallaxLayer layer in layers)
        {
            RecycleLayer(layer);
        }
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

        Vector3 startPos = layer.layerObject.transform.position;
        Quaternion startRot = layer.layerObject.transform.rotation;

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
            tile.transform.position = new Vector3(
                startPos.x + (i * layer.tileWidth),
                startPos.y,
                startPos.z
            );
            tile.transform.rotation = startRot;
            tile.transform.localScale = Vector3.one;

            SpriteRenderer tileRenderer = tile.AddComponent<SpriteRenderer>();
            tileRenderer.sprite = sprite;
            tileRenderer.sortingLayerID = sortingLayerID;
            tileRenderer.sortingOrder = sortingOrder;
            tileRenderer.sharedMaterial = sharedMaterial;
            tileRenderer.color = color;

            layer.tiles.Add(tile.transform);
        }

        Destroy(sourceRenderer);
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
            firstTile.position = new Vector3(
                lastTile.position.x + layer.tileWidth,
                lastTile.position.y,
                lastTile.position.z
            );

            layer.tiles.RemoveAt(0);
            layer.tiles.Add(firstTile);
        }
        else if (cameraX - screenHalfWidth < firstTile.position.x - halfWidth)
        {
            lastTile.position = new Vector3(
                firstTile.position.x - layer.tileWidth,
                lastTile.position.y,
                lastTile.position.z
            );

            layer.tiles.RemoveAt(layer.tiles.Count - 1);
            layer.tiles.Insert(0, lastTile);
        }
    }
}