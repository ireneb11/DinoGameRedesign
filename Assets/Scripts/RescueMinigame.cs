using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Ponlo en un GameObject vacío y ACTIVO (NO dentro del panel). GameManager lo encuentra solo.
// Todo lo visual (carriles, marcador, objetos, obstáculos) son objetos de la escena / prefabs.
public class RescueMinigame : MonoBehaviour
{
    [Header("Reglas")]
    [Tooltip("Segundos entre morir y empezar a recoger")]
    [SerializeField] private float startDelay = 1.5f;
    [SerializeField] private float duration = 8f;
    [SerializeField] private int itemsRequired = 5;
    [Tooltip("Segundos que se restan al tocar un obstáculo")]
    [SerializeField] private float hazardTimePenalty = 1.5f;

    [Header("Aparición")]
    [Tooltip("Segundos que tarda un objeto en cruzar hasta el marcador")]
    [SerializeField] private float itemTravelTime = 1.3f;
    [SerializeField] private float minSpawnInterval = 0.5f;
    [SerializeField] private float maxSpawnInterval = 0.9f;
    [Tooltip("Probabilidad de trampa: obstáculo justo delante del objeto, en su mismo carril")]
    [Range(0f, 1f)]
    [SerializeField] private float trapChance = 0.4f;
    [Tooltip("Probabilidad de doble trampa: obstáculos en todos los carriles menos uno libre, y el objeto detrás")]
    [Range(0f, 1f)]
    [SerializeField] private float doubleTrapChance = 0.3f;
    [Tooltip("Segundos entre el obstáculo y el objeto de una trampa. Menos = más difícil")]
    [SerializeField] private float trapGap = 0.35f;

    [Header("Escena")]
    [Tooltip("Raíz de la UI del minijuego (empieza desactivada)")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private string readyMessage = "¡Prepárate!";
    [Tooltip("Los carriles, de abajo a arriba (su posición Y define dónde va cada carril)")]
    [SerializeField] private RectTransform[] lanes;
    [Tooltip("El marcador del jugador (su posición X define dónde se recoge)")]
    [SerializeField] private RectTransform marker;
    [Tooltip("Prefab UI de lo que hay que recoger")]
    [SerializeField] private GameObject itemPrefab;
    [Tooltip("Prefab UI de lo que hay que esquivar (opcional)")]
    [SerializeField] private GameObject hazardPrefab;

    [Header("Depuración")]
    [SerializeField] private bool enableDebugWinKey = true;
    [SerializeField] private KeyCode debugWinKey = KeyCode.R;

    private class Item
    {
        public Transform t;
        public int lane;
        public bool hazard;
    }

    private readonly List<Item> items = new List<Item>();
    private Action<bool> onFinished;
    private bool running;
    private float delayLeft, timeLeft, spawnTimer;
    private int collected, currentLane, lastLane;
    private float spawnExtra;
    private float rightX, leftX, itemSpeed, hitRadius;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void Begin(Action<bool> callback)
    {
        onFinished = callback;
        delayLeft = startDelay;
        timeLeft = duration;
        spawnTimer = 0f;
        collected = 0;
        lastLane = -1; spawnExtra = 0f;
        currentLane = lanes.Length / 2;
        items.Clear();

        panel.SetActive(true);

        // Ordena los carriles de abajo a arriba, da igual en qué orden los hayas puesto
        Array.Sort(lanes, (a, b) => a.position.y.CompareTo(b.position.y));

        // El texto siempre por encima del resto de elementos
        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
            timerText.transform.SetAsLastSibling();
        }

        Canvas.ForceUpdateCanvases();

        // Medidas en coordenadas de mundo, así no dependen de anclas ni de la resolución
        Vector3[] corners = new Vector3[4];
        panel.GetComponent<RectTransform>().GetWorldCorners(corners);
        float laneGap = lanes.Length > 1 ? Mathf.Abs(lanes[1].position.y - lanes[0].position.y) : 1f;
        hitRadius = laneGap * 0.55f;
        leftX = corners[0].x - hitRadius;
        rightX = corners[2].x + hitRadius;
        itemSpeed = (rightX - marker.position.x) / Mathf.Max(0.1f, itemTravelTime);

        MoveMarker();
        if (timerText != null) timerText.text = readyMessage;
        running = true;
    }

    private void Update()
    {
        if (!running) return;

        if (enableDebugWinKey && Input.GetKeyDown(debugWinKey))
        {
            Finish(true);
            return;
        }

        if (delayLeft > 0f)
        {
            delayLeft -= Time.deltaTime;
            return;
        }

        timeLeft -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) currentLane = Mathf.Min(currentLane + 1, lanes.Length - 1);
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) currentLane = Mathf.Max(currentLane - 1, 0);
        MoveMarker();

        UpdateItems();

        if (timerText != null)
        {
            timerText.text = $"Objetos {collected}/{itemsRequired}   {Mathf.Max(0f, timeLeft):F1}s";
        }

        if (collected >= itemsRequired) Finish(true);
        else if (timeLeft <= 0f) Finish(false);
    }

    private void MoveMarker()
    {
        marker.position = new Vector3(marker.position.x, lanes[currentLane].position.y, marker.position.z);
    }

    private void UpdateItems()
    {
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            Spawn();
            spawnTimer = UnityEngine.Random.Range(minSpawnInterval, maxSpawnInterval) + spawnExtra;
        }

        for (int i = items.Count - 1; i >= 0; i--)
        {
            Item item = items[i];
            Vector3 p = item.t.position;
            p.x -= itemSpeed * Time.deltaTime;
            item.t.position = p;

            if (item.lane == currentLane && Mathf.Abs(p.x - marker.position.x) <= hitRadius)
            {
                if (item.hazard) timeLeft -= hazardTimePenalty;
                else collected++;

                Destroy(item.t.gameObject);
                items.RemoveAt(i);
            }
            else if (p.x < leftX)
            {
                Destroy(item.t.gameObject);
                items.RemoveAt(i);
            }
        }
    }

    // Reglas (no es azar puro): el objeto siempre sale en un carril distinto al anterior y
    // las trampas siempre dejan un carril libre, así que todo es superable.
    private void Spawn()
    {
        int lane;
        do
        {
            lane = UnityEngine.Random.Range(0, lanes.Length);
        } while (lanes.Length > 1 && lane == lastLane);
        lastLane = lane;

        float roll = UnityEngine.Random.value;
        bool canTrap = hazardPrefab != null;
        spawnExtra = 0f;

        if (canTrap && lanes.Length > 2 && roll < doubleTrapChance)
        {
            // Doble trampa: obstáculos en todos los carriles menos uno libre (distinto al del objeto)
            int safe;
            do
            {
                safe = UnityEngine.Random.Range(0, lanes.Length);
            } while (safe == lane);

            for (int i = 0; i < lanes.Length; i++)
            {
                if (i != safe) CreateAt(hazardPrefab, i, rightX, true);
            }
            CreateAt(itemPrefab, lane, rightX + itemSpeed * trapGap, false);
            spawnExtra = trapGap; // margen para poder reaccionar al siguiente
        }
        else if (canTrap && roll < doubleTrapChance + trapChance)
        {
            CreateAt(hazardPrefab, lane, rightX, true);
            CreateAt(itemPrefab, lane, rightX + itemSpeed * trapGap, false);
            spawnExtra = trapGap;
        }
        else
        {
            CreateAt(itemPrefab, lane, rightX, false);
        }
    }

    private void CreateAt(GameObject prefab, int lane, float x, bool hazard)
    {
        GameObject go = Instantiate(prefab, marker.parent);
        go.transform.position = new Vector3(x, lanes[lane].position.y, marker.position.z);
        items.Add(new Item { t = go.transform, lane = lane, hazard = hazard });
    }

    public void Finish(bool success)
    {
        if (!running) return;
        running = false;

        foreach (Item item in items) Destroy(item.t.gameObject);
        items.Clear();

        panel.SetActive(false);

        Action<bool> callback = onFinished;
        onFinished = null;
        callback?.Invoke(success);
    }

}