using UnityEngine;
using UnityEngine.UI;

// A small dot in the middle of the screen. It changes color when Alice's
// CharacterController is touching another collider, so the player knows they can press E.
//
// SETUP: put this on the PLAYER object (the one that has the CharacterController
// and PlayerController). That's all. The dot is built for you.
//
// Other scripts can check ContactIndicator.IsTouching to know when E should work.
[RequireComponent(typeof(CharacterController))]
public class ContactIndicator : MonoBehaviour
{
    public static bool IsTouching { get; private set; }

    [Header("Detection")]
    [Tooltip("How far past Alice's body a collider can be and still count as touching.")]
    public float reach = 0.25f;
    [Tooltip("Only colliders on these layers count.")]
    public LayerMask layers = ~0;

    [Header("Dot")]
    public Color idleColor = new Color(1f, 1f, 1f, 0.55f);
    public Color touchingColor = new Color(0.2f, 1f, 0.4f, 1f);
    public float dotSize = 18f;                 // pixels (at 1080p)
    public float touchingScale = 1.6f;          // dot grows a little when touching
    public float colorSpeed = 14f;              // how fast it changes color

    [Header("Optional")]
    [Tooltip("Leave empty and a dot is built for you.")]
    public Image dot;

    private CharacterController controller;
    private readonly Collider[] hits = new Collider[16];
    private RectTransform dotRect;
    private float scale = 1f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (dot == null) BuildDot();
        dotRect = dot.rectTransform;
        dot.color = idleColor;
    }

    void Update()
    {
        IsTouching = CheckTouching();

        Color target = IsTouching ? touchingColor : idleColor;
        dot.color = Color.Lerp(dot.color, target, 1f - Mathf.Exp(-colorSpeed * Time.unscaledDeltaTime));

        float targetScale = IsTouching ? touchingScale : 1f;
        scale = Mathf.Lerp(scale, targetScale, 1f - Mathf.Exp(-colorSpeed * Time.unscaledDeltaTime));
        dotRect.localScale = new Vector3(scale, scale, 1f);
    }

    void OnDisable()
    {
        IsTouching = false;
    }

    bool CheckTouching()
    {
        // A capsule slightly bigger than the character controller.
        // It starts just above the step height so the floor under Alice doesn't count.
        Vector3 c = transform.TransformPoint(controller.center);
        float r = controller.radius + reach;
        float bottomY = c.y - controller.height * 0.5f + controller.stepOffset + 0.05f;
        float topY = c.y + controller.height * 0.5f;

        Vector3 p1 = new Vector3(c.x, bottomY + r, c.z);
        Vector3 p2 = new Vector3(c.x, Mathf.Max(p1.y, topY - r), c.z);

        int count = Physics.OverlapCapsuleNonAlloc(p1, p2, r, hits, layers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider col = hits[i];
            if (col == null) continue;
            if (col.transform.IsChildOf(transform)) continue;   // ignore Alice herself
            return true;
        }
        return false;
    }

    // Small round dot in the center of the screen, built automatically
    void BuildDot()
    {
        GameObject canvasGO = new GameObject("ContactDotCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject dotGO = new GameObject("ContactDot", typeof(RectTransform));
        dotGO.transform.SetParent(canvasGO.transform, false);
        RectTransform rt = dotGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(dotSize, dotSize);

        dot = dotGO.AddComponent<Image>();
        dot.sprite = MakeCircleSprite(64);
        dot.raycastTarget = false;
    }

    static Sprite MakeCircleSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half));
                float a = Mathf.Clamp01(half - d);   // soft 1px edge
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
