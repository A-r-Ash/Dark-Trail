using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TutorialTrigger : MonoBehaviour
{
    [SerializeField] private TutorialType tutorialType;
    [SerializeField] private bool         showOnce = true;

    private bool triggered;

    void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (showOnce && triggered) return;
        triggered = true;
        TutorialManager.Instance?.Show(tutorialType);
    }

    // Green box so you can see trigger zones in the editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color  = new Color(0.2f, 0.9f, 0.3f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
        Gizmos.color  = new Color(0.2f, 0.9f, 0.3f, 0.85f);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }
}
