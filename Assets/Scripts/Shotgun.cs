using System.Collections;
using UnityEngine;

public class Shotgun : GunBase
{
    [Header("Shotgun")]
    [SerializeField] private int   pelletCount    = 8;
    [SerializeField] private float spreadAngle    = 12f;   // half-angle cone per side

    [Header("Muzzle Flash")]
    [SerializeField] private float muzzleFlashDuration  = 0.07f;
    [SerializeField] private float muzzleFlashIntensity = 18f;
    [SerializeField] private float muzzleFlashRange     = 7f;
    [SerializeField] private Color muzzleFlashColor     = new Color(1f, 0.65f, 0.15f);

    private Light muzzleLight;

    protected override void Awake()
    {
        base.Awake();
        BuildMuzzleFlash();
    }

    void BuildMuzzleFlash()
    {
        Transform parent = muzzlePoint != null ? muzzlePoint : transform;
        var go = new GameObject("MuzzleFlash");
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

    protected override void PerformRaycast(Vector3 direction)
    {
        Vector3 origin = muzzlePoint != null
            ? muzzlePoint.position
            : transform.position + Vector3.up * 0.5f;

        for (int i = 0; i < pelletCount; i++)
        {
            float yaw   = Random.Range(-spreadAngle, spreadAngle);
            Vector3 pelletDir = Quaternion.Euler(0f, yaw, 0f) * direction;

            if (Physics.Raycast(origin, pelletDir, out RaycastHit hit, data.range, hitMask, QueryTriggerInteraction.Ignore))
                OnHit(hit);
        }
    }

    IEnumerator FlashRoutine()
    {
        muzzleLight.intensity = muzzleFlashIntensity;
        yield return new WaitForSeconds(muzzleFlashDuration);
        muzzleLight.intensity = 0f;
    }
}
