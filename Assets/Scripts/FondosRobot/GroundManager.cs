
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class GroundManager : MonoBehaviour
{
    [System.Serializable]
    public class GroundElement
    {
        public Transform transform;
        public float width;
    }

    [Header("VIGAS")]
    public GameObject[] beamPrefabs;
    public float beamWidth = 4.76f;
    public float beamY = 3.26f;

    [Header("BALDOSAS")]
    public GameObject[] tilePrefabs;
    public float[] tileWidths;
    public float tileY = -3f;

    [Header("MOVIMIENTO")]
    public float scrollSpeed = 5f;

    private List<GroundElement> beams =
        new List<GroundElement>();

    private List<GroundElement> tiles =
        new List<GroundElement>();

    private int beamIndex = 0;

    private Camera mainCamera;
    private float leftEdge;
    private float rightEdge;

    void Start()
    {
        mainCamera = Camera.main;

        if (mainCamera == null || !mainCamera.orthographic)
        {
            UnityEngine.Debug.LogError("Necesitas una camara ortografica.");
            enabled = false;
            return;
        }

        if (beamPrefabs.Length == 0 ||
            tilePrefabs.Length == 0 ||
            tileWidths.Length != tilePrefabs.Length)
        {
            UnityEngine.Debug.LogError("Revisa los prefabs y anchuras.");
            enabled = false;
            return;
        }

        leftEdge = mainCamera.transform.position.x -
            mainCamera.orthographicSize * mainCamera.aspect;

        rightEdge = mainCamera.transform.position.x +
            mainCamera.orthographicSize * mainCamera.aspect;

        // Generar vigas iniciales
        float beamX = leftEdge;

        while (beamX < rightEdge + beamWidth)
        {
            SpawnBeam(beamX);
            beamX += beamWidth;
        }

        // Generar baldosas iniciales
        float tileX = leftEdge;

        while (tileX < rightEdge + GetMaxTileWidth())
        {
            float width = SpawnTile(tileX);
            tileX += width;
        }
    }

    void Update()
    {
        MoveBeams();
        MoveTiles();
    }

    // ---------------- VIGAS ----------------

    void MoveBeams()
    {
        MoveElements(beams, beamWidth, true);
    }

    void SpawnBeam(float x)
    {
        GameObject prefab =
            beamPrefabs[beamIndex % beamPrefabs.Length];

        beamIndex++;

        GameObject newBeam = Instantiate(
            prefab,
            new Vector3(x, beamY, 0),
            Quaternion.identity,
            transform
        );

        beams.Add(new GroundElement
        {
            transform = newBeam.transform,
            width = beamWidth
        });
    }

    // ---------------- BALDOSAS ----------------

    void MoveTiles()
    {
        MoveElements(tiles, GetMaxTileWidth(), false);
    }

    float SpawnTile(float x)
    {
        int randomIndex = UnityEngine.Random.Range(0, tilePrefabs.Length);

        float width = tileWidths[randomIndex];

        GameObject newTile = Instantiate(
            tilePrefabs[randomIndex],
            new Vector3(x, tileY, 0),
            Quaternion.identity,
            transform
        );

        tiles.Add(new GroundElement
        {
            transform = newTile.transform,
            width = width
        });

        return width;
    }

    float GetMaxTileWidth()
    {
        float maxWidth = 0f;

        foreach (float width in tileWidths)
        {
            maxWidth = Mathf.Max(maxWidth, width);
        }

        return maxWidth;
    }

    // ---------------- MOVIMIENTO Y RECICLAJE ----------------

    void MoveElements(
        List<GroundElement> elements,
        float maxWidth,
        bool isBeam)
    {
        for (int i = elements.Count - 1; i >= 0; i--)
        {
            GroundElement element = elements[i];

            element.transform.position +=
                Vector3.left * scrollSpeed * Time.deltaTime;

            if (element.transform.position.x +
                element.width < leftEdge)
            {
                Destroy(element.transform.gameObject);
                elements.RemoveAt(i);
            }
        }

        float rightmost = leftEdge;

        foreach (GroundElement element in elements)
        {
            rightmost = Mathf.Max(
                rightmost,
                element.transform.position.x + element.width
            );
        }

        while (rightmost < rightEdge + maxWidth)
        {
            if (isBeam)
            {
                SpawnBeam(rightmost);
                rightmost += beamWidth;
            }
            else
            {
                float width = SpawnTile(rightmost);
                rightmost += width;
            }
        }
    }
}
