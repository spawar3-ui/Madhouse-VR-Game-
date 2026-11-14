using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Shows a message the moment Alice picks up the golden key.
//
// SETUP: put this on any empty object in the scene (for example "KeyFoundMessage").
// That's it. If you leave "Message Text" empty, a banner is built for you at the top of the screen.
// It doesn't change GoldenKey, GateDoor or KeyTimer at all.
public class KeyFoundMessage : MonoBehaviour
{
    [Header("Message")]
    public string foundMessage = "Yayyy you escaped it!";
    public float showSeconds = 4f;            // how long the message stays on screen

    [Header("UI (optional - a banner is created for you if this is empty)")]
    public TMP_Text messageText;

    private bool wasHasKey;
    private bool showing;
    private float timer;
    private GameObject autoCanvas;
    private TMP_Text autoLabel;

    void Awake()
    {
        if (messageText == null) BuildUI();
        SetVisible(false);
        wasHasKey = GoldenKey.HasKey;   // so it only fires when the key is picked up during play
    }

    void Update()
    {
        bool has = GoldenKey.HasKey;
        if (has && !wasHasKey) Show();
        wasHasKey = has;

        if (showing)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                showing = false;
                SetVisible(false);
            }
        }
    }

    void Show()
    {
        Debug.Log("[KeyFoundMessage] Key found!");
        showing = true;
        timer = showSeconds;

        if (messageText != null) messageText.text = foundMessage;
        if (autoLabel != null) autoLabel.text = foundMessage;
        SetVisible(true);
    }

    void SetVisible(bool visible)
    {
        if (autoCanvas != null) autoCanvas.SetActive(visible);
        else if (messageText != null) messageText.gameObject.SetActive(visible);
    }

    // Banner at the top-center of the screen, built automatically
    void BuildUI()
    {
        autoCanvas = new GameObject("KeyFoundCanvas");
        Canvas canvas = autoCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = autoCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject panel = new GameObject("KeyFoundPanel", typeof(RectTransform));
        panel.transform.SetParent(autoCanvas.transform, false);
        RectTransform pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.5f, 1f);
        pr.anchorMax = new Vector2(0.5f, 1f);
        pr.pivot = new Vector2(0.5f, 1f);
        pr.anchoredPosition = new Vector2(0f, -80f);
        pr.sizeDelta = new Vector2(900f, 120f);

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.16f, 0.09f, 0.12f, 0.85f);
        bg.raycastTarget = false;

        GameObject textGO = new GameObject("KeyFoundText", typeof(RectTransform));
        textGO.transform.SetParent(panel.transform, false);
        RectTransform tr = textGO.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(20, 10);
        tr.offsetMax = new Vector2(-20, -10);

        TextMeshProUGUI label = textGO.AddComponent<TextMeshProUGUI>();
        label.fontSize = 56;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 0.85f, 0.3f);
        label.raycastTarget = false;
        autoLabel = label;
    }
}
