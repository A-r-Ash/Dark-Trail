using UnityEngine;

public enum SurfaceType { Default, Wood, Ground, Flesh, Metal, Stone }

public class SurfaceIdentifier : MonoBehaviour
{
    [SerializeField] public SurfaceType surfaceType = SurfaceType.Default;
}
