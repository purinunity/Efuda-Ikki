using UnityEngine;

public sealed class ShowdownCutInPopupProvider
{
    private readonly MonoBehaviour owner;

    public ShowdownCutInPopup CurrentPopup { get; private set; }

    public ShowdownCutInPopupProvider(
        MonoBehaviour owner,
        UIManager uiManager,
        ShowdownCutInPopup initialPopup)
    {
        this.owner = owner;
        CurrentPopup = initialPopup;
    }

    public ShowdownCutInPopup GetScenePopup()
    {
        if (IsUsablePopup(CurrentPopup))
        {
            CurrentPopup.Initialize();
            return CurrentPopup;
        }

        if (CurrentPopup != null)
        {
            Debug.LogError("ShowdownCutInPopup must be attached to its authored OverlayLayer object.");
            CurrentPopup = null;
        }

        Debug.LogError("ShowdownCutInPopupProvider: the scene-authored popup reference is missing.");
        return null;
    }

    private bool IsUsablePopup(ShowdownCutInPopup popup)
    {
        return popup != null && (owner == null || popup.gameObject != owner.gameObject);
    }
}
