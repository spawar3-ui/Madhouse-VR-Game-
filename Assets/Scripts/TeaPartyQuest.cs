using UnityEngine;
using TMPro;

// Put this on the TEA TABLE. Needs a Collider with "Is Trigger" checked
// (make it a bit larger than the table so Alice can stand next to it).
// DEBUG VERSION: prints messages to the Console to help find problems.
public class TeaPartyQuest : MonoBehaviour
{
    public static TeaPartyQuest Instance;

    [Header("Quest")]
    public int itemsNeeded = 3;
    public KeyCode deliverKey = KeyCode.E;

    [Header("UI (optional)")]
    public TMP_Text counterText;       // shows "Items: 1/3"
    public TMP_Text messageText;       // shows hints and dialogue

    [Header("What happens when done")]
    public GameObject blocker;         // the thing blocking the path (hedge, gate, etc.)
    public Animator blockerAnimator;   // optional: play an open animation instead
    public string openTrigger = "Open";

    [Header("Alice size")]
    public float normalScale = 1f;     // Alice's original size

    [Header("Sounds (both play at the same time when the gate opens)")]
    public AudioClip deliverSound1;
    public AudioClip deliverSound2;
    [Range(0f, 1f)] public float deliverVolume1 = 1f;
    [Range(0f, 1f)] public float deliverVolume2 = 1f;

    private int collected;
    private bool delivered;
    private bool playerAtTable;
    private PlayerShrink playerShrink;
    private AudioSource source1;
    private AudioSource source2;

    void Awake()
    {
        Instance = this;

        source1 = gameObject.AddComponent<AudioSource>();
        source2 = gameObject.AddComponent<AudioSource>();
        foreach (var s in new[] { source1, source2 })
        {
            s.playOnAwake = false;
            s.spatialBlend = 0f;   // 2D sound
        }

        UpdateUI();
        Say("The path is blocked! The Hatter says: bring me three things for the tea party.");
    }

    public void Collect(string itemName)
    {
        collected = Mathf.Min(collected + 1, itemsNeeded);
        Debug.Log("[TeaParty] Collected " + itemName + " (" + collected + "/" + itemsNeeded + ")");
        UpdateUI();
        Say("You found the " + itemName + "! (" + collected + "/" + itemsNeeded + ")");

        // Cross the riddle off the Hatter's list
        if (RiddleBoard.Instance != null) RiddleBoard.Instance.MarkFound(itemName);
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("[TeaParty] Trigger entered by: " + other.name);
        if (other.GetComponent<PlayerController>() == null) return;

        playerAtTable = true;
        playerShrink = other.GetComponent<PlayerShrink>();
        Debug.Log("[TeaParty] Player at table. PlayerShrink found: " + (playerShrink != null));

        if (delivered) return;
        if (collected >= itemsNeeded) Say("Press E to put everything on the table.");
        else Say("Not enough yet! Find " + (itemsNeeded - collected) + " more.");
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null)
        {
            playerAtTable = false;
            Debug.Log("[TeaParty] Player left the table area");
        }
    }

    void Update()
    {
        if (delivered || !playerAtTable) return;

        if (Input.GetKeyDown(deliverKey))
            Debug.Log("[TeaParty] E pressed. collected = " + collected + "/" + itemsNeeded);

        if (collected >= itemsNeeded && Input.GetKeyDown(deliverKey))
        {
            delivered = true;
            Debug.Log("[TeaParty] Delivering items!");
            Say("Perfect! The tea party can begin... the path is open!");

            // Two sounds at the same moment
            if (deliverSound1 != null) source1.PlayOneShot(deliverSound1, deliverVolume1);
            if (deliverSound2 != null) source2.PlayOneShot(deliverSound2, deliverVolume2);

            // Fallback: find PlayerShrink anywhere in the scene if it wasn't on the object that entered
            if (playerShrink == null)
                playerShrink = FindFirstObjectByType<PlayerShrink>();

            if (playerShrink != null)
            {
                Debug.Log("[TeaParty] Growing Alice to scale " + normalScale);
                playerShrink.ScaleTo(normalScale);
            }
            else
            {
                Debug.LogError("[TeaParty] No PlayerShrink found! Add PlayerShrink.cs to the Player object.");
            }

            if (blockerAnimator != null) blockerAnimator.SetTrigger(openTrigger);
            else if (blocker != null) blocker.SetActive(false);
        }
    }

    void UpdateUI()
    {
        if (counterText != null) counterText.text = "Items: " + collected + "/" + itemsNeeded;
    }

    void Say(string msg)
    {
        if (messageText != null) messageText.text = msg;
    }
}