using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Put this on an empty object with a Collider ("Is Trigger" ticked), placed where the
// key hunt begins (for example the entrance to the Queen's garden).
//
// When Alice walks into the zone:
//  - a countdown starts
//  - card guards start spawning around her and chase her
//
// She goes to JAIL (message shown, game freezes) if:
//  - a card guard touches her, OR
//  - the timer runs out before she gets the golden key
//
// If she picks up the golden key in time, the timer stops and the cards disappear.
public class KeyTimer : MonoBehaviour
{
    public static KeyTimer Instance;

    [Header("Timer")]
    public float timeLimit = 120f;            // seconds Alice has to find the key

    [Header("Card guards")]
    public GameObject cardPrefab;             // optional. If empty, a simple white card is built for you
    public Vector3 cardSize = new Vector3(2f, 3f, 0.2f);   // only used for the auto-built card
    public float cardSpawnInterval = 4f;      // seconds between new cards
    public int maxCards = 8;                  // most cards alive at once
    public float spawnRadius = 25f;           // how far from Alice cards appear
    public float cardSpeed = 4f;              // how fast cards chase her
    [Tooltip("Turns each card so its front faces Alice. If the card is sideways or backwards, try Y = 0, 90, 180 or -90.")]
    public Vector3 cardRotationOffset = new Vector3(0f, 90f, 0f);

    [Header("Sound (all optional)")]
    public AudioClip startSound;              // plays once when Alice hits the trigger (e.g. a trumpet / alarm)
    [Range(0f, 1f)] public float startVolume = 1f;
    public AudioClip chaseSound;              // loops while the cards are chasing her (e.g. tense music / marching)
    [Range(0f, 1f)] public float chaseVolume = 0.6f;
    public AudioClip cardSpawnSound;          // plays each time a new card appears
    [Range(0f, 1f)] public float cardSpawnVolume = 0.7f;
    public AudioClip caughtSound;             // plays when a card touches Alice
    [Range(0f, 1f)] public float caughtVolume = 1f;

    [Header("Jail")]
    public string jailMessage = "You spend the rest of your life in jail.";

    [Header("UI (optional - a small timer and a jail message are created for you if these are empty)")]
    public TMP_Text timerText;
    public TMP_Text messageText;              // also used for the jail message if assigned
    public string startMessage = "Find the golden key before time runs out! Watch out for the card guards!";

    private bool running;
    private bool finished;       // key found, or jailed
    private bool jailed;
    private float remaining;
    private float spawnTimer;
    private GameObject player;
    private TMP_Text autoLabel;
    private GameObject autoCanvas;
    private TMP_Text autoJailText;
    private readonly List<GameObject> cards = new List<GameObject>();
    private AudioSource sfxSource;     // one-shots (start, card spawn)
    private AudioSource loopSource;    // looping chase sound

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;   // in case a previous run ended in jail

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;   // 2D sound

        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.playOnAwake = false;
        loopSource.loop = true;
        loopSource.spatialBlend = 0f;

        if (timerText == null) BuildUI();
        if (messageText == null) BuildJailUI();
        SetTimerVisible(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (running || finished) return;
        if (other.GetComponent<PlayerController>() == null) return;
        if (GoldenKey.HasKey) return;   // already has the key, no need for a timer

        player = other.gameObject;
        remaining = timeLimit;
        spawnTimer = 0f;                // first card appears right away
        running = true;

        SetTimerVisible(true);
        if (messageText != null) messageText.text = startMessage;

        if (startSound != null) sfxSource.PlayOneShot(startSound, startVolume);
        if (chaseSound != null)
        {
            loopSource.clip = chaseSound;
            loopSource.volume = chaseVolume;
            loopSource.Play();
        }
        Debug.Log("[KeyTimer] Timer started: " + timeLimit + " seconds");
    }

    void Update()
    {
        if (!running) return;

        // Key found in time
        if (GoldenKey.HasKey)
        {
            running = false;
            finished = true;
            SetTimerVisible(false);
            ClearCards();
            loopSource.Stop();
            Debug.Log("[KeyTimer] Key found, timer stopped");
            return;
        }

        remaining -= Time.deltaTime;
        UpdateLabel();

        // Spawn card guards
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            spawnTimer = cardSpawnInterval;
            SpawnCard();
        }

        if (remaining <= 0f)
        {
            Debug.Log("[KeyTimer] Time is up!");
            SendToJail(player);
        }
    }

    // ---------- Card guards ----------

    void SpawnCard()
    {
        if (player == null) return;

        cards.RemoveAll(c => c == null);
        if (cards.Count >= maxCards) return;

        Vector2 circle = Random.insideUnitCircle.normalized * spawnRadius;
        Vector3 pos = player.transform.position + new Vector3(circle.x, 0f, circle.y);

        GameObject card;
        if (cardPrefab != null)
        {
            card = Instantiate(cardPrefab, pos, Quaternion.identity);
        }
        else
        {
            card = GameObject.CreatePrimitive(PrimitiveType.Cube);
            card.name = "CardGuard";
            card.transform.position = pos;
            card.transform.localScale = cardSize;
        }

        CardGuard guard = card.GetComponent<CardGuard>();
        if (guard == null) guard = card.AddComponent<CardGuard>();
        guard.Init(player.transform, cardSpeed, cardRotationOffset);

        cards.Add(card);

        if (cardSpawnSound != null) sfxSource.PlayOneShot(cardSpawnSound, cardSpawnVolume);
    }

    void ClearCards()
    {
        foreach (GameObject c in cards)
            if (c != null) Destroy(c);
        cards.Clear();
    }

    // ---------- Jail ----------

    // Called when a card touches Alice, or when time runs out.
    public void SendToJail(GameObject who, bool caughtByCard = false)
    {
        if (jailed) return;
        if (GoldenKey.HasKey && !running) return;   // already escaped

        if (caughtByCard && caughtSound != null) sfxSource.PlayOneShot(caughtSound, caughtVolume);

        jailed = true;
        running = false;
        finished = true;
        SetTimerVisible(false);
        ClearCards();
        loopSource.Stop();

        Debug.Log("[KeyTimer] Alice goes to jail.");

        if (messageText != null) messageText.text = jailMessage;
        if (autoJailText != null)
        {
            autoJailText.text = jailMessage;
            autoJailText.transform.parent.gameObject.SetActive(true);
        }

        // Stop Alice from moving, then freeze the game
        GameObject p = who != null ? who : player;
        if (p != null)
        {
            PlayerController pc = p.GetComponent<PlayerController>();
            if (pc != null) pc.enabled = false;
        }
        Time.timeScale = 0f;
    }

    // ---------- UI ----------

    void UpdateLabel()
    {
        int secs = Mathf.Max(0, Mathf.CeilToInt(remaining));
        string t = (secs / 60) + ":" + (secs % 60).ToString("00");
        string label = "Find the key  " + t;

        TMP_Text target = timerText != null ? timerText : autoLabel;
        if (target == null) return;

        target.text = label;
        target.color = remaining <= 10f ? new Color(1f, 0.35f, 0.35f) : Color.white;
    }

    void SetTimerVisible(bool visible)
    {
        if (timerText != null) timerText.gameObject.SetActive(visible);
        if (autoCanvas != null) autoCanvas.SetActive(visible);
        if (visible) UpdateLabel();
    }

    // Small timer in the top right corner, built automatically
    void BuildUI()
    {
        autoCanvas = new GameObject("KeyTimerCanvas");
        Canvas canvas = autoCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;

        CanvasScaler scaler = autoCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject panel = new GameObject("TimerPanel", typeof(RectTransform));
        panel.transform.SetParent(autoCanvas.transform, false);
        RectTransform pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(1, 1);
        pr.anchorMax = new Vector2(1, 1);
        pr.pivot = new Vector2(1, 1);
        pr.anchoredPosition = new Vector2(-40, -40);
        pr.sizeDelta = new Vector2(380, 70);

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.16f, 0.09f, 0.12f, 0.85f);
        bg.raycastTarget = false;

        GameObject textGO = new GameObject("TimerText", typeof(RectTransform));
        textGO.transform.SetParent(panel.transform, false);
        RectTransform tr = textGO.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(15, 5);
        tr.offsetMax = new Vector2(-15, -5);

        TextMeshProUGUI label = textGO.AddComponent<TextMeshProUGUI>();
        label.fontSize = 32;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        autoLabel = label;
    }

    // Big message in the middle of the screen, only shown when Alice is sent to jail
    void BuildJailUI()
    {
        GameObject canvasGO = new GameObject("JailCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject panel = new GameObject("JailPanel", typeof(RectTransform));
        panel.transform.SetParent(canvasGO.transform, false);
        RectTransform pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = Vector2.zero;
        pr.anchorMax = Vector2.one;
        pr.offsetMin = Vector2.zero;
        pr.offsetMax = Vector2.zero;

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.8f);
        bg.raycastTarget = false;

        GameObject textGO = new GameObject("JailText", typeof(RectTransform));
        textGO.transform.SetParent(panel.transform, false);
        RectTransform tr = textGO.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0.1f, 0.3f);
        tr.anchorMax = new Vector2(0.9f, 0.7f);
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;

        TextMeshProUGUI label = textGO.AddComponent<TextMeshProUGUI>();
        label.fontSize = 72;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 0.35f, 0.35f);
        label.raycastTarget = false;
        autoJailText = label;

        panel.SetActive(false);
    }
}