using UnityEngine;
using UnityEngine.UI;

// Top-left HUD showing current threat level (1-5) and a progress bar to the next tier.
// Self-builds its UI — just add this component to any GameObject.
public class DifficultyIndicator : MonoBehaviour
{
    [Header("Appearance")]
    [SerializeField] private Color labelColor    = new Color(0.84f, 0.80f, 0.76f);
    [SerializeField] private Color barBgColor    = new Color(0.12f, 0.10f, 0.10f, 0.8f);
    [SerializeField] private Color barFillColor  = new Color(0.85f, 0.18f, 0.10f);
    [SerializeField] private Color panelBgColor  = new Color(0.04f, 0.02f, 0.02f, 0.80f);

    private Text         levelText;
    private RectTransform barFill;

    void Awake()
    {
        BuildUI();
    }

    void Start()
    {
        if (DifficultyManager.Instance != null)
            DifficultyManager.Instance.OnThreatChanged.AddListener(OnThreatChanged);
        Refresh();
    }

    void OnDestroy()
    {
        if (DifficultyManager.Instance != null)
            DifficultyManager.Instance.OnThreatChanged.RemoveListener(OnThreatChanged);
    }

    void Update()
    {
        UpdateBar();
    }

    void OnThreatChanged(int level)
    {
        Refresh();
    }

    void Refresh()
    {
        int lvl = DifficultyManager.Instance != null ? DifficultyManager.Instance.ThreatLevel : 1;
        if (levelText != null)
            levelText.text = $"THREAT  {new string('|', lvl)}{new string(' ', 5 - lvl)}\nLEVEL {lvl}";
    }

    void UpdateBar()
    {
        if (barFill == null || DifficultyManager.Instance == null) return;
        float progress = DifficultyManager.Instance.LevelProgress;
        barFill.anchorMax = new Vector2(progress, 1f);
    }

    // ── UI construction ───────────────────────────────────────────────────────

    void BuildUI()
    {
        var canvasGo = new GameObject("DifficultyCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas         = canvasGo.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;
        var scaler                 = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        // Panel — top-left
        var panel = MakeRect("DiffPanel", canvasGo.transform,
                             new Vector2(0f, 1f), new Vector2(0f, 1f),
                             new Vector2(90f, -50f), new Vector2(180f, 68f));
        panel.gameObject.AddComponent<Image>().color = panelBgColor;

        // Level text
        var textRt = MakeRect("LevelText", panel,
                               new Vector2(0f, 0f), new Vector2(1f, 1f),
                               new Vector2(8f, 0f), new Vector2(-8f, -20f));
        levelText = MakeLabel(textRt.gameObject, "", 14, FontStyle.Bold, labelColor, TextAnchor.MiddleLeft);

        // Progress bar background
        var barBg = MakeRect("BarBg", panel,
                              new Vector2(0f, 0f), new Vector2(1f, 0f),
                              new Vector2(8f, 10f), new Vector2(-16f, 8f));
        barBg.gameObject.AddComponent<Image>().color = barBgColor;

        // Progress bar fill — driven by anchorMax.x
        var fillRt = MakeRect("BarFill", barBg,
                               Vector2.zero, Vector2.one,
                               Vector2.zero, Vector2.zero);
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(0f, 1f);   // starts empty; updated in UpdateBar
        fillRt.sizeDelta = Vector2.zero;
        fillRt.anchoredPosition = Vector2.zero;
        fillRt.gameObject.AddComponent<Image>().color = barFillColor;
        barFill = fillRt;
    }

    static RectTransform MakeRect(string name, Transform parent,
                                  Vector2 ancMin, Vector2 ancMax,
                                  Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = ancMin;
        rt.anchorMax = ancMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static Text MakeLabel(GameObject go, string text, int size, FontStyle style,
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
}
