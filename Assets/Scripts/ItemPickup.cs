using UnityEngine;

// Put this on each of the 3 objects hidden in the garden.
// Needs a Collider with "Is Trigger" checked. Walk close and press E.
// DEBUG VERSION: prints messages to the Console to help find problems.
public class ItemPickup : MonoBehaviour
{
    public string itemName = "Teacup";
    public KeyCode pickupKey = KeyCode.E;
    public GameObject prompt;          // optional "Press E" UI

    [Header("Sound")]
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float pickupVolume = 1f;

    private bool playerNear;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("[ItemPickup] Trigger entered by: " + other.name);

        if (other.GetComponent<PlayerController>() == null)
        {
            Debug.Log("[ItemPickup] " + other.name + " has no PlayerController, ignoring.");
            return;
        }

        playerNear = true;
        Debug.Log("[ItemPickup] Player is near " + itemName);
        if (prompt != null) prompt.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;

        playerNear = false;
        Debug.Log("[ItemPickup] Player left " + itemName);
        if (prompt != null) prompt.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(pickupKey))
            Debug.Log("[ItemPickup] " + pickupKey + " pressed. playerNear = " + playerNear);

        if (playerNear && Input.GetKeyDown(pickupKey))
        {
            Debug.Log("[ItemPickup] Picking up " + itemName);

            if (TeaPartyQuest.Instance == null)
            {
                Debug.LogError("[ItemPickup] No TeaPartyQuest in the scene! Add it to the tea table.");
                return;
            }

            TeaPartyQuest.Instance.Collect(itemName);
            PlaySound2D();

            if (prompt != null) prompt.SetActive(false);
            Destroy(gameObject);
        }
    }

    // Plays the sound on a temporary object, so it keeps playing after this item is destroyed.
    void PlaySound2D()
    {
        if (pickupSound == null) return;

        GameObject temp = new GameObject("PickupSound");
        AudioSource src = temp.AddComponent<AudioSource>();
        src.clip = pickupSound;
        src.volume = pickupVolume;
        src.spatialBlend = 0f;   // 2D: same volume no matter how small Alice is
        src.Play();
        Destroy(temp, pickupSound.length + 0.1f);
    }
}