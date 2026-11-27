

using System.Collections.Generic;
using UnityEngine;

public class GroundManager : MonoBehaviour
{
    [System.Serializable]
    public class GroundElement
    {
        public Transform transform;
        public float width;
        public GameObject prefab;
    }

    [System.Serializable]
    public class DecorationRow
    {
        [Header("PREFABS ZONA 1")]
        public GameObject[] zone1Prefabs = new GameObject[2];
        public float[] zone1Widths = new float[2];

        [Header("PREFABS ZONA 2")]
        public GameObject[] zone2Prefabs = new GameObject[2];
        public float[] zone2Widths = new float[2];

        [Header("POSICION Y ESPACIO")]
        public float yPosition = 0f;
        public float gap = 1f;

        [System.NonSerialized]
        public List<GroundElement> elements = new List<GroundElement>();

        [System.NonSerialized]
        public int sequenceIndex = 0;

        [System.NonSerialized]
        public bool configured = false;
    }

    [Header("VIGAS")]
    public GameObject[] beamPrefabs;
    public float beamWidth = 4.76f;
    public float beamY = 3.26f;

    [Header("BALDOSAS")]
    public GameObject[] tilePrefabs;
    public float[] tileWidths;
    public float tileY = -3f;

    [Header("DECORACION FILA 1")]
    public DecorationRow decorationRow1 = new DecorationRow();

    [Header("DECORACION FILA 2")]
    public DecorationRow decorationRow2 = new DecorationRow();

    [Header("LUCES - ZONA 1")]
    public GameObject blueLightPrefab;
    public GameObject orangeLightPrefab;

    [Header("LUCES - ZONA 2")]
    public GameObject greenLightPrefab;
    public GameObject pinkLightPrefab;

    [Header("GENERACION DE LUCES")]
    public float lightSpacing = 5f;
    [Range(0f, 1f)]
    public float lightSpawnChance = 0.6f;
    public float lightWidth = 1f;
    public float lightY = 2f;

    [Header("MOVIMIENTO")]
    public float scrollSpeed = 5f;

    private List<GroundElement> beams = new List<GroundElement>();
    private List<GroundElement> tiles = new List<GroundElement>();
    private List<GroundElement> lights = new List<GroundElement>();

    // Pool compartido para todos los objetos
    private Dictionary<GameObject, Stack<GameObject>> objectPools =
        new Dictionary<GameObject, Stack<GameObject>>();

    private int beamIndex = 0;

    private Camera mainCamera;
    private BackgroundManager backgroundManager;

    private float leftEdge;
    private float rightEdge;
    private float maxTileWidth;
    private float nextLightX;

    private bool previousZone = false;

    void Start()
    {
        mainCamera = Camera.main;
        backgroundManager = FindFirstObjectByType<BackgroundManager>();

        if (mainCamera == null || !mainCamera.orthographic)
        {
            Debug.LogError("Necesitas una camara ortografica.");
            enabled = false;
            return;
        }

        if (backgroundManager == null)
        {
            Debug.LogError("No se encuentra BackgroundManager.");
            enabled = false;
            return;
        }

        if (beamPrefabs == null || tilePrefabs == null ||
            tileWidths == null ||
            beamPrefabs.Length == 0 ||
            tilePrefabs.Length == 0 ||
            tileWidths.Length != tilePrefabs.Length)
        {
            Debug.LogError("Revisa los prefabs y anchuras.");
            enabled = false;
            return;
        }

        foreach (float width in tileWidths)
        {
            if (width <= 0f)
            {
                Debug.LogError("Las anchuras de las baldosas deben ser positivas.");
                enabled = false;
                return;
            }
        }

        ValidateDecorationRow(decorationRow1, "Fila 1");
        ValidateDecorationRow(decorationRow2, "Fila 2");

        maxTileWidth = GetMaxTileWidth();

        float halfCameraWidth =
            mainCamera.orthographicSize * mainCamera.aspect;

        leftEdge = mainCamera.transform.position.x - halfCameraWidth;
        rightEdge = mainCamera.transform.position.x + halfCameraWidth;

        previousZone = backgroundManager.IsSecondZone;

        // GENERAR VIGAS INICIALES
        float beamX = leftEdge;

        while (beamX < rightEdge + beamWidth)
        {
            SpawnBeam(beamX);
            beamX += beamWidth;
        }

        // GENERAR BALDOSAS INICIALES
        float tileX = leftEdge;

        while (tileX < rightEdge + maxTileWidth)
        {
            float width = SpawnTile(tileX);
            tileX += width;
        }

        // GENERAR DECORACIONES INICIALES
        if (decorationRow1.configured)
            InitializeDecorationRow(decorationRow1);

        if (decorationRow2.configured)
            InitializeDecorationRow(decorationRow2);

        // GENERAR POSIBLES LUCES INICIALES
        nextLightX = leftEdge;

        while (nextLightX < rightEdge + lightSpacing)
        {
            SpawnLightSlot(nextLightX, previousZone);
            nextLightX += lightSpacing;
        }
    }

    void Update()
    {
        bool currentZone = backgroundManager.IsSecondZone;

        // Detectar cambio de zona
        if (currentZone != previousZone)
        {
            decorationRow1.sequenceIndex = 0;
            decorationRow2.sequenceIndex = 0;

            previousZone = currentZone;
        }

        MoveBeams();
        MoveTiles();

        if (decorationRow1.configured)
            MoveDecorationRow(decorationRow1, currentZone);

        if (decorationRow2.configured)
            MoveDecorationRow(decorationRow2, currentZone);

        MoveLights(currentZone);
    }

    // ---------------- OBJECT POOLING ----------------

    GameObject GetPooledObject(GameObject prefab, Vector3 position)
    {
        if (!objectPools.ContainsKey(prefab))
        {
            objectPools[prefab] = new Stack<GameObject>();
        }

        Stack<GameObject> pool = objectPools[prefab];

        GameObject obj;

        if (pool.Count > 0)
        {
            obj = pool.Pop();

            obj.transform.position = position;
            obj.transform.rotation = Quaternion.identity;
            obj.SetActive(true);
        }
        else
        {
            obj = Instantiate(
                prefab,
                position,
                Quaternion.identity,
                transform
            );
        }

        return obj;
    }

    void ReturnToPool(GroundElement element)
    {
        element.transform.gameObject.SetActive(false);

        if (!objectPools.ContainsKey(element.prefab))
        {
            objectPools[element.prefab] = new Stack<GameObject>();
        }

        objectPools[element.prefab].Push(
            element.transform.gameObject
        );
    }

    // ---------------- VIGAS ----------------

    void MoveBeams()
    {
        MoveElements(beams, beamWidth, true);
    }

    void SpawnBeam(float x)
    {
        int index = beamIndex % beamPrefabs.Length;
        beamIndex++;

        GameObject prefab = beamPrefabs[index];

        GameObject newBeam = GetPooledObject(
            prefab,
            new Vector3(x, beamY, 0)
        );

        beams.Add(new GroundElement
        {
            transform = newBeam.transform,
            width = beamWidth,
            prefab = prefab
        });
    }

    // ---------------- BALDOSAS ----------------

    void MoveTiles()
    {
        MoveElements(tiles, maxTileWidth, false);
    }

    float SpawnTile(float x)
    {
        int randomIndex = UnityEngine.Random.Range(
            0, tilePrefabs.Length
        );

        float width = tileWidths[randomIndex];
        GameObject prefab = tilePrefabs[randomIndex];

        GameObject newTile = GetPooledObject(
            prefab,
            new Vector3(x, tileY, 0)
        );

        tiles.Add(new GroundElement
        {
            transform = newTile.transform,
            width = width,
            prefab = prefab
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

    // ---------------- DECORACIONES ----------------

    void ValidateDecorationRow(DecorationRow row, string rowName)
    {
        if (row.zone1Prefabs == null ||
            row.zone2Prefabs == null ||
            row.zone1Widths == null ||
            row.zone2Widths == null ||
            row.zone1Prefabs.Length != 2 ||
            row.zone2Prefabs.Length != 2 ||
            row.zone1Widths.Length != 2 ||
            row.zone2Widths.Length != 2)
        {
            Debug.LogWarning(rowName +
                ": necesitas exactamente dos prefabs y dos anchuras por zona.");
            return;
        }

        for (int i = 0; i < 2; i++)
        {
            if (row.zone1Prefabs[i] == null ||
                row.zone2Prefabs[i] == null ||
                row.zone1Widths[i] <= 0f ||
                row.zone2Widths[i] <= 0f)
            {
                Debug.LogWarning(rowName +
                    ": faltan prefabs o anchuras. Esta fila no se generará.");
                return;
            }
        }

        if (row.gap < 0f)
        {
            Debug.LogWarning(rowName +
                ": el espacio no puede ser negativo.");
            return;
        }

        row.configured = true;
    }

    void InitializeDecorationRow(DecorationRow row)
    {
        row.elements = new List<GroundElement>();
        row.sequenceIndex = 0;

        bool secondZone = backgroundManager.IsSecondZone;

        float x = leftEdge;

        float maxWidth = GetMaxDecorationWidth(row, secondZone);

        while (x < rightEdge + maxWidth + row.gap)
        {
            float width = SpawnDecoration(row, x, secondZone);
            x += width + row.gap;
        }
    }

    float SpawnDecoration(
        DecorationRow row,
        float x,
        bool secondZone)
    {
        GameObject[] prefabs = secondZone
            ? row.zone2Prefabs
            : row.zone1Prefabs;

        float[] widths = secondZone
            ? row.zone2Widths
            : row.zone1Widths;

        int index = row.sequenceIndex % 2;
        row.sequenceIndex++;

        GameObject prefab = prefabs[index];
        float width = widths[index];

        GameObject newObject = GetPooledObject(
            prefab,
            new Vector3(x, row.yPosition, 0)
        );

        row.elements.Add(new GroundElement
        {
            transform = newObject.transform,
            width = width,
            prefab = prefab
        });

        return width;
    }

    float GetMaxDecorationWidth(
        DecorationRow row,
        bool secondZone)
    {
        float[] widths = secondZone
            ? row.zone2Widths
            : row.zone1Widths;

        return Mathf.Max(widths[0], widths[1]);
    }

    void MoveDecorationRow(
        DecorationRow row,
        bool secondZone)
    {
        List<GroundElement> elements = row.elements;

        for (int i = elements.Count - 1; i >= 0; i--)
        {
            GroundElement element = elements[i];

            element.transform.position +=
                Vector3.left * scrollSpeed * Time.deltaTime;

            if (element.transform.position.x +
                element.width + row.gap < leftEdge)
            {
                ReturnToPool(element);
                elements.RemoveAt(i);
            }
        }

        float rightmost = leftEdge;

        foreach (GroundElement element in elements)
        {
            rightmost = Mathf.Max(
                rightmost,
                element.transform.position.x +
                element.width + row.gap
            );
        }

        float maxWidth = GetMaxDecorationWidth(row, secondZone);

        while (rightmost < rightEdge + maxWidth + row.gap)
        {
            float width = SpawnDecoration(
                row,
                rightmost,
                secondZone
            );

            rightmost += width + row.gap;
        }
    }

    // ---------------- MOVIMIENTO VIGAS Y BALDOSAS ----------------

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
                ReturnToPool(element);
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

    // ---------------- LUCES ----------------

    void MoveLights(bool secondZone)
    {
        float movement = scrollSpeed * Time.deltaTime;

        for (int i = lights.Count - 1; i >= 0; i--)
        {
            GroundElement light = lights[i];

            light.transform.position += Vector3.left * movement;

            if (light.transform.position.x + light.width < leftEdge)
            {
                ReturnToPool(light);
                lights.RemoveAt(i);
            }
        }

        // AVANZAR EL SIGUIENTE ESPACIO POSIBLE
        nextLightX -= movement;

        while (nextLightX < rightEdge + lightSpacing)
        {
            SpawnLightSlot(nextLightX, secondZone);
            nextLightX += lightSpacing;
        }
    }

    void SpawnLightSlot(float x, bool secondZone)
    {
        // PROBABILIDAD DE QUE NO APAREZCA NINGUNA LUZ
        if (UnityEngine.Random.value > lightSpawnChance)
            return;

        GameObject prefab1 = secondZone
            ? greenLightPrefab
            : blueLightPrefab;

        GameObject prefab2 = secondZone
            ? pinkLightPrefab
            : orangeLightPrefab;

        // SI NO HAY PREFABS ASIGNADOS, NO GENERAR
        if (prefab1 == null && prefab2 == null)
            return;

        GameObject selectedPrefab;

        if (prefab1 == null)
            selectedPrefab = prefab2;
        else if (prefab2 == null)
            selectedPrefab = prefab1;
        else
            selectedPrefab = UnityEngine.Random.value < 0.5f
                ? prefab1
                : prefab2;

        GameObject newLight = GetPooledObject(
            selectedPrefab,
            new Vector3(x, lightY, 0)
        );

        lights.Add(new GroundElement
        {
            transform = newLight.transform,
            width = lightWidth,
            prefab = selectedPrefab
        });
    }

}
