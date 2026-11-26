using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ponlo en un GameObject vacío y ACTIVO de la escena (NO en el panel, porque el panel
// se desactiva). GameManager lo encuentra solo con FindObjectOfType.
//
// Minijuego: 3 carriles horizontales. Un marcador se mueve entre ellos (↑/↓ o W/S) y los
// objetos llegan desde la derecha. Recoge itemsRequired antes de que acabe el tiempo.
// Todo el aspecto se construye por código en Begin() y se destruye en Finish().
public class RescueMinigame : MonoBehaviour
{
    private const int LaneCount = 3;

    [Header("Reglas")]
    [SerializeField] private float duration = 8f;
    [SerializeField] private int itemsRequired = 5;

    [Header("Objetos")]
    [Tooltip("Segundos que tarda un objeto en cruzar desde el borde derecho hasta el marcador")]
    [SerializeField] private float itemTravelTime = 1.3f;
    [SerializeField] private float minSpawnInterval = 0.5f;
    [SerializeField] private float maxSpawnInterval = 0.9f;

    [Header("UI")]
    [Tooltip("Raíz de la UI del minijuego. Empieza desactivada y se activa al morir")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Aspecto (opcional: sin sprites se usan colores lisos)")]
    [SerializeField] private Sprite markerSprite;
    [SerializeField] private Sprite itemSprite;
    [SerializeField] private Color laneColor = new Color(1f, 1f, 1f, 0.15f);
    [SerializeField] private Color markerColor = new Color(0.2f, 0.8f, 1f);
    [SerializeField] private Color itemColor = Color.yellow;

    [Header("Depuración")]
    [Tooltip("Desmárcalo en la versión final")]
    [SerializeField] private bool enableDebugWinKey = true;
    [SerializeField] private KeyCode debugWinKey = KeyCode.R;

    private class Item
    {
        public RectTransform rect;
        public int lane;
    }

    private Action<bool> onFinished;
    private float timeLeft;
    private bool running;
    private int collected;
    private int currentLane;
    private float spawnTimer;

    private RectTransform area;
    private RectTransform marker;
    private readonly List<Item> items = new List<Item>();

    private float laneSpacing;
    private float halfWidth;
    private float markerX;
    private float spawnX;
    private float itemSpeed;
    private float hitRadius;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void Begin(Action<bool> callback)
    {
        onFinished = callback;
        timeLeft = duration;
        collected = 0;
        currentLane = 1; // carril central
        spawnTimer = 0f; // el primer objeto sale enseguida
        items.Clear();

        if (panel != null) panel.SetActive(true);
        Canvas.ForceUpdateCanvases();

        BuildArea();
        running = true;
        RefreshText();
    }

    private void Update()
    {
        if (!running) return;

        // Time.deltaTime no depende de gameSpeed, así que funciona con el mundo congelado
        timeLeft -= Time.deltaTime;

        HandleInput();
        UpdateItems();
        RefreshText();

        if (collected >= itemsRequired)
        {
            Finish(true);
        }
        else if (enableDebugWinKey && Input.GetKeyDown(debugWinKey))
        {
            Finish(true);
        }
        else if (timeLeft <= 0f)
        {
            Finish(false);
        }
    }

    private void HandleInput()
    {
        bool up = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
        bool down = Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);

        if (up) currentLane = Mathf.Min(currentLane + 1, LaneCount - 1);
        if (down) currentLane = Mathf.Max(currentLane - 1, 0);

        marker.anchoredPosition = new Vector2(markerX, LaneY(currentLane));
    }

    private void UpdateItems()
    {
        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            SpawnItem();
            spawnTimer = UnityEngine.Random.Range(minSpawnInterval, maxSpawnInterval);
        }

        for (int i = items.Count - 1; i >= 0; i--)
        {
            Item item = items[i];

            Vector2 pos = item.rect.anchoredPosition;
            pos.x -= itemSpeed * Time.deltaTime;
            item.rect.anchoredPosition = pos;

            if (item.lane == currentLane && Mathf.Abs(pos.x - markerX) <= hitRadius)
            {
                // Recogido
                collected++;
                Destroy(item.rect.gameObject);
                items.RemoveAt(i);
            }
            else if (pos.x < -halfWidth - 50f)
            {
                // Se ha escapado por la izquierda
                Destroy(item.rect.gameObject);
                items.RemoveAt(i);
            }
        }
    }

    private void SpawnItem()
    {
        int lane = UnityEngine.Random.Range(0, LaneCount);

        Image image = CreateImage("Item", area, Vector2.one * laneSpacing * 0.45f, itemColor, itemSprite);
        image.rectTransform.anchoredPosition = new Vector2(spawnX, LaneY(lane));

        items.Add(new Item { rect = image.rectTransform, lane = lane });
    }

    // Aquí termina el minijuego (también se puede llamar desde fuera)
    public void Finish(bool success)
    {
        if (!running) return;
        running = false;

        if (area != null)
        {
            Destroy(area.gameObject); // se lleva consigo carriles, marcador y objetos
            area = null;
        }
        items.Clear();

        if (panel != null) panel.SetActive(false);

        Action<bool> callback = onFinished;
        onFinished = null;
        callback?.Invoke(success);
    }

    private void BuildArea()
    {
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        float width = panelRect.rect.width > 0f ? panelRect.rect.width : 800f;
        float height = panelRect.rect.height > 0f ? panelRect.rect.height : 450f;

        laneSpacing = Mathf.Min(120f, height / 5f);
        halfWidth = width * 0.5f;
        markerX = -width * 0.3f;
        spawnX = halfWidth + 50f;
        itemSpeed = (spawnX - markerX) / Mathf.Max(0.1f, itemTravelTime);
        hitRadius = laneSpacing * 0.55f;

        GameObject areaObject = new GameObject("RescueArea", typeof(RectTransform));
        area = areaObject.GetComponent<RectTransform>();
        area.SetParent(panel.transform, false);
        area.anchorMin = area.anchorMax = area.pivot = new Vector2(0.5f, 0.5f);
        area.anchoredPosition = Vector2.zero;
        area.sizeDelta = new Vector2(width, laneSpacing * LaneCount);
        area.SetAsFirstSibling(); // así el texto del temporizador queda por encima

        for (int i = 0; i < LaneCount; i++)
        {
            Image lane = CreateImage("Lane" + i, area, new Vector2(width, laneSpacing - 8f), laneColor, null);
            lane.rectTransform.anchoredPosition = new Vector2(0f, LaneY(i));
        }

        Image markerImage = CreateImage("Marker", area, Vector2.one * laneSpacing * 0.6f, markerColor, markerSprite);
        marker = markerImage.rectTransform;
        marker.anchoredPosition = new Vector2(markerX, LaneY(currentLane));
    }

    // Carril 0 abajo, 1 en el centro, 2 arriba
    private float LaneY(int lane)
    {
        return (lane - 1) * laneSpacing;
    }

    private Image CreateImage(string objectName, Transform parent, Vector2 size, Color color, Sprite sprite)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image));

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.raycastTarget = false;
        image.color = color;

        if (sprite != null)
        {
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = Color.white;
        }

        return image;
    }

    private void RefreshText()
    {
        if (timerText != null)
        {
            timerText.text = $"Objetos {collected}/{itemsRequired}   {Mathf.Max(0f, timeLeft):F1}s";
        }
    }

}