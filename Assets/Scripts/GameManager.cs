using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-1)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public float initialGameSpeed = 5f;
    public float gameSpeedIncrease = 0.1f;
    public float gameSpeed { get; private set; }

    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI hiscoreText;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private Button retryButton;

    private Player player;
    private Spawner spawner;

    // ---- AÑADIDO: salvación ----
    private RescueMinigame rescue;
    private float speedBeforeDeath;
    private bool rescueUsed;
    public bool IsRescuing { get; private set; }
    public bool IsReviving { get; private set; }
    // ----------------------------

    private float score;
    public float Score => score;

    private void Awake()
    {
        if (Instance != null)
        {
            DestroyImmediate(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        player = FindObjectOfType<Player>();
        spawner = FindObjectOfType<Spawner>();

        // AÑADIDO: si no hay RescueMinigame en la escena, el juego funciona como el original
        rescue = FindObjectOfType<RescueMinigame>();

        NewGame();
    }

    public void NewGame()
    {
        Obstacle[] obstacles = FindObjectsOfType<Obstacle>();

        foreach (var obstacle in obstacles)
        {
            Destroy(obstacle.gameObject);
        }

        score = 0f;
        gameSpeed = initialGameSpeed;
        enabled = true;

        // AÑADIDO
        rescueUsed = false;
        IsRescuing = false;

        player.gameObject.SetActive(true);
        spawner.gameObject.SetActive(true);
        gameOverText.gameObject.SetActive(false);
        retryButton.gameObject.SetActive(false);

        UpdateHiscore();
    }

    // MODIFICADO: ahora decide si hay salvación o game over definitivo
    public void GameOver()
    {
        if (IsRescuing) return; // ya estamos en la salvación (por si hay dos choques en el mismo frame)

        if (rescue != null && !rescueUsed)
        {
            rescueUsed = true; // una salvación por partida
            StartRescue();
            return;
        }

        FinalGameOver();
    }

    // Este es el GameOver() original, sin cambios
    private void FinalGameOver()
    {
        gameSpeed = 0f;
        enabled = false;

        player.gameObject.SetActive(false);
        spawner.gameObject.SetActive(false);
        gameOverText.gameObject.SetActive(true);
        retryButton.gameObject.SetActive(true);

        UpdateHiscore();
    }

    // ---- AÑADIDO ----
    private void StartRescue()
    {
        IsRescuing = true;
        speedBeforeDeath = gameSpeed;

        // Limpiamos obstáculos: al revivir no habrá nada encima del dino, y evitamos que
        // AnimatedSprite divida entre gameSpeed = 0 en los pájaros que estén vivos.
        foreach (Obstacle obstacle in FindObjectsOfType<Obstacle>())
        {
            Destroy(obstacle.gameObject);
        }

        gameSpeed = 0f;   // congela suelo y animaciones
        enabled = false;  // congela puntuación y subida de velocidad

        player.gameObject.SetActive(false);
        spawner.gameObject.SetActive(false);

        rescue.Begin(OnRescueFinished);
    }

    private void OnRescueFinished(bool success)
    {
        IsRescuing = false;

        if (success)
        {
            Revive();
        }
        else
        {
            FinalGameOver();
        }
    }

    private void Revive()
    {
        gameSpeed = speedBeforeDeath; // antes de activar al jugador (AnimatedSprite divide entre gameSpeed)
        enabled = true;

        IsReviving = true;
        player.gameObject.SetActive(true);
        IsReviving = false;

        // El spawner arranca con su retraso aleatorio, así que hay margen antes del siguiente obstáculo
        spawner.gameObject.SetActive(true);
    }
    // -----------------

    private void Update()
    {
        gameSpeed += gameSpeedIncrease * Time.deltaTime;
        score += gameSpeed * Time.deltaTime;
        scoreText.text = Mathf.FloorToInt(score).ToString("D5");
    }

    private void UpdateHiscore()
    {
        float hiscore = PlayerPrefs.GetFloat("hiscore", 0);

        if (score > hiscore)
        {
            hiscore = score;
            PlayerPrefs.SetFloat("hiscore", hiscore);
        }

        hiscoreText.text = Mathf.FloorToInt(hiscore).ToString("D5");
    }

}