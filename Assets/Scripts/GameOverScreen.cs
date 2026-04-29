using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverScreen : MonoBehaviour
{
    [Header("Background")]
    [SerializeField] private Sprite  backgroundSprite;
    [SerializeField] private Color   overlayColor  = new Color(0f, 0f, 0f, 0.72f);
    [SerializeField] private Color   panelColor    = new Color(0.07f, 0.04f, 0.04f, 0.96f);

    [Header("Panel Size")]
    [SerializeField] private Vector2 panelSize = new Vector2(520f, 340f);

    [Header("Navigation")]
    [SerializeField] private string mainMenuScene = "MainMenu";

    [Header("UI Mode")]
    [Tooltip("Uncheck if you have your own Game Over canvas in the scene. " +
             "This prevents the script from building a second canvas on top of yours.")]
    [SerializeField] private bool buildUIAutomatically = true;

    private GameObject canvasRoot = null;
    private Button     respawnBtn;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (!buildUIAutomatically) return;
        Build();
        canvasRoot.SetActive(false);
    }

    void Start()
    {
        if (!buildUIAutomatically) return;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver.AddListener(Show);
            GameManager.Instance.OnGameRestart.AddListener(Hide);
        }
    }

    void OnDestroy()
    {
        if (!buildUIAutomatically) return;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver.RemoveListener(Show);
            GameManager.Instance.OnGameRestart.RemoveListener(Hide);
        }
    }

    // ── Show / Hide ───────────────────────────────────────────────────────────

    public void Show()
    {
        if (canvasRoot == null) return;
        canvasRoot.SetActive(true);
        UpdateRespawnButton();
    }

    public void Hide()
    {
        if (canvasRoot == null) return;
        canvasRoot.SetActive(false);
    }

    void UpdateRespawnButton()
    {
        if (respawnBtn == null) return;
        bool hasCheckpoint = GameManager.Instance != null && GameManager.Instance.HasCheckpoint;
        respawnBtn.interactable = hasCheckpoint;

        var colors = respawnBtn.colors;
        colors.disabledColor = new Color(0.12f, 0.12f, 0.12f, 0.6f);
        respawnBtn.colors = colors;
    }

    // ── Button actions ────────────────────────────────────────────────────────

    void RespawnAtCheckpoint()
    {
        GameManager.Instance?.RespawnAtCheckpoint();
    }

    void Restart()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RestartGame();
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    void Build()
    {
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        canvasRoot = new GameObject("GameOverCanvas");
        canvasRoot.transform.SetParent(transform, false);

        var canvas          = canvasRoot.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        canvasRoot.AddComponent<CanvasGroup>().ignoreParentGroups = true;

        var scaler                 = canvasRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasRoot.AddComponent<GraphicRaycaster>();

        // Full-screen dim
        var overlay = Rect("Overlay", canvasRoot.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        overlay.gameObject.AddComponent<Image>().color = overlayColor;

        // Panel — centred
        var panel = Rect("Panel", canvasRoot.transform,
                         new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                         Vector2.zero, panelSize);
        var panelImg = panel.gameObject.AddComponent<Image>();
        if (backgroundSprite != null) { panelImg.sprite = backgroundSprite; panelImg.type = Image.Type.Sliced; }
        else panelImg.color = panelColor;

        // Title — top-anchored
        var titleRt = Rect("Title", panel,
                           new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                           new Vector2(0f, -55f), new Vector2(panelSize.x - 40f, 72f));
        Label(titleRt.gameObject, "GAME OVER", 48, FontStyle.Bold, new Color(0.9f, 0.15f, 0.15f));

        // Buttons — top-anchored, stacked
        var respawnRt = MakeButton(panel, "RESPAWN AT CHECKPOINT", new Vector2(0f, -140f), RespawnAtCheckpoint,
                                   new Color(0.10f, 0.22f, 0.10f), new Color(0.18f, 0.38f, 0.18f));
        respawnBtn = respawnRt.GetComponent<Button>();
        MakeButton(panel, "RESTART",   new Vector2(0f, -205f), Restart);
        MakeButton(panel, "MAIN MENU", new Vector2(0f, -265f), LoadMainMenu);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static RectTransform Rect(string name, Transform parent,
                              Vector2 ancMin, Vector2 ancMax,
                              Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return rt;
    }

    static void Label(GameObject go, string text, int size, FontStyle style, Color color)
    {
        var t = go.AddComponent<Text>();
        t.text = text; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size; t.fontStyle = style;
        t.alignment = TextAnchor.MiddleCenter; t.color = color;
    }

    RectTransform MakeButton(RectTransform parent, string text, Vector2 pos,
                             UnityEngine.Events.UnityAction action,
                             Color normal = default, Color highlight = default)
    {
        if (normal    == default) normal    = new Color(0.20f, 0.10f, 0.10f);
        if (highlight == default) highlight = new Color(0.38f, 0.16f, 0.16f);

        var rt  = Rect(text, parent,
                       new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                       pos, new Vector2(280f, 48f));

        var img = rt.gameObject.AddComponent<Image>();
        img.color = normal;

        var btn = rt.gameObject.AddComponent<Button>();
        var cb  = new Button.ButtonClickedEvent();
        cb.AddListener(action);
        btn.onClick = cb;

        var colors              = btn.colors;
        colors.normalColor      = normal;
        colors.highlightedColor = highlight;
        colors.pressedColor     = new Color(normal.r * 0.6f, normal.g * 0.6f, normal.b * 0.6f);
        btn.colors              = colors;
        btn.targetGraphic       = img;

        var lblRt = Rect("Label", rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Label(lblRt.gameObject, text, 20, FontStyle.Bold, Color.white);

        return rt;
    }
}
