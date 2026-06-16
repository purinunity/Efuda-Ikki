using System.Collections;
using UnityEngine;

public sealed class GameUiUpdateService
{
    private readonly GameState gameState;
    private readonly UIManager uiManager;

    public GameUiUpdateService(GameState gameState, UIManager uiManager)
    {
        this.gameState = gameState;
        this.uiManager = uiManager;
    }

    public IEnumerator WaitForUpdate(float duration = 5f)
    {
        if (uiManager == null)
        {
            Debug.LogWarning("UIManager is not assigned.");
            yield break;
        }

        uiManager.UIUpdate(gameState, duration);

        while (uiManager.UIUpdateInProgress)
        {
            yield return null;
        }

        Debug.Log("UI update completed.");
    }
}
