using UnityEngine;
using UnityEngine.UI;

// Ponlo en el mismo GameObject que el Player.
// - Como GameManager desactiva/activa al Player en GameOver/NewGame,
//   la barra se reinicia sola en cada partida (OnEnable).
// - Para apagar toda la mecánica: desmarca este componente en el Inspector.
[RequireComponent(typeof(CharacterController))]
public class EmpSystem : MonoBehaviour
{
    [Header("Casi-choque")]
    [Tooltip("Distancia máxima (en unidades de mundo) entre dino y obstáculo para contar como casi-choque")]
    [SerializeField] private float nearMissDistance = 0.4f;

    [Tooltip("Casi-choques necesarios para llenar la barra (5 = cada uno es 1/5)")]
    [SerializeField] private int nearMissesToCharge = 5;

    [Header("EMP")]
    [SerializeField] private KeyCode activationKey = KeyCode.E;

    [Header("UI")]
    [Tooltip("Image con Image Type = Filled, Fill Method = Horizontal (necesita un sprite asignado)")]
    [SerializeField] private Image fillImage;
    [SerializeField] private Color normalColor = new Color(0.2f, 0.8f, 1f);
    [SerializeField] private Color readyColor = Color.yellow;

    [Header("Depuración")]
    [Tooltip("Escribe en la consola cada obstáculo esquivado y su distancia mínima")]
    [SerializeField] private bool debugLogs = true;

    private Collider playerCollider;
    private Camera cam;
    private int charges;

    public bool IsReady => charges >= nearMissesToCharge;

    private void Awake()
    {
        playerCollider = GetComponent<CharacterController>();
        cam = Camera.main;
    }

    private void OnEnable()
    {
        charges = 0;
        RefreshUI();
    }

    private void Update()
    {
        TrackNearMisses();

        if (IsReady && Input.GetKeyDown(activationKey))
        {
            ActivateEmp();
        }
    }

    private void TrackNearMisses()
    {
        Bounds playerBounds = playerCollider.bounds;

        foreach (Obstacle obstacle in Obstacle.Active)
        {
            if (obstacle.IsDisarmed || obstacle.nearMissResolved) continue;

            Bounds obstacleBounds = obstacle.GetBounds();

            // Nos quedamos con la distancia mínima durante todo el cruce
            float gap = GapBetween(playerBounds, obstacleBounds);
            obstacle.closestDistance = Mathf.Min(obstacle.closestDistance, gap);

            // El obstáculo ya ha pasado completamente por detrás del dino
            if (obstacleBounds.max.x < playerBounds.min.x)
            {
                obstacle.nearMissResolved = true;

                // > 0 descarta solapamientos reales (choques)
                bool isNearMiss = obstacle.closestDistance > 0.001f && obstacle.closestDistance <= nearMissDistance;

                if (debugLogs)
                {
                    Debug.Log($"[EMP] {obstacle.name}: distancia mínima = {obstacle.closestDistance:F2} -> {(isNearMiss ? "CASI-CHOQUE (+1)" : "no cuenta")}");
                }

                if (isNearMiss)
                {
                    AddCharge();
                }
            }
        }
    }

    private void AddCharge()
    {
        charges = Mathf.Min(charges + 1, nearMissesToCharge);
        RefreshUI();
    }

    private void ActivateEmp()
    {
        foreach (Obstacle obstacle in Obstacle.Active)
        {
            if (obstacle.IsDisarmed) continue;

            // Solo los que están en pantalla (o a punto de entrar)
            float viewportX = cam.WorldToViewportPoint(obstacle.transform.position).x;

            if (viewportX <= 1.05f)
            {
                obstacle.Disarm();
            }
        }

        charges = 0;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (fillImage == null) return;

        fillImage.fillAmount = (float)charges / nearMissesToCharge;
        fillImage.color = IsReady ? readyColor : normalColor;
    }

    // Dibuja en la vista Scene (con el Player seleccionado) la zona de casi-choque
    private void OnDrawGizmosSelected()
    {
        if (playerCollider == null) return;

        Bounds b = playerCollider.bounds;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(b.center, b.size + new Vector3(nearMissDistance * 2f, nearMissDistance * 2f, 0f));
    }

    // Distancia entre dos AABB en el plano XY (0 si se solapan)
    private static float GapBetween(Bounds a, Bounds b)
    {
        float dx = Mathf.Max(0f, Mathf.Max(a.min.x - b.max.x, b.min.x - a.max.x));
        float dy = Mathf.Max(0f, Mathf.Max(a.min.y - b.max.y, b.min.y - a.max.y));
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

}