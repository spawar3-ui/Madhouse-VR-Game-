using UnityEngine;

// Put this on the potion bottle. Needs a Collider with "Is Trigger" checked.
// Walk up to it and press E to drink.
public class Potion : MonoBehaviour
{
    public float shrinkTo = 0.1f;     // 0.1 = 10% of normal size
    public KeyCode drinkKey = KeyCode.E;
    public GameObject drinkPrompt;    // optional UI text: "Press E to drink"

    private PlayerShrink player;

    void OnTriggerEnter(Collider other)
    {
        player = other.GetComponent<PlayerShrink>();
        if (player != null && drinkPrompt != null) drinkPrompt.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerShrink>() == player)
        {
            player = null;
            if (drinkPrompt != null) drinkPrompt.SetActive(false);
        }
    }

    void Update()
    {
        if (player != null && Input.GetKeyDown(drinkKey))
        {
            player.ScaleTo(shrinkTo);
            if (drinkPrompt != null) drinkPrompt.SetActive(false);
            Destroy(gameObject);   // bottle is gone once drunk
        }
    }
}
