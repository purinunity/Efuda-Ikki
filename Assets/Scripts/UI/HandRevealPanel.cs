using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Owns the final confirmation between the exchange phase and showdown.</summary>
public sealed class HandRevealPanel : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image image;
    [SerializeField] private ShowdownCutInAssetSet assets;
    private bool confirmed;
    private bool cancelled;
    private bool initialized;

    private void Awake() => Initialize();

    private void Initialize()
    {
        if (initialized) return;
        if (assets == null) assets = Resources.Load<ShowdownCutInAssetSet>("ShowdownCutInAssets");
        if (button == null || image == null)
        {
            Debug.LogError("HandRevealPanel: scene references are incomplete.", this);
            return;
        }
        initialized = true;
        button.targetGraphic = image;
        if (assets != null && assets.handRevealButtonPressed != null)
        {
            SpriteState state = button.spriteState;
            state.highlightedSprite = assets.handRevealButtonPressed;
            state.pressedSprite = assets.handRevealButtonPressed;
            state.selectedSprite = assets.handRevealButtonPressed;
            button.spriteState = state;
            button.transition = Selectable.Transition.SpriteSwap;
        }
        button.onClick.AddListener(Confirm);
        gameObject.SetActive(false);
    }

    public IEnumerator WaitForConfirmation()
    {
        Initialize();
        confirmed = false;
        cancelled = false;
        ResetVisualState();
        button.interactable = true;
        gameObject.SetActive(true);
        while (!confirmed && !cancelled) yield return null;
        gameObject.SetActive(false);
    }

    public bool WasConfirmed => confirmed && !cancelled;

    public void CancelDisplay()
    {
        cancelled = true;
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    private void Confirm()
    {
        if (confirmed || cancelled) return;
        confirmed = true;
        button.interactable = false;
        if (assets != null && assets.handRevealButtonPressed != null)
            image.sprite = assets.handRevealButtonPressed;
    }

    private void ResetVisualState()
    {
        if (image != null)
        {
            image.sprite = assets != null ? assets.handRevealButton : null;
            image.color = Color.white;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null && eventSystem.currentSelectedGameObject == gameObject)
        {
            eventSystem.SetSelectedGameObject(null);
        }
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(Confirm);
    }

    private void OnValidate()
    {
        if (!Application.isPlaying && !string.IsNullOrEmpty(gameObject.scene.path) &&
            (button == null || image == null))
        {
            Debug.LogWarning("HandRevealPanel: assign the scene-authored Button and Image.", this);
        }
    }
}
