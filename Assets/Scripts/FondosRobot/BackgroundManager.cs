using System.Collections.Generic;
using UnityEngine;

public class BackgroundManager : MonoBehaviour
{
    [Header("ZONA 1 ")]
    public GameObject windowPrefab;
    public GameObject hallway1Prefab;
    public GameObject hallway2Prefab;
    public GameObject columnZone1Prefab;

    [Header("ZONA 2 ")]
    public GameObject laboratoryPrefab;
    public GameObject hallway3Prefab;
    public GameObject hallway4Prefab;
    public GameObject columnZone2Prefab;

    [Header("Dimensiones")]
    public float backgroundWidth = 14.22f;
    public float columnWidth = 1.2f;

    [Header("Movimiento")]
    public float scrollSpeed = 5f;
    public float yPosition = 0f;

    [Header("Duracion Zona 1 (segundos)")]
    public float zoneDuration = 240f;

    private float elapsedTime = 0f;
    public bool IsSecondZone => elapsedTime >= zoneDuration;

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

        if (windowPrefab == null ||
            hallway1Prefab == null ||
            hallway2Prefab == null ||
            laboratoryPrefab == null ||
            hallway3Prefab == null ||
            hallway4Prefab == null ||
            columnZone1Prefab == null ||
            columnZone2Prefab == null)
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
        GameObject columnPrefab;

        if (elapsedTime < zoneDuration)
        {
            // ZONA 1

            if (firstZoneIndex == 0)
            {
                backgroundPrefab = windowPrefab;
            }
            else if (firstZoneIndex == 1)
            {
                backgroundPrefab = hallway1Prefab;
            }
            else
            {
                backgroundPrefab = hallway2Prefab;
            }

            columnPrefab = columnZone1Prefab;

            firstZoneIndex++;
        }
        else
        {
            // ZONA 2

            if (secondZoneIndex == 0)
            {
                backgroundPrefab = laboratoryPrefab;
            }
            else if (secondZoneIndex == 1)
            {
                backgroundPrefab = hallway3Prefab;
            }
            else
            {
                backgroundPrefab = hallway4Prefab;
            }

            columnPrefab = columnZone2Prefab;

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