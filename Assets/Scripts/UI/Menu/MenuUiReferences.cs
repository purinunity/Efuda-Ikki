using UnityEngine;

/// <summary>Serialized composition references for the five menu screen prefabs.</summary>
[DisallowMultipleComponent]
public sealed class MenuUiReferences : MonoBehaviour
{
    [SerializeField] private TitleScreenPanel title;
    [SerializeField] private MainMenuPanel modeSelect;
    [SerializeField] private StageSelectPanel stageSelect;
    [SerializeField] private SpecialCardSelectPanel specialCardSelect;
    [SerializeField] private SettingsPanel settings;

    public TitleScreenPanel Title => title;
    public MainMenuPanel ModeSelect => modeSelect;
    public StageSelectPanel StageSelect => stageSelect;
    public SpecialCardSelectPanel SpecialCardSelect => specialCardSelect;
    public SettingsPanel Settings => settings;

    private void OnValidate()
    {
        if (title == null || modeSelect == null || stageSelect == null ||
            specialCardSelect == null || settings == null)
        {
            Debug.LogWarning("MenuUiReferences: assign all menu screen prefab instances.", this);
        }
    }
}
