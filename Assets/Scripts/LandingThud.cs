using UnityEngine;

/// <summary>
/// A hard, sudden JERK of the camera when the player lands.
/// The camera snaps down instantly on impact, freezes for a split second,
/// then recovers fast with a quick rattle. No floaty wobble.
///
/// SETUP
/// 1. Make an empty GameObject called "CameraShake" as a child of your player.
/// 2. Make your Main Camera a child of "CameraShake".
/// 3. Put THIS script on "CameraShake" (not on the camera itself).
/// 4. If you use a CharacterController, drag it into the Controller slot.
///    Otherwise call GetComponent<LandingThud>().Thud(1f) from your own landing code.
/// 5. Optional: drag the camera into the Cam slot for a FOV punch on impact.
/// </summary>
public class LandingThud : MonoBehaviour
{
    [Header("Landing detection (optional)")]
    public CharacterController controller;
    public float minFallSpeed = 4f;
    public float maxFallSpeed = 30f;

    [Header("Jerk strength (at full intensity)")]
    [Tooltip("Instant camera drop in meters.")]
    public float maxDip = 0.5f;
    [Tooltip("Instant forward pitch in degrees.")]
    public float maxPitch = 12f;
    [Tooltip("Random sideways roll in degrees.")]
    public float maxRoll = 4f;

    [Header("Timing")]
    [Tooltip("Seconds the camera stays frozen at the bottom of the hit. Gives the 'jolt' feel.")]
    public float holdTime = 0.06f;
    [Tooltip("Seconds to recover. Smaller = harder, snappier.")]
    public float recoverTime = 0.18f;

    [Header("Rattle after impact")]
    public float shakeDuration = 0.2f;
    public float shakePosition = 0.03f;
    public float shakeRotation = 1.5f;
    public float shakeSpeed = 60f;

    [Header("FOV punch (optional)")]
    public Camera cam;
    [Tooltip("Degrees the FOV snaps by on impact (negative = squeeze in).")]
    public float fovPunch = -8f;

    Vector3 posOffset, posVel;
    Vector3 rotOffset, rotVel;
    Vector3 basePos;
    Quaternion baseRot;

    float holdTimer;
    float shakeTimer;
    float shakeAmp;
    float baseFov, fovOffset, fovVel;

    bool wasGrounded;
    float lastYVelocity;

    void Awake()
    {
        basePos = transform.localPosition;
        baseRot = transform.localRotation;
        if (cam != null) baseFov = cam.fieldOfView;
    }

    void Update()
    {
        if (controller == null) return;

        bool grounded = controller.isGrounded;

        if (grounded && !wasGrounded)
        {
            float fallSpeed = -lastYVelocity;
            if (fallSpeed > minFallSpeed)
            {
                Thud(Mathf.InverseLerp(minFallSpeed, maxFallSpeed, fallSpeed));
            }
        }

        wasGrounded = grounded;
        lastYVelocity = controller.velocity.y;
    }

    /// <summary>Trigger the jerk. strength: 0 (small) to 1 (full).</summary>
    public void Thud(float strength)
    {
        // keep even small landings noticeable
        strength = Mathf.Clamp(strength, 0.35f, 1f);

        // INSTANT snap, no easing in
        posOffset = Vector3.down * (maxDip * strength);
        rotOffset = new Vector3(maxPitch * strength, 0f, Random.Range(-maxRoll, maxRoll) * strength);
        posVel = Vector3.zero;
        rotVel = Vector3.zero;

        holdTimer = holdTime;
        shakeTimer = shakeDuration;
        shakeAmp = strength;

        if (cam != null)
        {
            fovOffset = fovPunch * strength;
            fovVel = 0f;
        }
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;

        // freeze at the bottom of the hit for a moment
        if (holdTimer > 0f)
        {
            holdTimer -= dt;
        }
        else
        {
            // critically damped = fast, no floaty overshoot
            posOffset = Vector3.SmoothDamp(posOffset, Vector3.zero, ref posVel, recoverTime);
            rotOffset = Vector3.SmoothDamp(rotOffset, Vector3.zero, ref rotVel, recoverTime);
            fovOffset = Mathf.SmoothDamp(fovOffset, 0f, ref fovVel, recoverTime);
        }

        // quick rattle that dies off fast
        Vector3 shakePos = Vector3.zero;
        Vector3 shakeRot = Vector3.zero;
        if (shakeTimer > 0f)
        {
            shakeTimer -= dt;
            float t = Mathf.Clamp01(shakeTimer / shakeDuration);
            float fade = t * t * shakeAmp;
            float time = Time.time * shakeSpeed;

            shakePos = new Vector3(
                (Mathf.PerlinNoise(time, 0f) - 0.5f) * 2f,
                (Mathf.PerlinNoise(0f, time) - 0.5f) * 2f,
                0f) * (shakePosition * fade);

            shakeRot = new Vector3(
                (Mathf.PerlinNoise(time, 10f) - 0.5f) * 2f,
                (Mathf.PerlinNoise(10f, time) - 0.5f) * 2f,
                (Mathf.PerlinNoise(time, 20f) - 0.5f) * 2f) * (shakeRotation * fade);
        }

        transform.localPosition = basePos + posOffset + shakePos;
        transform.localRotation = baseRot * Quaternion.Euler(rotOffset + shakeRot);

        if (cam != null) cam.fieldOfView = baseFov + fovOffset;
    }
}