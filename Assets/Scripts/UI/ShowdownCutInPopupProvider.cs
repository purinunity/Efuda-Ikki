using UnityEngine;

public sealed class ShowdownCutInPopupProvider
{
    private readonly MonoBehaviour owner;
    private readonly UIManager uiManager;

    public ShowdownCutInPopup CurrentPopup { get; private set; }

    public ShowdownCutInPopupProvider(
        MonoBehaviour owner,
        UIManager uiManager,
        ShowdownCutInPopup initialPopup)
    {
        this.owner = owner;
        this.uiManager = uiManager;
        CurrentPopup = initialPopup;
    }

    public ShowdownCutInPopup GetOrCreate()
    {
        Transform popupParent = GetPopupParent();

        if (IsUsablePopup(CurrentPopup))
        {
            ConfigurePopup(CurrentPopup, popupParent);
            return CurrentPopup;
        }

        if (CurrentPopup != null)
        {
            Debug.LogWarning("ShowdownCutInPopup must be attached to its own UI GameObject, not the GameManager GameObject. A popup object will be created under the game Canvas.");
            CurrentPopup = null;
        }

        foreach (ShowdownCutInPopup candidate in Object.FindObjectsOfType<ShowdownCutInPopup>(true))
        {
            if (!IsUsablePopup(candidate))
            {
                continue;
            }

            CurrentPopup = candidate;
            ConfigurePopup(CurrentPopup, popupParent);
            return CurrentPopup;
        }

        CurrentPopup = ShowdownCutInPopup.Create(popupParent);
        return CurrentPopup;
    }

    private void ConfigurePopup(ShowdownCutInPopup popup, Transform popupParent)
    {
        popup.SetPopupParent(popupParent);
        popup.Initialize();
    }

    private bool IsUsablePopup(ShowdownCutInPopup popup)
    {
        return popup != null && (owner == null || popup.gameObject != owner.gameObject);
    }

    private Transform GetPopupParent()
    {
        Canvas gameCanvas = FindGameCanvas();
        if (gameCanvas != null)
        {
            return gameCanvas.transform;
        }

        if (uiManager != null)
        {
            return uiManager.transform;
        }

        return owner != null ? owner.transform : null;
    }

    private Canvas FindGameCanvas()
    {
        if (uiManager == null)
        {
            return owner != null ? owner.GetComponentInParent<Canvas>() : null;
        }

        Canvas canvas =
            FindCanvas(uiManager.deck) ??
            FindCanvas(uiManager.common) ??
            FindCanvas(uiManager.player1) ??
            FindCanvas(uiManager.player2) ??
            FindCanvas(uiManager.player1Special) ??
            FindCanvas(uiManager.player2Special) ??
            FindCanvas(uiManager.trash) ??
            uiManager.GetComponentInParent<Canvas>();

        return canvas;
    }

    private static Canvas FindCanvas(Component component)
    {
        return component != null ? component.GetComponentInParent<Canvas>() : null;
    }
}
