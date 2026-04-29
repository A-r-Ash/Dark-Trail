using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerController))]
public class PlayerShooter : MonoBehaviour
{
    [Header("Gun")]
    [SerializeField] private GunBase equippedGun;

    [Header("Aim Settings")]
    [SerializeField] private float aimSpeedMultiplier = 0.45f;
    [SerializeField] private float aimTransitionSpeed = 12f;

    [Header("Aim Sounds")]
    [SerializeField] private AudioClip aimSound;
    [SerializeField] private AudioClip holsterSound;
    [SerializeField] [Range(0f, 1f)] private float aimSoundVolume = 0.8f;

    [Header("Crosshair")]
    [SerializeField] private Color crosshairColor = new Color(1f, 1f, 1f, 0.9f);

    private PlayerController playerController;
    private Camera           mainCamera;
    private AudioSource      audioSource;
    private bool             isAiming;
    private bool             wasAiming;
    private float            currentSpeedMult = 1f;

    public bool    IsAiming     => isAiming;
    public bool    IsReloading  => equippedGun != null && equippedGun.IsReloading;
    public GunBase EquippedGun  => equippedGun;
    private GameObject       crosshairCanvas;
    private RectTransform    crosshairRT;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        mainCamera       = Camera.main;
        Cursor.lockState = CursorLockMode.Confined;

        audioSource            = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource        = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        BuildCrosshair();
        crosshairCanvas.SetActive(false);
    }

    void OnDestroy()
    {
        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void Update()
    {
        HandleAim();
        HandleFire();
        HandleReload();

        if (isAiming)
            MoveCrosshairToMouse();
    }

    void MoveCrosshairToMouse()
    {
        Vector2 mouse = Mouse.current.position.ReadValue();
        crosshairRT.anchoredPosition = mouse - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    void HandleAim()
    {
        bool reloading = equippedGun != null && equippedGun.IsReloading;
        isAiming = !reloading && Mouse.current.rightButton.isPressed;

        if (isAiming && !wasAiming)
            PlaySound(aimSound);
        else if (!isAiming && wasAiming)
            PlaySound(holsterSound);
        wasAiming = isAiming;

        Cursor.visible = !isAiming;
        crosshairCanvas.SetActive(isAiming);

        float target     = isAiming ? aimSpeedMultiplier : 1f;
        currentSpeedMult = Mathf.Lerp(currentSpeedMult, target, Time.deltaTime * aimTransitionSpeed);
        playerController.SetSpeedMultiplier(currentSpeedMult);
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null)
            audioSource.PlayOneShot(clip, aimSoundVolume);
    }

    void HandleFire()
    {
        if (!isAiming || equippedGun == null) return;

        bool triggerPulled = equippedGun.Data != null && equippedGun.Data.isAutomatic
            ? Mouse.current.leftButton.isPressed
            : Mouse.current.leftButton.wasPressedThisFrame;

        if (triggerPulled)
            equippedGun.Fire(GetFireDirection());
    }

    void HandleReload()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
            equippedGun?.Reload();
    }

    Vector3 GetFireDirection()
    {
        float spread = 0f;
        if (equippedGun != null && equippedGun.Data != null)
            spread = isAiming ? equippedGun.Data.adsSpread : equippedGun.Data.hipSpread;

        Ray   ray   = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane plane = new Plane(Vector3.up, transform.position);

        Vector3 dir = transform.forward;
        if (plane.Raycast(ray, out float dist))
        {
            Vector3 worldTarget = ray.GetPoint(dist);
            dir = (worldTarget - transform.position);
            dir.y = 0f;
            dir.Normalize();
        }

        if (spread > 0f)
            dir = Quaternion.Euler(0f, Random.Range(-spread, spread), 0f) * dir;

        return dir;
    }

    // ── Crosshair ─────────────────────────────────────────────────────────────

    void BuildCrosshair()
    {
        crosshairCanvas             = new GameObject("CrosshairCanvas");
        var canvas                  = crosshairCanvas.AddComponent<Canvas>();
        canvas.renderMode           = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder         = 50;
        crosshairCanvas.AddComponent<CanvasScaler>();

        var inner               = new GameObject("Crosshair");
        inner.transform.SetParent(crosshairCanvas.transform, false);
        crosshairRT             = inner.AddComponent<RectTransform>();
        crosshairRT.anchorMin   = crosshairRT.anchorMax = crosshairRT.pivot = new Vector2(0.5f, 0.5f);
        crosshairRT.anchoredPosition = Vector2.zero;
        crosshairRT.sizeDelta   = Vector2.zero;

        MakePart("H", new Vector2(12, 1), Vector2.zero);
        MakePart("V", new Vector2(1, 12), Vector2.zero);
    }

    void MakePart(string partName, Vector2 size, Vector2 pos)
    {
        var go              = new GameObject(partName);
        go.transform.SetParent(crosshairRT, false);
        var rt              = go.AddComponent<RectTransform>();
        rt.anchorMin        = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = pos;
        go.AddComponent<Image>().color = crosshairColor;
    }
}
