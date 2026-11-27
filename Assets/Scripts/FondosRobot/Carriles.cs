using UnityEngine;
using UnityEngine.UI;


public class Carriles : MonoBehaviour
{
    public Image image;
    public Sprite[] frames;
    public float framesPerSecond = 8f;

    private int currentFrame = 0;
    private float timer = 0f;

    void Start()
    {
        if (frames != null && frames.Length > 0)
        {
            image.sprite = frames[0];
        }
    }

    void Update()
    {
        if (frames == null || frames.Length == 0)
            return;

        timer += Time.deltaTime;

        if (timer >= 1f / framesPerSecond)
        {
            timer = 0f;

            currentFrame++;

            if (currentFrame >= frames.Length)
                currentFrame = 0;

            image.sprite = frames[currentFrame];
        }
    }
}