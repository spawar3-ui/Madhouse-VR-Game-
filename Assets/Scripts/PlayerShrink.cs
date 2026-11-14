using System.Collections;
using UnityEngine;

// Put this on the Player (same object as PlayerController).
// Call ScaleTo(0.1f) to shrink, ScaleTo(1f) to grow back (e.g. "Eat Me" cake).
public class PlayerShrink : MonoBehaviour
{
    [Header("Timing")]
    public float duration = 3f;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Feel")]
    public bool scaleMoveSpeed = true;     // smaller Alice = slower walking
    public bool fovKick = true;            // little "whoosh" while shrinking
    public float fovKickAmount = 15f;

    [Header("Sounds (both play at the same time when shrinking)")]
    public AudioClip shrinkSound1;
    public AudioClip shrinkSound2;
    [Range(0f, 1f)] public float shrinkVolume1 = 1f;
    [Range(0f, 1f)] public float shrinkVolume2 = 1f;

    [Header("Grow sound (optional, plays when growing back)")]
    public AudioClip growSound;
    [Range(0f, 1f)] public float growVolume = 1f;

    private PlayerController controller;
    private CharacterController cc;
    private Camera cam;
    private float baseMoveSpeed;
    private float baseFov;
    private Coroutine running;

    // CharacterController settings that do NOT scale on their own
    private float baseScale = 1f;
    private float baseSkinWidth;
    private float baseStepOffset;

    // Two separate audio sources so the sounds can play at once
    private AudioSource source1;
    private AudioSource source2;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        cc = GetComponent<CharacterController>();
        cam = GetComponentInChildren<Camera>();
        baseMoveSpeed = controller.moveSpeed;

        baseScale = Mathf.Max(0.0001f, transform.localScale.y);
        if (cc != null)
        {
            baseSkinWidth = cc.skinWidth;
            baseStepOffset = cc.stepOffset;
        }
        if (cam != null) baseFov = cam.fieldOfView;

        source1 = gameObject.AddComponent<AudioSource>();
        source2 = gameObject.AddComponent<AudioSource>();
        foreach (var s in new[] { source1, source2 })
        {
            s.playOnAwake = false;
            s.spatialBlend = 0f;   // 2D sound, so it stays the same volume as Alice changes size
        }
    }

    public void ScaleTo(float targetScale)
    {
        bool shrinking = targetScale < transform.localScale.x;

        if (shrinking)
        {
            if (shrinkSound1 != null) source1.PlayOneShot(shrinkSound1, shrinkVolume1);
            if (shrinkSound2 != null) source2.PlayOneShot(shrinkSound2, shrinkVolume2);
        }
        else if (growSound != null)
        {
            source1.PlayOneShot(growSound, growVolume);
        }

        if (running != null) StopCoroutine(running);
        running = StartCoroutine(ScaleRoutine(targetScale));
    }

    // Instantly puts Alice at a position and scale (used when the key timer runs out).
    public void ResetTo(Vector3 position, Vector3 scale)
    {
        if (running != null)
        {
            StopCoroutine(running);
            running = null;
        }

        // A CharacterController ignores direct position changes unless it is switched off first
        if (cc != null) cc.enabled = false;

        transform.position = position;
        transform.localScale = scale;
        FixControllerSettings(scale.x);

        if (cc != null) cc.enabled = true;

        if (scaleMoveSpeed) controller.moveSpeed = baseMoveSpeed * scale.x;
        if (cam != null) cam.fieldOfView = baseFov;
    }

    IEnumerator ScaleRoutine(float target)
    {
        float startScale = transform.localScale.x;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float k = curve.Evaluate(Mathf.Clamp01(t));
            float s = Mathf.Lerp(startScale, target, k);

            ApplyScale(s);

            if (scaleMoveSpeed)
                controller.moveSpeed = baseMoveSpeed * s;

            if (fovKick && cam != null)
                cam.fieldOfView = baseFov + Mathf.Sin(k * Mathf.PI) * fovKickAmount;

            yield return null;
        }

        ApplyScale(target);
        if (cam != null) cam.fieldOfView = baseFov;
        running = null;
    }

    // Changes Alice's size but keeps her FEET on the same spot, so she
    // shrinks down to the ground instead of floating in the air.
    void ApplyScale(float s)
    {
        float oldFeetY = FeetY();

        transform.localScale = Vector3.one * s;
        FixControllerSettings(s);

        float gap = oldFeetY - FeetY();
        if (cc != null && cc.enabled && Mathf.Abs(gap) > 0.00001f)
            cc.Move(Vector3.up * gap);
    }

    float FeetY()
    {
        if (cc == null) return transform.position.y;
        return transform.position.y + (cc.center.y - cc.height * 0.5f) * transform.lossyScale.y;
    }

    // skinWidth and stepOffset stay the same size when Alice shrinks, which made a tiny Alice
    // "step up" onto things and hover. Scale them together with her.
    void FixControllerSettings(float s)
    {
        if (cc == null) return;
        float k = s / baseScale;
        cc.skinWidth = Mathf.Max(0.0001f, baseSkinWidth * k);
        cc.stepOffset = baseStepOffset * k;
        cc.minMoveDistance = 0f;
    }
}