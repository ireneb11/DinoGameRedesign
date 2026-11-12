using System.Collections.Generic;

using UnityEngine;

public class BackgroundManager : MonoBehaviour
{
    [Header("ZONA 1 - Primeros 3 minutos")]
    public GameObject[] firstZonePrefabs;

    [Header("ZONA 2 - Despues de 3 minutos")]
    public GameObject[] secondZonePrefabs;

    [Header("Columna")]
    public GameObject columnPrefab;

    [Header("Dimensiones")]
    public float backgroundWidth = 14.22f;
    public float columnWidth = 1.47f;

    [Header("Movimiento")]
    public float scrollSpeed = 5f;
    public float yPosition = 0f;

    [Header("Duracion de la primera zona")]
    public float zoneDuration = 180f;

    private float elapsedTime = 0f;

    private int firstZoneIndex = 0;
    private int secondZoneIndex = 0;

    private float segmentWidth;

    private List<Transform> backgrounds =
        new List<Transform>();

    private Camera mainCamera;
    private float leftEdge;
    private float rightEdge;

    void Start()
    {
        mainCamera = Camera.main;

        if (mainCamera == null || !mainCamera.orthographic)
        {
            Debug.LogError("Necesitas una camara ortografica.");
            enabled = false;
            return;
        }

        if (columnPrefab == null ||
            firstZonePrefabs == null ||
            secondZonePrefabs == null ||
            firstZonePrefabs.Length < 2 ||
            secondZonePrefabs.Length < 2)
        {
            Debug.LogError("Revisa los prefabs del BackgroundManager.");
            enabled = false;
            return;
        }

        segmentWidth = backgroundWidth + columnWidth;

        float halfCameraWidth =
            mainCamera.orthographicSize * mainCamera.aspect;

        leftEdge = mainCamera.transform.position.x -
            halfCameraWidth;

        rightEdge = mainCamera.transform.position.x +
            halfCameraWidth;

        float x = leftEdge;

        while (x < rightEdge + segmentWidth)
        {
            SpawnBackground(x);
            x += segmentWidth;
        }
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;

        for (int i = backgrounds.Count - 1; i >= 0; i--)
        {
            Transform bg = backgrounds[i];

            bg.position +=
                Vector3.left * scrollSpeed * Time.deltaTime;

            if (bg.position.x + segmentWidth < leftEdge)
            {
                Destroy(bg.gameObject);
                backgrounds.RemoveAt(i);
            }
        }

        float rightmost = leftEdge;

        foreach (Transform bg in backgrounds)
        {
            rightmost = Mathf.Max(
                rightmost,
                bg.position.x + segmentWidth
            );
        }

        while (rightmost < rightEdge + segmentWidth)
        {
            SpawnBackground(rightmost);
            rightmost += segmentWidth;
        }
    }

    void SpawnBackground(float x)
    {
        GameObject backgroundPrefab;

        if (elapsedTime < zoneDuration)
        {
            backgroundPrefab =
                firstZonePrefabs[
                    firstZoneIndex % firstZonePrefabs.Length
                ];

            firstZoneIndex++;
        }
        else
        {
            backgroundPrefab =
                secondZonePrefabs[
                    secondZoneIndex % secondZonePrefabs.Length
                ];

            secondZoneIndex++;
        }

        GameObject segment =
            new GameObject("Background_Segment");

        segment.transform.position =
            new Vector3(x, yPosition, 0);

        segment.transform.parent = transform;

        Instantiate(
            backgroundPrefab,
            segment.transform.position,
            Quaternion.identity,
            segment.transform
        );

        Instantiate(
            columnPrefab,
            segment.transform.position +
            Vector3.right * backgroundWidth,
            Quaternion.identity,
            segment.transform
        );

        backgrounds.Add(segment.transform);
    }
}