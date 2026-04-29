using UnityEngine;
using UnityEngine.UI;

public class HealthHUD : MonoBehaviour
{
    [Header("Appearance")]
    [SerializeField] private Color fullHeartColor  = new Color(0.85f, 0.10f, 0.10f);
    [SerializeField] private Color emptyHeartColor = new Color(0.18f, 0.08f, 0.08f);
    [SerializeField] private float heartSize       = 40f;
    [SerializeField] private float heartSpacing    = 10f;
    [SerializeField] private Vector2 screenOffset  = new Vector2(24f, 24f);
    [SerializeField] private GameObject player;

    private Image[]      hearts;
    private PlayerHealth playerHealth;

    void Awake()
    {
        BuildUI();
    }

    void Start()
    {
        // Prefer the serialized reference, then tag search, then scene search
        GameObject playerObj = player;
        if (playerObj == null)
            playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            var found = FindFirstObjectByType<PlayerHealth>();
            if (found != null) playerObj = found.gameObject;
        }

        if (playerObj != null)
        {
            playerHealth = playerObj.GetComponent<PlayerHealth>()
                        ?? playerObj.GetComponentInChildren<PlayerHealth>()
                        ?? playerObj.GetComponentInParent<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.AddListener(UpdateHearts);
            UpdateHearts(playerHealth.CurrentHealth);
        }
        else
        {
            Debug.LogWarning("HealthHUD: PlayerHealth not found. Assign the Player field in the inspector.");
        }
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged.RemoveListener(UpdateHearts);
    }

    // Called by PlayerHealth.OnHealthChanged(int currentHealth) — also callable from inspector
    public void UpdateHearts(int currentHealth)
    {
        for (int i = 0; i < hearts.Length; i++)
            hearts[i].color = i < currentHealth ? fullHeartColor : emptyHeartColor;
    }

    void BuildUI()
    {
        var canvasGo       = new GameObject("HealthHUDCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas         = canvasGo.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 85;
        var scaler                 = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        Texture2D heartTex = BuildHeartTexture(64);
        Sprite    heartSpr = Sprite.Create(heartTex,
                                new Rect(0, 0, heartTex.width, heartTex.height),
                                new Vector2(0.5f, 0.5f));

        int count = 3;
        hearts = new Image[count];

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject($"Heart_{i + 1}");
            go.transform.SetParent(canvasGo.transform, false);

            var rt          = go.AddComponent<RectTransform>();
            rt.anchorMin    = new Vector2(0f, 0f);
            rt.anchorMax    = new Vector2(0f, 0f);
            rt.pivot        = new Vector2(0f, 0f);
            rt.sizeDelta    = new Vector2(heartSize, heartSize);
            rt.anchoredPosition = new Vector2(
                screenOffset.x + i * (heartSize + heartSpacing),
                screenOffset.y);

            var img    = go.AddComponent<Image>();
            img.sprite = heartSpr;
            img.color  = fullHeartColor;
            hearts[i]  = img;
        }
    }

    // Procedural heart shape using the algebraic heart curve:
    // (x² + y² - 1)³ - x²y³ ≤ 0
    static Texture2D BuildHeartTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        for (int py = 0; py < size; py++)
        {
            for (int px = 0; px < size; px++)
            {
                // Map pixel to [-1.2, 1.2]; Unity textures have y=0 at bottom, so no flip needed
                float x = ((px + 0.5f) / size - 0.5f) * 2.4f;
                float y = ((py + 0.5f) / size - 0.5f) * 2.4f;

                float v   = x * x + y * y - 1f;
                float val = v * v * v - x * x * y * y * y;

                // val <= 0 → inside heart; use a soft ramp near the edge
                float alpha = Mathf.Clamp01(-val * 12f + 0.5f);
                tex.SetPixel(px, py, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return tex;
    }
}
