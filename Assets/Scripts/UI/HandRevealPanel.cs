using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Owns the final confirmation between the exchange phase and showdown.</summary>
public sealed class HandRevealPanel : MonoBehaviour
{
    private Button button;
    private Image image;
    private ShowdownCutInAssetSet assets;
    private bool confirmed;
    private bool cancelled;

    public static HandRevealPanel GetOrCreate(UIManager uiManager, MonoBehaviour owner)
    {
        HandRevealPanel existing = FindObjectOfType<HandRevealPanel>(true);
        if (existing != null) return existing;

        Canvas canvas = uiManager != null && uiManager.deck != null
            ? uiManager.deck.GetComponentInParent<Canvas>()
            : owner != null ? owner.GetComponentInParent<Canvas>() : null;
        if (canvas == null) return null;

        Transform parent = canvas.transform.Find("CharacterManager/control_frame");
        if (parent == null) parent = canvas.transform;
        GameObject root = new GameObject(
            "HandRevealPanel", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(Button));
        root.transform.SetParent(parent, false);
        return root.AddComponent<HandRevealPanel>();
    }

    private void Awake() => Initialize();

    private void Initialize()
    {
        if (button != null) return;
        assets = Resources.Load<ShowdownCutInAssetSet>("ShowdownCutInAssets");
        RectTransform rect = GetComponent<RectTransform>();
        bool attachedToControlPanel = transform.parent != null && transform.parent.name == "control_frame";
        rect.anchorMin = rect.anchorMax = attachedToControlPanel
            ? new Vector2(0.5f, 1f)
            : new Vector2(1f, 1f);
        rect.pivot = attachedToControlPanel
            ? new Vector2(0.5f, 0f)
            : new Vector2(1f, 1f);
        rect.anchoredPosition = attachedToControlPanel
            ? new Vector2(0f, 0.35f)
            : new Vector2(-30f, -188f);
        rect.sizeDelta = attachedToControlPanel
            ? new Vector2(7.7f, 2.2f)
            : new Vector2(250f, 72f);
        AlignWithDecisionButton(rect);

        image = GetComponent<Image>();
        image.sprite = assets != null ? assets.handRevealButton : null;
        image.color = Color.white;
        image.preserveAspect = true;

        button = GetComponent<Button>();
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
        AlignWithDecisionButton(GetComponent<RectTransform>());
        ResetVisualState();
        button.interactable = true;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
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

    private void AlignWithDecisionButton(RectTransform target)
    {
        if (target == null || transform.parent == null)
        {
            return;
        }

        RectTransform decisionRect = transform.parent.Find("D_button") as RectTransform;
        if (decisionRect == null)
        {
            return;
        }

        target.anchorMin = decisionRect.anchorMin;
        target.anchorMax = decisionRect.anchorMax;
        target.pivot = decisionRect.pivot;
        target.anchoredPosition3D = decisionRect.anchoredPosition3D;
        target.sizeDelta = decisionRect.sizeDelta;
        target.localRotation = decisionRect.localRotation;
        target.localScale = decisionRect.localScale;
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(Confirm);
    }
}
