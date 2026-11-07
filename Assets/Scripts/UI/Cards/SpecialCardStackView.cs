using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Owns stack button subscriptions, visual state and the switch animation.</summary>
internal sealed class SpecialCardStackView
{
    private readonly MonoBehaviour owner;
    private readonly Action<Card> onClick;
    private readonly Action<Card> refreshUsage;
    private readonly Dictionary<Card, UnityAction> handlers = new Dictionary<Card, UnityAction>();
    private readonly Dictionary<Card, Button> buttons = new Dictionary<Card, Button>();
    private Coroutine switchRoutine;
    public bool IsSwitching => switchRoutine != null;

    public SpecialCardStackView(MonoBehaviour owner, Action<Card> onClick, Action<Card> refreshUsage)
    {
        this.owner = owner;
        this.onClick = onClick;
        this.refreshUsage = refreshUsage;
    }

    public void Bind(Card card)
    {
        if (!owner.isActiveAndEnabled) return;
        Button button = card.GetComponent<Button>();
        if (button == null) return;
        if (handlers.TryGetValue(card, out UnityAction previous))
        {
            buttons.TryGetValue(card, out Button oldButton);
            if (oldButton == button)
            {
                refreshUsage(card);
                return;
            }
            oldButton?.onClick.RemoveListener(previous);
        }
        UnityAction handler = () => onClick(card);
        handlers[card] = handler;
        buttons[card] = button;
        button.onClick.AddListener(handler);
        refreshUsage(card);
    }

    public void Unbind(Card card)
    {
        if (card == null || !handlers.TryGetValue(card, out UnityAction handler)) return;
        if (buttons.TryGetValue(card, out Button button)) button?.onClick.RemoveListener(handler);
        handlers.Remove(card);
        buttons.Remove(card);
    }

    public void UnbindAll()
    {
        foreach (Card card in new List<Card>(handlers.Keys)) Unbind(card);
    }

    public void Stop()
    {
        if (switchRoutine == null) return;
        owner.StopCoroutine(switchRoutine);
        switchRoutine = null;
    }

    public void PlaySwitch(IReadOnlyList<Card> cards, Card featured, float moveDuration, float settleDuration,
        float lift, Func<bool> allComplete, Action recoverLayout)
    {
        Stop();
        switchRoutine = owner.StartCoroutine(Animate(cards, featured, moveDuration, settleDuration, lift,
            allComplete, recoverLayout));
    }

    private IEnumerator Animate(IReadOnlyList<Card> cards, Card featured, float moveDuration,
        float settleDuration, float lift, Func<bool> allComplete, Action recoverLayout)
    {
        foreach (Card card in cards)
        {
            if (card == null || card == featured) continue;
            if (!card.MoveComplete || card.NeedsAnimationForCurrentTarget()) card.MoveAndTurnCard(settleDuration);
        }
        if (featured != null)
        {
            featured.MoveAlongArcToTarget(moveDuration, lift);
            yield return new WaitUntil(() => featured.MoveComplete);
            featured.SnapToTargetPosition();
        }
        yield return new WaitUntil(allComplete);
        recoverLayout();
        switchRoutine = null;
    }

    public void RefreshTooltips(IReadOnlyList<Card> cards, bool enabled)
    {
        if (cards == null) return;
        Card top = cards.Count > 0 ? cards[cards.Count - 1] : null;
        foreach (Card card in cards)
        {
            if (card == null) continue;
            var target = card.GetComponent<SpecialCardTooltipTarget>();
            if (target != null) target.SetTooltipEnabled(enabled && card == top);
        }
    }

    public void RenderUsage(Card card, bool used, bool inputEnabled, bool dimUsed, Color usedTint, Color normalTint)
    {
        if (card == null) return;
        Image image = card.GetComponent<Image>();
        if (image != null)
        {
            Color tint = used && dimUsed ? usedTint : normalTint;
            tint.a = 1f;
            image.color = tint;
            image.raycastTarget = true;
        }
        Button button = card.GetComponent<Button>();
        if (button == null) return;
        ColorBlock colors = button.colors;
        colors.disabledColor = Color.white;
        if (!dimUsed)
        {
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.colorMultiplier = 1f;
        }
        button.colors = colors;
        button.interactable = inputEnabled;
        button.navigation = new Navigation { mode = inputEnabled ? Navigation.Mode.Automatic : Navigation.Mode.None };
    }
}
