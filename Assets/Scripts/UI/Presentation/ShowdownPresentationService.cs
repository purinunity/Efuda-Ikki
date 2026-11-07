using System.Collections;
using UnityEngine;

public sealed class ShowdownPresentationService
{
    private readonly MonoBehaviour owner;
    private readonly GameState gameState;
    private readonly UIManager uiManager;
    private readonly CharacterManager characterManager;
    private readonly Cards playerSpecialCardsDeck;

    public ShowdownCutInPopup CurrentPopup { get; private set; }

    public ShowdownPresentationService(
        MonoBehaviour owner,
        GameState gameState,
        UIManager uiManager,
        CharacterManager characterManager,
        ShowdownCutInPopup initialPopup,
        Cards playerSpecialCardsDeck = null)
    {
        this.owner = owner;
        this.gameState = gameState;
        this.uiManager = uiManager;
        this.characterManager = characterManager;
        this.playerSpecialCardsDeck = playerSpecialCardsDeck;
        CurrentPopup = initialPopup;
    }

    public IEnumerator Play(SpecialCardResolver.ShowdownResult showdownResult)
    {
        ShowdownCutInPopupProvider popupProvider =
            new ShowdownCutInPopupProvider(owner, uiManager, CurrentPopup);
        ShowdownCutInPopup popup = popupProvider.GetScenePopup();
        CurrentPopup = popupProvider.CurrentPopup;

        if (popup == null)
        {
            yield break;
        }

        ShowdownCutInPopup.Data cutInData =
            new ShowdownCutInDataBuilder(
                gameState,
                characterManager,
                playerSpecialCardsDeck).Build(showdownResult);
        yield return popup.Play(cutInData);
    }
}
