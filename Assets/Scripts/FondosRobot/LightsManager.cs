using System.Collections.Generic;
using UnityEngine;

public class LightsManager : MonoBehaviour
{
    [Header("ZONA 1")]
    public GameObject blueLightPrefab;
    public GameObject orangeLightPrefab;

    [Header("ZONA 2")]
    public GameObject greenLightPrefab;
    public GameObject pinkLightPrefab;

    [Header("GENERACION")]
    public float distanceBetweenLights = 4f;
    [Range(0f, 1f)]
    public float spawnChance = 0.6f;
    public float lightY = 0f;

    [Header("MOVIMIENTO")]
    public float scrollSpeed = 5f;

    private Camera mainCamera;
    private BackgroundManager backgroundManager;

    private float rightEdge;
    private float leftEdge;
    private float nextSpawnX;

    private List<LightObject> activeLights = new List<LightObject>();
    private Dictionary<GameObject, Queue<GameObject>> pools =
        new Dictionary<GameObject, Queue<GameObject>>();

    private class LightObject
    {
        public GameObject prefab;
        public GameObject obj;
    }

    void Start()
    {
        mainCamera = Camera.main;
        backgroundManager = FindFirstObjectByType<BackgroundManager>();

        if (mainCamera == null || backgroundManager == null)
        {
            Debug.LogError("Falta la Camera o el BackgroundManager.");
            enabled = false;
            return;
        }

        float halfWidth = mainCamera.orthographicSize * mainCamera.aspect;

        leftEdge = mainCamera.transform.position.x - halfWidth;
        rightEdge = mainCamera.transform.position.x + halfWidth;

        nextSpawnX = rightEdge + distanceBetweenLights;
    }

    void Update()
    {
        MoveLights();
        SpawnSlots();
    }

    void MoveLights()
    {
        for (int i = activeLights.Count - 1; i >= 0; i--)
        {
            LightObject light = activeLights[i];

            light.obj.transform.position +=
                Vector3.left * scrollSpeed * Time.deltaTime;

            float width = GetWidth(light.obj);

            if (light.obj.transform.position.x + width < leftEdge)
            {
                ReturnToPool(light.prefab, light.obj);
                activeLights.RemoveAt(i);
            }
        }
    }

    void SpawnSlots()
    {
        while (nextSpawnX < rightEdge + distanceBetweenLights)
        {
            if (Random.value <= spawnChance)
            {
                GameObject prefab = GetRandomLight();

                if (prefab != null)
                {
                    GameObject obj = GetFromPool(prefab);

                    obj.transform.position = new Vector3(
                        nextSpawnX,
                        lightY,
                        0f
                    );

                    obj.SetActive(true);

                    activeLights.Add(new LightObject
                    {
                        prefab = prefab,
                        obj = obj
                    });
                }
            }

            nextSpawnX += distanceBetweenLights;
        }
    }

    GameObject GetRandomLight()
    {
        GameObject[] lights;

        if (backgroundManager.IsSecondZone)
        {
            lights = new GameObject[]
            {
                greenLightPrefab,
                pinkLightPrefab
            };
        }
        else
        {
            lights = new GameObject[]
            {
                blueLightPrefab,
                orangeLightPrefab
            };
        }

        return lights[Random.Range(0, lights.Length)];
    }

    GameObject GetFromPool(GameObject prefab)
    {
        if (!pools.ContainsKey(prefab))
        {
            pools[prefab] = new Queue<GameObject>();
        }

        if (pools[prefab].Count > 0)
        {
            return pools[prefab].Dequeue();
        }

        GameObject obj = Instantiate(prefab, transform);
        obj.SetActive(false);

        return obj;
    }

    void ReturnToPool(GameObject prefab, GameObject obj)
    {
        obj.SetActive(false);
        pools[prefab].Enqueue(obj);
    }

    float GetWidth(GameObject obj)
    {
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();

        if (sr != null)
            return sr.bounds.size.x;

        return 0f;
    }
}