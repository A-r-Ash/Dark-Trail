using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AmmoDisplay : MonoBehaviour
{
    [SerializeField] private GunBase gun;

    // Ammo row (bottom-right)
    private TextMeshProUGUI currentAmmoText;
    private TextMeshProUGUI reserveAmmoText;

    // Reload bullets (center screen)
    private GameObject reloadContainer;
    private Image[]    bulletImages;
    private Color[]    bulletColors;
    private bool       wasReloading;
    private float      reloadStartTime;

    private static readonly Color ColorEmpty  = new Color(0.22f, 0.16f, 0.05f, 0.50f);
    private static readonly Color ColorFilled = new Color(1.00f, 0.78f, 0.24f, 1.00f);

    void Start()
    {
        if (gun == null)
        {
            var shooter = FindFirstObjectByType<PlayerShooter>();
            if (shooter != null) gun = shooter.EquippedGun;
        }
        if (gun == null)
            gun = FindFirstObjectByType<GunBase>();

        Sprite bulletSprite = BuildBulletSprite();
        BuildCanvas(bulletSprite);

        if (gun != null)
        {
            gun.OnAmmoChanged += Refresh;
            Refresh(gun.CurrentAmmo, gun.ReserveAmmo);
        }
        else
        {
            Debug.LogWarning("AmmoDisplay: no GunBase found. Assign it in the inspector.");
        }
    }

    void Update()
    {
        if (gun == null) return;

        bool reloading = gun.IsReloading;

        if (reloading && !wasReloading)
            reloadStartTime = Time.time;

        reloadContainer.SetActive(reloading || AnyBulletVisible());
        TickBulletColors(reloading);

        wasReloading = reloading;
    }

    void OnDestroy()
    {
        if (gun != null) gun.OnAmmoChanged -= Refresh;
    }

    void Refresh(int current, int reserve)
    {
        currentAmmoText.text = current.ToString();
        reserveAmmoText.text = reserve.ToString();
    }

    // ── Bullet color logic ────────────────────────────────────────────────────

    void TickBulletColors(bool reloading)
    {
        float reloadTime = gun.Data != null ? gun.Data.reloadTime : 1f;
        float progress   = reloading
            ? Mathf.Clamp01((Time.time - reloadStartTime) / reloadTime)
            : 0f;

        for (int i = 0; i < bulletImages.Length; i++)
        {
            bool  filled = progress >= (float)(i + 1) / bulletImages.Length;
            Color target = filled ? ColorFilled : ColorEmpty;
            bulletColors[i]       = Color.Lerp(bulletColors[i], target, Time.deltaTime * 12f);
            bulletImages[i].color = bulletColors[i];
        }
    }

    bool AnyBulletVisible()
    {
        // Keep container alive until all bullets have faded back to empty
        for (int i = 0; i < bulletColors.Length; i++)
            if (bulletColors[i].a > ColorEmpty.a + 0.02f) return true;
        return false;
    }

    // ── Canvas builder ────────────────────────────────────────────────────────

    void BuildCanvas(Sprite bulletSprite)
    {
        var canvasGO               = new GameObject("AmmoCanvas");
        var canvas                 = canvasGO.AddComponent<Canvas>();
        canvas.renderMode          = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder        = 10;
        var scaler                 = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        BuildAmmoRow(canvasGO.transform);
        BuildBulletRow(canvasGO.transform, bulletSprite);
    }

    // Bottom-right: "12 / 48"
    void BuildAmmoRow(Transform root)
    {
        var panel           = new GameObject("AmmoPanel");
        panel.transform.SetParent(root, false);
        var rt              = panel.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(1f, 0f);
        rt.anchorMax        = new Vector2(1f, 0f);
        rt.pivot            = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-40f, 40f);
        rt.sizeDelta        = new Vector2(260f, 70f);

        currentAmmoText = MakeLabel(panel.transform, "Cur",
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0f), new Vector2(110f, 70f),
            48, Color.white, TextAlignmentOptions.Right);

        var sep  = MakeLabel(panel.transform, "Sep",
            new Vector2(0.44f, 0.5f), new Vector2(0.44f, 0.5f),
            new Vector2(0f, -4f), new Vector2(40f, 50f),
            28, new Color(0.55f, 0.55f, 0.55f), TextAlignmentOptions.Center);
        sep.text = "/";

        reserveAmmoText = MakeLabel(panel.transform, "Res",
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(0f, -6f), new Vector2(100f, 50f),
            30, new Color(0.60f, 0.60f, 0.60f), TextAlignmentOptions.Left);
    }

    // Center screen: 6 bullet silhouettes
    void BuildBulletRow(Transform root, Sprite bulletSprite)
    {
        const int   count  = 6;
        const float bW     = 14f;   // bullet width
        const float bH     = 36f;   // bullet height
        const float gap    = 7f;
        const float totalW = count * bW + (count - 1) * gap;

        reloadContainer = new GameObject("ReloadBullets");
        reloadContainer.transform.SetParent(root, false);
        var cRT             = reloadContainer.AddComponent<RectTransform>();
        cRT.anchorMin       = cRT.anchorMax = new Vector2(0.5f, 0.5f);
        cRT.pivot           = new Vector2(0.5f, 0.5f);
        cRT.anchoredPosition = new Vector2(0f, -75f);
        cRT.sizeDelta       = new Vector2(totalW, bH);

        bulletImages = new Image[count];
        bulletColors = new Color[count];

        float startX = -totalW * 0.5f + bW * 0.5f;

        for (int i = 0; i < count; i++)
        {
            var go  = new GameObject($"Bullet_{i}");
            go.transform.SetParent(reloadContainer.transform, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(bW, bH);
            rt.anchoredPosition = new Vector2(startX + i * (bW + gap), 0f);

            var img          = go.AddComponent<Image>();
            img.sprite       = bulletSprite;
            img.preserveAspect = false;
            img.color        = ColorEmpty;

            bulletImages[i] = img;
            bulletColors[i] = ColorEmpty;
        }

        reloadContainer.SetActive(false);
    }

    // ── Bullet sprite ─────────────────────────────────────────────────────────

    static Sprite BuildBulletSprite()
    {
        const int w = 24, h = 64;
        var tex       = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx      = w * 0.5f;

        for (int y = 0; y < h; y++)
        {
            float yf = (float)y / h;

            // Half-width at this row: body → shoulder → ogive
            float hw;
            if (yf < 0.52f)
                hw = w * 0.44f;
            else if (yf < 0.72f)
            {
                float t = (yf - 0.52f) / 0.20f;
                hw = Mathf.Lerp(w * 0.44f, w * 0.26f, t);
            }
            else
            {
                float t = (yf - 0.72f) / 0.28f;
                hw = Mathf.Lerp(w * 0.26f, 0.4f, Mathf.Pow(t, 0.6f));
            }

            for (int x = 0; x < w; x++)
            {
                float dx    = x - cx;
                float alpha = Mathf.Clamp01(hw - Mathf.Abs(dx) + 0.5f);

                if (alpha <= 0f) { tex.SetPixel(x, y, Color.clear); continue; }

                // Metallic shading: highlight left, shadow right
                float relX = hw > 0f ? dx / hw : 0f;          // –1..1
                float br;
                if (relX < -0.45f)
                    br = Mathf.Lerp(1.00f, 0.88f, (relX + 1f) / 0.55f); // left highlight
                else if (relX < 0.35f)
                    br = 0.88f;
                else
                    br = Mathf.Lerp(0.88f, 0.60f, (relX - 0.35f) / 0.65f); // right shadow

                // Primer base — darker band at the very bottom
                if (yf < 0.10f) br *= 0.75f;

                // Rim groove just above primer
                if (yf >= 0.10f && yf < 0.14f) br *= 0.70f;

                tex.SetPixel(x, y, new Color(br, br, br, alpha));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    // ── Text helper ───────────────────────────────────────────────────────────

    TextMeshProUGUI MakeLabel(Transform parent, string goName,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta,
        float fontSize, Color color, TextAlignmentOptions alignment)
    {
        var go          = new GameObject(goName);
        go.transform.SetParent(parent, false);
        var rt          = go.AddComponent<RectTransform>();
        rt.anchorMin    = anchorMin;
        rt.anchorMax    = anchorMax;
        rt.pivot        = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta    = sizeDelta;
        var tmp         = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize    = fontSize;
        tmp.color       = color;
        tmp.alignment   = alignment;
        tmp.text        = "0";
        return tmp;
    }
}
