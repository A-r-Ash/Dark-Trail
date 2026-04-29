using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum TutorialType
{
    Shooting,
    Flashlight,
    FlashlightBoost,
    Trap,
    SmallCreature,
    BigCreature,
    SpeedyCreature,
    Checkpoint
}

[System.Serializable]
public class TutorialEntry
{
    public TutorialType type;
    public string       title;
    [TextArea(2, 5)]
    public string       body;
    public Sprite       icon;
}

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Appearance")]
    [SerializeField] private Color   panelColor  = new Color(0.04f, 0.02f, 0.02f, 0.93f);
    [SerializeField] private Color   titleColor  = new Color(0.95f, 0.52f, 0.12f);
    [SerializeField] private Color   bodyColor   = new Color(0.84f, 0.80f, 0.76f);
    [SerializeField] private Color   hintColor   = new Color(0.45f, 0.42f, 0.40f);
    [SerializeField] private Vector2 panelSize   = new Vector2(700f, 220f);
    [SerializeField] private float   panelYOffset = 190f;

    [Header("Tutorials")]
    [SerializeField] private TutorialEntry[] tutorials;

    // ── Internal ──────────────────────────────────────────────────────────────

    private readonly Queue<TutorialEntry> queue = new Queue<TutorialEntry>();
    private Dictionary<TutorialType, TutorialEntry> lookup;

    private GameObject panelRoot;
    private Image      iconImage;
    private Text       titleText;
    private Text       bodyText;
    private bool       isShowing;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        BuildLookup();
        BuildUI();
        panelRoot.SetActive(false);
    }

    void Update()
    {
        if (!isShowing) return;
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            Dismiss();
    }

    // ── Public ────────────────────────────────────────────────────────────────

    public void Show(TutorialType type)
    {
        if (!lookup.TryGetValue(type, out var entry)) return;
        queue.Enqueue(entry);
        if (!isShowing) ShowNext();
    }

    // ── Private ───────────────────────────────────────────────────────────────

    void ShowNext()
    {
        if (queue.Count == 0) { isShowing = false; panelRoot.SetActive(false); return; }

        var e = queue.Dequeue();
        isShowing = true;
        panelRoot.SetActive(true);

        titleText.text = e.title.ToUpper();
        bodyText.text  = e.body;

        bool hasIcon = e.icon != null;
        iconImage.gameObject.SetActive(hasIcon);
        if (hasIcon) iconImage.sprite = e.icon;
    }

    void Dismiss()
    {
        isShowing = false;
        panelRoot.SetActive(false);
        ShowNext();
    }

    void BuildLookup()
    {
        lookup = new Dictionary<TutorialType, TutorialEntry>();
        if (tutorials == null) return;
        foreach (var t in tutorials)
            lookup[t.type] = t;
    }

    // ── UI construction ───────────────────────────────────────────────────────

    void BuildUI()
    {
        var canvasGo        = new GameObject("TutorialCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas          = canvasGo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        var scaler                  = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight   = 0.5f;

        // Panel — bottom-centre
        var panel = Rect("TutorialPanel", canvasGo.transform,
                         new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                         new Vector2(0f, panelYOffset), panelSize);
        panelRoot = panel.gameObject;
        panel.gameObject.AddComponent<Image>().color = panelColor;

        // Left accent bar
        var bar = Rect("Accent", panel,
                       new Vector2(0f, 0f), new Vector2(0f, 1f),
                       new Vector2(3f, 0f), new Vector2(6f, 0f));
        bar.gameObject.AddComponent<Image>().color = titleColor;

        // Icon (optional, top-left)
        float iconSz = 52f;
        var iconRt = Rect("Icon", panel,
                          new Vector2(0f, 1f), new Vector2(0f, 1f),
                          new Vector2(28f + iconSz * 0.5f, -(16f + iconSz * 0.5f)),
                          new Vector2(iconSz, iconSz));
        iconImage = iconRt.gameObject.AddComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.color = new Color(1f, 1f, 1f, 0.85f);

        // Title — top-anchored, centre at 30 px from top, 40 px tall → rows 10–50
        var titleRt = Rect("Title", panel,
                           new Vector2(0f, 1f), new Vector2(1f, 1f),
                           new Vector2(24f, -30f), new Vector2(-48f, 40f));
        titleText = Label(titleRt.gameObject, "", 24, FontStyle.Bold, titleColor, TextAnchor.MiddleLeft);

        // Body — top-anchored, centre at 125 px from top, 100 px tall → rows 75–175
        // (25 px gap below title bottom at row 50)
        var bodyRt = Rect("Body", panel,
                          new Vector2(0f, 1f), new Vector2(1f, 1f),
                          new Vector2(24f, -125f), new Vector2(-48f, 100f));
        bodyText = Label(bodyRt.gameObject, "", 17, FontStyle.Normal, bodyColor, TextAnchor.UpperLeft);

        // Dismiss hint — bottom right
        var hintRt = Rect("Hint", panel,
                          new Vector2(1f, 0f), new Vector2(1f, 0f),
                          new Vector2(-20f, 20f), new Vector2(220f, 24f));
        Label(hintRt.gameObject, "[ E ]  continue", 13, FontStyle.Italic, hintColor, TextAnchor.MiddleRight);

        // Separator line above hint
        var sep = Rect("Sep", panel,
                       new Vector2(0f, 0f), new Vector2(1f, 0f),
                       new Vector2(0f, 44f), new Vector2(-24f, 1f));
        sep.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);
    }

    static RectTransform Rect(string name, Transform parent,
                              Vector2 ancMin, Vector2 ancMax,
                              Vector2 pos,   Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt              = go.AddComponent<RectTransform>();
        rt.anchorMin        = ancMin;
        rt.anchorMax        = ancMax;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        return rt;
    }

    static Text Label(GameObject go, string text, int size, FontStyle style,
                      Color color, TextAnchor align)
    {
        var t       = go.AddComponent<Text>();
        t.text      = text;
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize  = size;
        t.fontStyle = style;
        t.color     = color;
        t.alignment = align;
        return t;
    }

    // ── Inspector defaults (called when component is first added) ─────────────

    void Reset()
    {
        tutorials = new TutorialEntry[]
        {
            new TutorialEntry
            {
                type  = TutorialType.Shooting,
                title = "Shooting",
                body  = "Right-click to aim.\n" +
                        "Left-click to fire.  Press R to reload.\n" +
                        "You can only fire while aiming."
            },
            new TutorialEntry
            {
                type  = TutorialType.Flashlight,
                title = "Flashlight",
                body  = "Your flashlight keeps most creatures at bay.\n" +
                        "The battery drains over time — manage it carefully.\n" +
                        "Darkness is their domain."
            },
            new TutorialEntry
            {
                type  = TutorialType.FlashlightBoost,
                title = "Flashlight Boost",
                body  = "Activate a blinding burst of light to repel anything nearby.\n" +
                        "Drains battery fast — save it for emergencies."
            },
            new TutorialEntry
            {
                type  = TutorialType.Trap,
                title = "Spike Trap",
                body  = "Spike traps arm automatically and re-arm after firing.\n" +
                        "Watch the red glow — it pulses brighter just before it fires.\n" +
                        "They deal damage and then reset. Don't linger."
            },
            new TutorialEntry
            {
                type  = TutorialType.SmallCreature,
                title = "Small Creature",
                body  = "Chases you on sight and closes distance quickly.\n" +
                        "Shine your flashlight to drive it back.\n" +
                        "A single bullet puts it down for good."
            },
            new TutorialEntry
            {
                type  = TutorialType.BigCreature,
                title = "The Big One",
                body  = "Light only slows it — it is not afraid.\n" +
                        "Only bullets cause real damage.\n" +
                        "It never retreats. Keep your distance and keep shooting."
            },
            new TutorialEntry
            {
                type  = TutorialType.SpeedyCreature,
                title = "Speedy Creature",
                body  = "Extremely fast. It will close the gap before you react.\n" +
                        "Your flashlight sends it running — use it early.\n" +
                        "If it gets close, it's already too late."
            },
            new TutorialEntry
            {
                type  = TutorialType.Checkpoint,
                title = "Checkpoint",
                body  = "Walk through to save your progress.\n" +
                        "If you die, you will respawn at the last checkpoint.\n" +
                        "Find them. Use them."
            },
        };
    }
}
