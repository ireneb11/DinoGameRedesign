using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    private CharacterController character;
    private Vector3 direction;

    public float jumpForce = 8f;
    public float gravity = 9.81f * 2f;

    // ---- AÑADIDO: muerte (todo opcional) ----
    [Header("Muerte (usa una de las dos opciones)")]
    [Tooltip("Animator con un parámetro Trigger llamado 'Death'")]
    [SerializeField] private Animator animator;
    [Tooltip("Sprite fijo de muerte, si no usas Animator")]
    [SerializeField] private Sprite deathSprite;

    private bool dead;
    private AnimatedSprite animatedSprite;
    private SpriteRenderer spriteRenderer;
    // -----------------------------------------

    private void Awake()
    {
        character = GetComponent<CharacterController>();
        animatedSprite = GetComponentInChildren<AnimatedSprite>(); // AÑADIDO
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(); // AÑADIDO
    }

    private void OnEnable()
    {
        direction = Vector3.zero;

        // AÑADIDO: al revivir / reiniciar vuelve a estar vivo
        dead = false;
        if (animatedSprite != null) animatedSprite.enabled = true;
    }

    // AÑADIDO: lo llama GameManager al morir (si hay salvación)
    public void Die()
    {
        dead = true; // ya no puede saltar, pero cae al suelo si estaba en el aire

        if (animatedSprite != null) animatedSprite.enabled = false;

        if (animator != null)
        {
            animator.SetTrigger("Death");
        }
        else if (deathSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = deathSprite;
        }
    }

    private void Update()
    {
        direction += gravity * Time.deltaTime * Vector3.down;

        if (character.isGrounded)
        {
            direction = Vector3.down;

            if (!dead && Input.GetButton("Jump"))
            { // MODIFICADO: !dead
                direction = Vector3.up * jumpForce;
            }
        }

        character.Move(direction * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            GameManager.Instance.GameOver();
        }
    }

}