using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Put this on ANY object in the scene (for example the tea table).
// It builds its own on-screen riddle card, so no UI setup is needed.
//
// The card stays hidden until Alice walks into a RiddleTrigger zone.
// Then the riddles appear ONE AT A TIME: pick up the item, and the next riddle shows.
//
//  - Tab : hide / show the card
//  - H   : reveal the hint for the current riddle right away
//  - The hint also appears by itself after "Hint Delay" seconds
//
// Each riddle's "Item Name" must match the "Item Name" on the ItemPickup object.
public class RiddleBoard : MonoBehaviour
{
    public static RiddleBoard Instance;

    [System.Serializable]
    public class Riddle
    {
        public string itemName;
        [TextArea(2, 6)] public string riddle;
        [TextArea(1, 3)] public string hint;
        [Tooltip("Optional. Drag the item object here. If empty, it is found automatically by matching Item Name.")]
        public GameObject itemObject;
        [HideInInspector] public bool found;
    }

    [Header("Riddles (shown in this order)")]
    public List<Riddle> riddles = new List<Riddle>
    {
        new Riddle
        {
            itemName = "Teacup",
            riddle = "I have a handle, but no hand to hold.\nI have a lip, but never a word.\nI am full of warmth when the tea is poured, and lonely when the party is over.\nSeek me in the shade of a giant mushroom.",
            hint = "Look low to the ground, near the biggest mushrooms."
        },
        new Riddle
        {
            itemName = "Pocket Watch",
            riddle = "I have a face, but I never smile.\nI have hands, but I cannot clap.\nI am always running, yet I never arrive, and here it is always six o'clock.\nSeek me among the painted roses.",
            hint = "Search the rose garden, where the cards paint the flowers red."
        },
        new Riddle
        {
            itemName = "Sugar Bowl",
            riddle = "I am white, but I am not snow.\nI am sweet, but I am not a song.\nI vanish in the tea, yet the Hatter cannot go on without me.\nI wait where the lost things belong, behind the hedge.",
            hint = "Check behind the tall hedge at the edge of the garden."
        }
    };

    [Header("Controls")]
    public KeyCode toggleKey = KeyCode.Tab;
    public KeyCode hintKey = KeyCode.H;

    [Header("Timing")]
    public float hintDelay = 60f;        // seconds after a riddle appears before its hint shows
    public float foundMessageTime = 2.5f; // how long "found!" shows before the next riddle
    public float doneMessageTime = 6f;    // how long the final message stays up

    [Header("Look")]
    public float panelWidth = 640f;
    public float fontSize = 26f;
    public Color panelColor = new Color(0.16f, 0.09f, 0.12f, 0.88f);
    public Color titleColor = new Color(1f, 0.85f, 0.45f);
    public Color hintColor = new Color(1f, 0.78f, 0.45f);

    private enum Mode { Riddle, Found, Done }

    private CanvasGroup group;
    private RectTransform panelRect;
    private TextMeshProUGUI text;

    private bool started;
    private Mode mode;
    private int currentIndex = -1;
    private string lastFoundName;
    private bool hintsUnlocked;
    private bool hiddenByUser;
    private bool hideAll;
    private float riddleTimer;
    private float foundTimer;
    private float doneTimer;

    void Awake()
    {
        Instance = this;
        BuildUI();

        // Link each riddle to its item, then hide all items until their riddle shows
        LinkItems();
        foreach (var r in riddles)
            if (r.itemObject != null) r.itemObject.SetActive(false);
    }

    // Finds each item by name (if you didn't drag it in). Runs before items are hidden.
    void LinkItems()
    {
        ItemPickup[] pickups = FindObjectsOfType<ItemPickup>();
        foreach (var r in riddles)
        {
            if (r.itemObject != null) continue;
            foreach (var p in pickups)
            {
                if (string.Equals(p.itemName, r.itemName, System.StringComparison.OrdinalIgnoreCase))
                {
                    r.itemObject = p.gameObject;
                    break;
                }
            }
            if (r.itemObject == null)
                Debug.LogWarning("[RiddleBoard] No item found for riddle '" + r.itemName + "'. Drag it into the Item Object slot.");
        }
    }

    // Only the item for the current riddle is visible. The others stay hidden.
    void ShowOnlyCurrentItem()
    {
        for (int i = 0; i < riddles.Count; i++)
        {
            var r = riddles[i];
            if (r.itemObject == null) continue;   // already picked up (destroyed) or not linked
            r.itemObject.SetActive(!r.found && i == currentIndex);
        }
    }

    // Called by RiddleTrigger when Alice walks into the zone
    public void BeginRiddles()
    {
        if (started) return;
        started = true;
        Debug.Log("[RiddleBoard] Riddles started");
        ShowCurrent();
    }

    // Called by TeaPartyQuest when an item is collected
    public void MarkFound(string itemName)
    {
        bool matched = false;
        foreach (var r in riddles)
        {
            if (string.Equals(r.itemName, itemName, System.StringComparison.OrdinalIgnoreCase))
            {
                r.found = true;
                matched = true;
                lastFoundName = r.itemName;
            }
        }

        if (!started || !matched) return;

        mode = Mode.Found;
        foundTimer = foundMessageTime;
        hiddenByUser = false;
        Refresh();
    }

    void ShowCurrent()
    {
        currentIndex = -1;
        for (int i = 0; i < riddles.Count; i++)
        {
            if (!riddles[i].found) { currentIndex = i; break; }
        }

        hiddenByUser = false;
        ShowOnlyCurrentItem();

        if (currentIndex == -1)
        {
            mode = Mode.Done;
            doneTimer = doneMessageTime;
        }
        else
        {
            mode = Mode.Riddle;
            hintsUnlocked = false;
            riddleTimer = 0f;
        }
        Refresh();
    }

    void Update()
    {
        if (!started)
        {
            group.alpha = 0f;
            return;
        }

        if (Input.GetKeyDown(toggleKey)) hiddenByUser = !hiddenByUser;

        if (mode == Mode.Found)
        {
            foundTimer -= Time.deltaTime;
            if (foundTimer <= 0f) ShowCurrent();
        }
        else if (mode == Mode.Riddle)
        {
            riddleTimer += Time.deltaTime;
            if (!hintsUnlocked && (riddleTimer >= hintDelay || Input.GetKeyDown(hintKey)))
            {
                hintsUnlocked = true;
                Refresh();
            }
        }
        else if (mode == Mode.Done)
        {
            doneTimer -= Time.deltaTime;
            if (doneTimer <= 0f) hideAll = true;
        }

        float target = (!hiddenByUser && !hideAll) ? 1f : 0f;
        group.alpha = Mathf.MoveTowards(group.alpha, target, Time.deltaTime * 4f);
    }

    void Refresh()
    {
        if (text == null) return;

        string titleHex = ColorUtility.ToHtmlStringRGB(titleColor);
        string hintHex = ColorUtility.ToHtmlStringRGB(hintColor);
        var sb = new System.Text.StringBuilder();

        int foundCount = 0;
        foreach (var r in riddles) if (r.found) foundCount++;

        sb.Append("<color=#" + titleHex + "><b>The Hatter's Riddle</b></color>");

        if (mode == Mode.Riddle && currentIndex >= 0)
        {
            var r = riddles[currentIndex];
            sb.Append("  <size=65%><color=#bbbbbb>(" + (foundCount + 1) + " of " + riddles.Count + ")</color></size>\n\n");
            sb.Append(r.riddle + "\n");
            if (hintsUnlocked && !string.IsNullOrEmpty(r.hint))
                sb.Append("\n<size=85%><i><color=#" + hintHex + ">Hint: " + r.hint + "</color></i></size>\n");
            sb.Append("\n<size=65%><color=#aaaaaa>" + toggleKey + ": hide / show" +
                      (hintsUnlocked ? "" : "     " + hintKey + ": hint") + "</color></size>");
        }
        else if (mode == Mode.Found)
        {
            sb.Append("\n\n<color=#9fe39f>You found the " + lastFoundName + "!</color>\n");
            sb.Append("<size=70%><color=#bbbbbb>" + foundCount + " of " + riddles.Count + " found</color></size>");
        }
        else
        {
            sb.Append("\n\n<color=#9fe39f>That's all three!</color>\nBring everything back to the tea table.");
        }

        text.text = sb.ToString();

        float h = text.GetPreferredValues(text.text, panelWidth - 50f, 0f).y;
        panelRect.sizeDelta = new Vector2(panelWidth, h + 50f);
    }

    void BuildUI()
    {
        GameObject canvasGO = new GameObject("RiddleCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject panelGO = new GameObject("RiddlePanel", typeof(RectTransform));
        panelGO.transform.SetParent(canvasGO.transform, false);
        panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = new Vector2(40, -40);
        panelRect.sizeDelta = new Vector2(panelWidth, 300);

        Image bg = panelGO.AddComponent<Image>();
        bg.color = panelColor;
        bg.raycastTarget = false;

        group = panelGO.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        GameObject textGO = new GameObject("RiddleText", typeof(RectTransform));
        textGO.transform.SetParent(panelGO.transform, false);
        RectTransform tr = textGO.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(25, 25);
        tr.offsetMax = new Vector2(-25, -25);

        text = textGO.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = Color.white;
        text.enableWordWrapping = true;
        text.richText = true;
        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.TopLeft;
    }
}