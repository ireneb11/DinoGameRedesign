using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class AnimatedSprite : MonoBehaviour
{
    [Header("Run")]
    public Sprite[] runSprites;

    [Header("Jump")]
    public Sprite jumpSprite;

    [Header("Dead")]
    public Sprite[] deadSprites;

    [Header("Animation Speeds")]
    public float runAnimationSpeed = 12f;
    public float deadAnimationSpeed = 8f;

    private SpriteRenderer spriteRenderer;
    private Sprite[] sprites;
    private int frame;
    private float timer;
    private bool isJumping;
    private bool isDead;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        PlayRun();
    }

    private void Update()
    {
        Animate();
    }

    public void PlayRun()
    {
        if (isDead) return;

        if (isJumping || sprites != runSprites)
        {
            isJumping = false;
            SetAnimation(runSprites);
        }
    }

    public void PlayJump()
    {
        if (isDead || isJumping) return;

        isJumping = true;
        sprites = null;
        timer = 0f;

        if (jumpSprite != null)
        {
            spriteRenderer.sprite = jumpSprite;
        }
    }

    public void PlayDead()
    {
        if (isDead) return;

        isDead = true;
        isJumping = false;
        SetAnimation(deadSprites);
    }

    private void SetAnimation(Sprite[] newSprites)
    {
        sprites = newSprites;
        frame = 0;
        timer = 0f;

        if (sprites != null && sprites.Length > 0)
        {
            spriteRenderer.sprite = sprites[0];
        }
    }

    private void Animate()
    {
        if (isJumping || sprites == null || sprites.Length <= 1)
            return;

        float speed = isDead ? deadAnimationSpeed : runAnimationSpeed;

        timer += Time.deltaTime;

        if (timer >= 1f / speed)
        {
            timer = 0f;
            frame++;

            if (isDead && frame >= sprites.Length)
            {
                frame = sprites.Length - 1;
                return;
            }

            if (frame >= sprites.Length)
            {
                frame = 0;
            }

            spriteRenderer.sprite = sprites[frame];
        }
    }
}

/* using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class AnimatedSprite : MonoBehaviour
{
    public Sprite[] sprites;
    private SpriteRenderer spriteRenderer;
    private int frame;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        Invoke(nameof(Animate), 0f);
    }

    private void OnDisable()
    {
        CancelInvoke();
    }

    private void Animate()
    {
        frame++;

        if (frame >= sprites.Length) {
            frame = 0;
        }

        if (frame >= 0 && frame < sprites.Length) {
            spriteRenderer.sprite = sprites[frame];
        }

        Invoke(nameof(Animate), 1f / GameManager.Instance.gameSpeed);
    }

}
*/