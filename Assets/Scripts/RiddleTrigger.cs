using UnityEngine;

// Put this on an empty object with a Collider ("Is Trigger" ticked) placed where
// you want the riddles to begin, for example where Alice first meets the Mad Hatter.
// When Alice walks in, the first riddle appears.
public class RiddleTrigger : MonoBehaviour
{
    private bool used;

    void OnTriggerEnter(Collider other)
    {
        if (used) return;
        if (other.GetComponent<PlayerController>() == null) return;

        if (RiddleBoard.Instance == null)
        {
            Debug.LogError("[RiddleTrigger] No RiddleBoard in the scene! Add RiddleBoard.cs to an object.");
            return;
        }

        used = true;
        RiddleBoard.Instance.BeginRiddles();
    }
}
