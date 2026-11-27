using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    private CharacterController character;
    private AnimatedSprite animatedSprite; // NUEVO
    private Vector3 direction;
    private bool isJumping; // NUEVO

    public float jumpForce = 8f;
    public float gravity = 9.81f * 2f;

    private void Awake()
    {
        character = GetComponent<CharacterController>();
        animatedSprite = GetComponent<AnimatedSprite>(); // NUEVO
    }

    private void OnEnable()
    {
        direction = Vector3.zero;
    }


    private void Update()
    {
        direction += gravity * Time.deltaTime * Vector3.down;

        if (character.isGrounded)
        {
            direction = Vector3.down;

            if (isJumping && direction.y <= 0f)
            {
                isJumping = false;
                animatedSprite.PlayRun();
            }

            if (Input.GetButton("Jump"))
            {
                direction = Vector3.up * jumpForce;

                isJumping = true;
                animatedSprite.PlayJump();
            }
        }

        character.Move(direction * Time.deltaTime);
    }
    /*
    private void Update()
    {
        
        
        
        direction += gravity * Time.deltaTime * Vector3.down;

        if (character.isGrounded)
        {
            direction = Vector3.down;

            if (Input.GetButton("Jump")) {
                direction = Vector3.up * jumpForce;
            }
        }

        character.Move(direction * Time.deltaTime);
        
}*/

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle")) {
            animatedSprite.PlayDead(); // NUEVO
            GameManager.Instance.GameOver();
        }
    }

}
