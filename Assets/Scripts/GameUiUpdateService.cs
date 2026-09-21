using System.Collections;
using UnityEngine;

public sealed class GameUiUpdateService
{
    private const float DefaultRecoveryTimeoutSeconds = 10f;
    private readonly GameState gameState;
    private readonly UIManager uiManager;
    private readonly GameUiPresenter presenter;
    private int updateVersion;

    public GameUiUpdateService(GameState gameState, UIManager uiManager)
    {
        this.gameState = gameState;
        this.uiManager = uiManager;
        presenter = new GameUiPresenter();
    }

    public IEnumerator WaitForUpdate(float duration = 5f)
    {
        if (uiManager == null)
        {
            Debug.LogWarning("UIManager is not assigned.");
            yield break;
        }

        int currentVersion = ++updateVersion;
        uiManager.Render(presenter.CreateSnapshot(gameState), duration);
        float startedAt = Time.realtimeSinceStartup;

        while (uiManager.UIUpdateInProgress)
        {
            if (currentVersion != updateVersion)
            {
                currentVersion = updateVersion;
                startedAt = Time.realtimeSinceStartup;
            }

            if (Time.realtimeSinceStartup - startedAt >= DefaultRecoveryTimeoutSeconds)
            {
                Debug.LogWarning("UI update service timed out. Recovering the current layout.");
                uiManager.RecoverFromStalledUpdate();
                break;
            }

            yield return null;
        }

        Debug.Log("UI update completed.");
    }
}
