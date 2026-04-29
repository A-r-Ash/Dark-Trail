using System.Collections;
using UnityEngine;

public class Pistol : GunBase
{
    [Header("Muzzle Flash")]
    [SerializeField] private float muzzleFlashDuration  = 0.05f;
    [SerializeField] private float muzzleFlashIntensity = 10f;
    [SerializeField] private float muzzleFlashRange     = 5f;
    [SerializeField] private Color muzzleFlashColor     = new Color(1f, 0.78f, 0.28f);

    private Light muzzleLight;

    protected override void Awake()
    {
        base.Awake();
        BuildMuzzleFlash();
    }

    void BuildMuzzleFlash()
    {
        Transform parent = muzzlePoint != null ? muzzlePoint : transform;

        GameObject go     = new GameObject("MuzzleFlash");
        go.transform.SetParent(parent);
        go.transform.localPosition = Vector3.zero;

        muzzleLight           = go.AddComponent<Light>();
        muzzleLight.type      = LightType.Point;
        muzzleLight.color     = muzzleFlashColor;
        muzzleLight.intensity = 0f;
        muzzleLight.range     = muzzleFlashRange;
        muzzleLight.shadows   = LightShadows.None;
    }

    protected override void FireEffect()
    {
        if (muzzleLight != null)
            StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        muzzleLight.intensity = muzzleFlashIntensity;
        yield return new WaitForSeconds(muzzleFlashDuration);
        muzzleLight.intensity = 0f;
    }
}
