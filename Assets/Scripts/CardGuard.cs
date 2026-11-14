using UnityEngine;

// You don't need to add this yourself. KeyTimer adds it to every card it spawns.
// (If you use your own Card Prefab, it's fine either way.)
// The card walks toward Alice. If it touches her, she goes to jail.
public class CardGuard : MonoBehaviour
{
    public float speed = 4f;
    public float lifetime = 0f;   // 0 = lives until the key hunt ends

    // Turns the card so its FRONT faces Alice. KeyTimer sets this for you.
    public Vector3 rotationOffset = new Vector3(0f, 90f, 0f);

    private Transform target;

    public void Init(Transform player, float moveSpeed, Vector3 faceOffset)
    {
        target = player;
        speed = moveSpeed;
        rotationOffset = faceOffset;
    }

    public void Init(Transform player, float moveSpeed)
    {
        target = player;
        speed = moveSpeed;
    }

    void Awake()
    {
        // Needs a trigger collider and a kinematic Rigidbody so Alice's
        // CharacterController registers the touch.
        Collider col = GetComponent<Collider>();
        if (col == null) col = gameObject.AddComponent<BoxCollider>();
        col.isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void Start()
    {
        if (lifetime > 0f) Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (target == null) return;

        // Chase Alice on the flat plane, face her
        Vector3 flat = target.position - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f) return;

        transform.position += flat.normalized * speed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up) * Quaternion.Euler(rotationOffset);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        Debug.Log("[CardGuard] Caught Alice!");
        if (KeyTimer.Instance != null)
            KeyTimer.Instance.SendToJail(other.gameObject, true);
    }
}