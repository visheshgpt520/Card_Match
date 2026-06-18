using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum CardState
{
    FaceDown,
    FlippingUp,
    FaceUp,
    FlippingDown,
    Matched
}

public class _Card : MonoBehaviour
{
    [SerializeField] private Image img;

    public int SpriteID { get; set; }
    public int ID { get; set; }
    public CardState State { get; private set; } = CardState.FaceDown;

    private Coroutine activeAnimation;

    public void Initialize(int id, int spriteID, Sprite backSprite)
    {
        ID = id;
        SpriteID = spriteID;
        
        if (img != null)
        {
            img.sprite = backSprite;
            img.color = Color.white;
        }

        transform.rotation = Quaternion.Euler(0, 180, 0); // start face down
        State = CardState.FaceDown;
        gameObject.SetActive(true);

        if (activeAnimation != null)
        {
            StopCoroutine(activeAnimation);
            activeAnimation = null;
        }
    }

    public void RestoreState(CardState state, int spriteID, Sprite sprite)
    {
        this.SpriteID = spriteID;
        this.State = state;

        if (activeAnimation != null)
        {
            StopCoroutine(activeAnimation);
            activeAnimation = null;
        }

        if (state == CardState.Matched)
        {
            if (img != null)
            {
                img.color = Color.clear;
            }
            gameObject.SetActive(false);
        }
        else if (state == CardState.FaceUp)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
            if (img != null)
            {
                img.sprite = sprite;
                img.color = Color.white;
            }
            gameObject.SetActive(true);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
            if (img != null)
            {
                img.sprite = _CardGameManager.Instance.CardBack();
                img.color = Color.white;
            }
            gameObject.SetActive(true);
        }
    }

    // Called by the Button component onClick event in Unity
    public void CardBtn()
    {
        if (State != CardState.FaceDown) return;
        if (!_CardGameManager.Instance.canClick()) return;

        FlipUp();
        _CardGameManager.Instance.CardClicked(ID);
    }

    public void FlipUp()
    {
        State = CardState.FlippingUp;
        if (activeAnimation != null) StopCoroutine(activeAnimation);
        activeAnimation = StartCoroutine(FlipCoroutine(0.25f, true));
    }

    public void AnimateMatch()
    {
        State = CardState.Matched;
        if (activeAnimation != null) StopCoroutine(activeAnimation);
        activeAnimation = StartCoroutine(MatchCoroutine());
    }

    public void AnimateMismatch()
    {
        State = CardState.FlippingDown;
        if (activeAnimation != null) StopCoroutine(activeAnimation);
        activeAnimation = StartCoroutine(FlipCoroutine(0.25f, false));
    }

    // Compatibility method for the original manager if called
    public void Flip()
    {
        if (State == CardState.FaceDown || State == CardState.FlippingDown)
        {
            FlipUp();
        }
        else if (State == CardState.FaceUp || State == CardState.FlippingUp)
        {
            AnimateMismatch();
        }
    }

    // Compatibility method for original manager fade out
    public void Inactive()
    {
        AnimateMatch();
    }

    // Compatibility method for original manager
    public void Active()
    {
        if (img != null)
            img.color = Color.white;
    }

    // Compatibility method for original manager
    public void ResetRotation()
    {
        transform.rotation = Quaternion.Euler(0, 180, 0);
        State = CardState.FaceDown;
        if (img != null)
        {
            img.sprite = _CardGameManager.Instance.CardBack();
            img.color = Color.white;
        }
    }

    private IEnumerator FlipCoroutine(float duration, bool flipUp)
    {
        // Play click/flip sound with slight pitch variation (1.0 for flip up, 0.8 for flip down)
        if (AudioPlayer.Instance != null)
        {
            AudioPlayer.Instance.PlayAudio(0, 0.8f, flipUp ? 1.0f : 0.8f);
        }

        float startY = transform.rotation.eulerAngles.y;
        float targetY = flipUp ? 0f : 180f;

        // Ensure we rotate correctly around the closest Y direction
        if (Mathf.Abs(startY - targetY) > 180f)
        {
            if (startY > targetY) startY -= 360f;
            else targetY -= 360f;
        }

        float elapsed = 0f;
        bool spriteChanged = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            float currentY = Mathf.Lerp(startY, targetY, smoothT);
            transform.rotation = Quaternion.Euler(0, currentY, 0);

            // Swap the sprite exactly halfway through the flip (at 90 degrees)
            if (!spriteChanged && t >= 0.5f)
            {
                spriteChanged = true;
                if (img != null)
                {
                    if (flipUp)
                        img.sprite = _CardGameManager.Instance.GetSprite(SpriteID);
                    else
                        img.sprite = _CardGameManager.Instance.CardBack();
                }
            }

            yield return null;
        }

        transform.rotation = Quaternion.Euler(0, targetY, 0);
        State = flipUp ? CardState.FaceUp : CardState.FaceDown;
        activeAnimation = null;
    }

    private IEnumerator MatchCoroutine()
    {
        // Play success audio pitch-shifted higher
        if (AudioPlayer.Instance != null)
        {
            AudioPlayer.Instance.PlayAudio(0, 0.9f, 1.4f);
        }

        Vector3 originalScale = transform.localScale;
        
        // Pop scaling effect (scale up to 1.15x then back)
        float popDuration = 0.15f;
        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;
            // Pop out and back using a sine wave
            transform.localScale = originalScale * (1.0f + 0.15f * Mathf.Sin(t * Mathf.PI));
            yield return null;
        }
        transform.localScale = originalScale;

        // Smooth fade out
        float fadeDuration = 0.4f;
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            if (img != null)
            {
                img.color = Color.Lerp(Color.white, Color.clear, t);
            }
            yield return null;
        }

        if (img != null)
        {
            img.color = Color.clear;
        }
        gameObject.SetActive(false);
        activeAnimation = null;
    }
}