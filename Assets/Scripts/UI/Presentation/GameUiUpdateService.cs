using System.Collections;
using UnityEngine;

public sealed class GameUiUpdateService
{
    private const float DefaultRecoveryTimeoutSeconds = 10f;
    private readonly GameState gameState;
    private readonly UIManager uiManager;
    private readonly GameUiPresenter presenter;
    private readonly GameModeData mode;
    private int updateVersion;

    public GameUiUpdateService(GameState gameState, UIManager uiManager, GameModeData mode = null)
    {
        this.gameState = gameState;
        this.uiManager = uiManager;
        this.mode = mode;
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
        uiManager.Render(presenter.CreateSnapshot(gameState, mode), duration);
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

    public void UpdateImmediately()
    {
        if (uiManager == null)
        {
            Debug.LogWarning("UIManager is not assigned.");
            return;
        }

        updateVersion++;
        uiManager.Render(presenter.CreateSnapshot(gameState, mode), 0f);
        uiManager.RecoverFromStalledUpdate();
    }
}
