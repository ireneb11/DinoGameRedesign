using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    // ---- AÑADIDO: lista de obstáculos vivos (la usa EmpSystem) ----
    public static readonly List<Obstacle> Active = new List<Obstacle>();

    // ---- AÑADIDO: estado para la mecánica de "casi-choque" ----
    [HideInInspector] public float closestDistance = float.MaxValue;
    [HideInInspector] public bool nearMissResolved;

    public bool IsDisarmed { get; private set; }

    private Collider[] colliders;
    private SpriteRenderer[] renderers;
    // -----------------------------------------------------------

    private float leftEdge;

    // ---- AÑADIDO ----
    private void Awake()
    {
        colliders = GetComponentsInChildren<Collider>();
        renderers = GetComponentsInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }
    // -----------------

    private void Start()
    {
        leftEdge = Camera.main.ScreenToWorldPoint(Vector3.zero).x - 2f;
    }

    private void Update()
    {
        transform.position += GameManager.Instance.gameSpeed * Time.deltaTime * Vector3.left;

        if (transform.position.x < leftEdge)
        {
            Destroy(gameObject);
        }
    }

    // ---- AÑADIDO: bounds combinados de todos sus colliders ----
    public Bounds GetBounds()
    {
        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        bool first = true;

        foreach (Collider c in colliders)
        {
            if (first)
            {
                bounds = c.bounds;
                first = false;
            }
            else
            {
                bounds.Encapsulate(c.bounds);
            }
        }

        return bounds;
    }

    // ---- AÑADIDO: el EMP lo deja inofensivo (sigue moviéndose, pero fantasma) ----
    public void Disarm()
    {
        if (IsDisarmed) return;
        IsDisarmed = true;

        foreach (Collider c in colliders)
        {
            c.enabled = false;
        }

        foreach (SpriteRenderer sr in renderers)
        {
            Color color = sr.color;
            color.a = 0.3f;
            sr.color = color;
        }
    }

}