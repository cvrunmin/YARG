using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighlightFlashHelper : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Color noteHitColor = new Color(0x8d / 255f, 0x94 / 255f, 0xf4 / 255f);
    private Color currentBaseColor;
    private float flashDuration = 0.5f;
    float flashStartTime;
    bool shouldFlash;

    // Start is called before the first frame update
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
        currentBaseColor = spriteRenderer.color;
        spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0);
    }

    // Update is called once per frame
    void Update()
    {
        if (shouldFlash)
        {
            float time = Time.unscaledTime;
            spriteRenderer.color = new Color(currentBaseColor.r, currentBaseColor.g, currentBaseColor.b, (1 - (time - flashStartTime) / flashDuration) * 0.5f);
            if(flashStartTime + flashDuration <= time)
            {
                shouldFlash = false;
            }
        }
    }

    public void TriggerFlash()
    {
        currentBaseColor = originalColor;
        flashStartTime = Time.unscaledTime;
        flashDuration = 0.5f;
        shouldFlash = true;
    }

    public void TriggerNoteHitFlash()
    {
        currentBaseColor = noteHitColor;
        flashStartTime = Time.unscaledTime;
        flashDuration = 0.25f;
        shouldFlash = true;
    }
}
