using UnityEngine;
using TMPro;

// Put this on the GOLDEN KEY. Needs a Collider with "Is Trigger" checked.
// Walk close and press E to pick it up.
public class GoldenKey : MonoBehaviour
{
    public static bool HasKey;

    public KeyCode pickupKey = KeyCode.E;
    public GameObject prompt;          // optional "Press E to pick up" UI
    public TMP_Text messageText;       // optional message text
    public AudioClip pickupSound;      // sound when Alice takes the key
    [Range(0f, 1f)] public float pickupVolume = 1f;
    public GameObject pickupEffect;    // optional particle prefab

    private bool playerNear;

    // Makes sure the key flag resets every time you press Play
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetFlag() { HasKey = false; }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("[GoldenKey] Trigger entered by: " + other.name);
        if (other.GetComponent<PlayerController>() == null)
        {
            Debug.Log("[GoldenKey] " + other.name + " has no PlayerController, ignoring.");
            return;
        }
        playerNear = true;
        Debug.Log("[GoldenKey] Player is near the key. Press " + pickupKey);
        if (prompt != null) prompt.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        playerNear = false;
        if (prompt != null) prompt.SetActive(false);
    }

    void Update()
    {
        if (playerNear && Input.GetKeyDown(pickupKey))
        {
            HasKey = true;
            Debug.Log("[GoldenKey] Key picked up");

            if (messageText != null)
                messageText.text = "You found the golden key! Take it to the gate.";
            PlaySound2D();
            if (pickupEffect != null)
                Instantiate(pickupEffect, transform.position, Quaternion.identity);
            if (prompt != null) prompt.SetActive(false);

            Destroy(gameObject);
        }
    }

    // Plays the sound on a temporary object, so it keeps playing after the key is destroyed.
    void PlaySound2D()
    {
        if (pickupSound == null) return;

        GameObject temp = new GameObject("KeyPickupSound");
        AudioSource src = temp.AddComponent<AudioSource>();
        src.clip = pickupSound;
        src.volume = pickupVolume;
        src.spatialBlend = 0f;   // 2D: same volume no matter how small Alice is
        src.Play();
        Destroy(temp, pickupSound.length + 0.1f);
    }
}