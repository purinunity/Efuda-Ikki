using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleGroundHud : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Button surrenderButton;
    [SerializeField] private GameObject confirmationRoot;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private Action surrender;

    private void Awake()
    {
        ValidateSceneReferences();
        WireButtons();
        if (confirmationRoot != null) confirmationRoot.SetActive(false);
    }

    private void OnDestroy() => UnwireButtons();

    public void Configure(bool active, Action onSurrender)
    {
        surrender = onSurrender;
        gameObject.SetActive(active);
        if (confirmationRoot != null) confirmationRoot.SetActive(false);
    }

    private void Update()
    {
        GameModeData mode = GameModeManager.GetGameModeData();
        if (gameObject.activeSelf && (mode == null || mode.Mode != GameModeData.GameMode.BattleGroundMode))
        {
            gameObject.SetActive(false);
        }
    }

    private void WireButtons()
    {
        UnwireButtons();
        surrenderButton?.onClick.AddListener(ShowConfirmation);
        confirmButton?.onClick.AddListener(ConfirmSurrender);
        cancelButton?.onClick.AddListener(HideConfirmation);
    }

    private void UnwireButtons()
    {
        surrenderButton?.onClick.RemoveListener(ShowConfirmation);
        confirmButton?.onClick.RemoveListener(ConfirmSurrender);
        cancelButton?.onClick.RemoveListener(HideConfirmation);
    }

    private void ShowConfirmation()
    {
        if (confirmationRoot != null) confirmationRoot.SetActive(true);
    }

    private void HideConfirmation()
    {
        if (confirmationRoot != null) confirmationRoot.SetActive(false);
    }

    private void ConfirmSurrender()
    {
        HideConfirmation();
        surrender?.Invoke();
    }

    private void ValidateSceneReferences()
    {
        if (surrenderButton == null || confirmationRoot == null ||
            confirmButton == null || cancelButton == null)
        {
            Debug.LogError("BattleGroundHud: scene references are incomplete.", this);
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying && !string.IsNullOrEmpty(gameObject.scene.path))
        {
            ValidateSceneReferences();
        }
    }
}
