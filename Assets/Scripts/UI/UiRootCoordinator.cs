using UnityEngine;

/// <summary>
/// Owns the top-level menu/game visibility. Both canvases remain active so
/// every authored prefab instance stays inspectable and keeps its references.
/// </summary>
[DisallowMultipleComponent]
public sealed class UiRootCoordinator : MonoBehaviour
{
    [SerializeField] private CanvasGroup menuRoot;
    [SerializeField] private CanvasGroup gameplayRoot;

    public bool IsGameplayVisible => gameplayRoot != null && gameplayRoot.alpha > 0.99f;

    private void Awake()
    {
        EnsureRootsActive();
        ShowMenu();
    }

    public void ShowMenu()
    {
        EnsureRootsActive();
        SetVisible(menuRoot, true);
        SetVisible(gameplayRoot, false);
    }

    public void ShowGameplay()
    {
        EnsureRootsActive();
        SetVisible(menuRoot, false);
        SetVisible(gameplayRoot, true);
    }

    private void EnsureRootsActive()
    {
        if (menuRoot != null) menuRoot.gameObject.SetActive(true);
        if (gameplayRoot != null) gameplayRoot.gameObject.SetActive(true);
    }

    private static void SetVisible(CanvasGroup group, bool visible)
    {
        if (group == null) return;
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    private void OnValidate()
    {
        if (menuRoot == null || gameplayRoot == null)
        {
            Debug.LogWarning("UiRootCoordinator: assign both root CanvasGroups.", this);
        }
    }
}
