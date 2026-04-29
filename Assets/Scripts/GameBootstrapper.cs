using UnityEngine;

[DefaultExecutionOrder(-100)]
public class GameBootstrapper : MonoBehaviour
{
    void Awake()
    {
        if (FindFirstObjectByType<GameManager>() == null)
            new GameObject("GameManager").AddComponent<GameManager>();

        if (FindFirstObjectByType<HUDController>() == null)
            new GameObject("HUD").AddComponent<HUDController>();

        if (FindFirstObjectByType<PlayerHealth>() == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                player.AddComponent<PlayerHealth>();
        }
    }
}
