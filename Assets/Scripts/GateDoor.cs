using UnityEngine;
using TMPro;

// Put this on an invisible trigger zone in front of the GATE
// (an empty object with a Box Collider, "Is Trigger" ticked, bigger than the gate).
// When Alice walks in WITH the key, the gate opens automatically.
public class GateDoor : MonoBehaviour
{
    [Header("Gate")]
    public Animator gateAnimator;      // the gate's Animator (with an "Open" trigger)
    public string openTrigger = "Open";
    public GameObject gateObject;      // used only if no Animator is assigned (it just disappears)

    [Header("Messages (optional)")]
    public TMP_Text messageText;
    public string lockedMessage = "The gate is locked. I need to find the golden key.";
    public string openMessage = "The key fits! The gate is open!";

    [Header("Ending (optional)")]
    public GameObject endingObject;    // e.g. a "The End" UI panel, turned on when the gate opens

    [Header("Sound")]
    public AudioClip gateOpenSound;
    [Range(0f, 1f)] public float gateOpenVolume = 1f;

    private bool opened;
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;   // 2D sound
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("[GateDoor] Trigger entered by: " + other.name);
        if (opened) return;
        if (other.GetComponent<PlayerController>() == null)
        {
            Debug.Log("[GateDoor] " + other.name + " has no PlayerController, ignoring.");
            return;
        }

        if (GoldenKey.HasKey)
        {
            opened = true;
            Debug.Log("[GateDoor] Opening gate");

            if (messageText != null) messageText.text = openMessage;
            if (gateOpenSound != null) audioSource.PlayOneShot(gateOpenSound, gateOpenVolume);

            if (gateAnimator != null) gateAnimator.SetTrigger(openTrigger);
            else if (gateObject != null) gateObject.SetActive(false);

            if (endingObject != null) endingObject.SetActive(true);
        }
        else
        {
            Debug.Log("[GateDoor] Player has no key yet");
            if (messageText != null) messageText.text = lockedMessage;
        }
    }
}