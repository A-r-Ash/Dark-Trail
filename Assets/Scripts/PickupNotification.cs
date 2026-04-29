using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PickupNotification : MonoBehaviour
{
    private static PickupNotification _instance;

    private TextMeshProUGUI label;
    private Image           background;
    private Coroutine       fadeRoutine;

    static PickupNotification Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("PickupNotification");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<PickupNotification>();
                _instance.Build();
            }
            return _instance;
        }
    }

    public static void Show(string message) => Instance.ShowMessage(message);

    void ShowMessage(string message)
    {
        label.text = message;
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine());
    }

    IEnumerator FadeRoutine()
    {
        SetAlpha(0f);

        // Fade in
        float t = 0f;
        while (t < 0.12f)
        {
            t += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Clamp01(t / 0.12f));
            yield return null;
        }

        // Hold
        yield return new WaitForSecondsRealtime(1.4f);

        // Fade out
        t = 0f;
        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            SetAlpha(1f - Mathf.Clamp01(t / 0.35f));
            yield return null;
        }

        SetAlpha(0f);
    }

    void SetAlpha(float a)
    {
        var lc = label.color;      label.color      = new Color(lc.r, lc.g, lc.b, a);
        var bc = background.color; background.color = new Color(bc.r, bc.g, bc.b, a * 0.55f);
    }

    void Build()
    {
        var canvasGo               = new GameObject("Canvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas                 = canvasGo.AddComponent<Canvas>();
        canvas.renderMode          = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder        = 95;
        var scaler                 = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        // Background pill
        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(canvasGo.transform, false);
        var bgRt             = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin       = bgRt.anchorMax = new Vector2(0.5f, 0.5f);
        bgRt.pivot           = new Vector2(0.5f, 0.5f);
        bgRt.anchoredPosition = new Vector2(0f, 110f);
        bgRt.sizeDelta       = new Vector2(340f, 48f);
        background           = bgGo.AddComponent<Image>();
        background.color     = new Color(0f, 0f, 0f, 0f);

        // Label
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(bgGo.transform, false);
        var labelRt             = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin       = Vector2.zero;
        labelRt.anchorMax       = Vector2.one;
        labelRt.offsetMin       = labelRt.offsetMax = Vector2.zero;
        label                   = labelGo.AddComponent<TextMeshProUGUI>();
        label.alignment         = TextAlignmentOptions.Center;
        label.fontSize          = 22f;
        label.fontStyle         = FontStyles.Bold;
        label.color             = new Color(1f, 0.82f, 0.2f, 0f);

        SetAlpha(0f);
    }
}
