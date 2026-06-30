using UnityEngine;

public sealed class GameEndNavigationService
{
    private readonly TitleUIManager titleUIManager;

    public GameEndNavigationService(TitleUIManager titleUIManager)
    {
        this.titleUIManager = titleUIManager;
    }

    public void ReturnToTitleOrStopEditor()
    {
        if (titleUIManager != null)
        {
            titleUIManager.ShowTitleScreen();
            GameModeManager.ResetGameModeData();
            return;
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
