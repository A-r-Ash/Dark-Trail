using UnityEngine;
using UnityEngine.UI;

// Game-over display is now handled entirely by GameOverScreen (buildUIAutomatically = true).
// This script only wires a legacy restart button if one is still assigned.
public class HUDController : MonoBehaviour
{
    [SerializeField] private Button restartButton;

    void Start()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(() => GameManager.Instance?.RestartGame());
    }
}
