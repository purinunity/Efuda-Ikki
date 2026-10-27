using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Discard source area. The separately-authored panel owns every preview Image.</summary>
public class PopupPreviewCardArea : CardArea
{
    private readonly Dictionary<Button, UnityAction> sourceCardClickHandlers = new Dictionary<Button, UnityAction>();
    private DiscardPreviewPanel previewPanel;

    public void ConfigurePreview(DiscardPreviewPanel panel) => previewPanel = panel;

    public override void SetCards(List<Card> cards, float totalDuration = 1f)
    {
        base.SetCards(cards, totalDuration);
        RefreshSourceCardClickHandlers();
    }

    public override void SetCardsBySpeed(List<Card> cards, float moveSpeed, float turnSpeed)
    {
        base.SetCardsBySpeed(cards, moveSpeed, turnSpeed);
        RefreshSourceCardClickHandlers();
    }

    public void ShowPopup(Card focusedCard = null)
    {
        if (focusedCard != null && !focusedCard.MoveComplete) return;
        if (previewPanel == null)
        {
            Debug.LogError("PopupPreviewCardArea: DiscardPreviewPanel is not configured.", this);
            return;
        }
        previewPanel.Show(cardsInArea, focusedCard);
    }

    public void ClosePopup() => previewPanel?.Close();
    private void OnEnable() => BindSourceCardClickHandlers();

    private void BindSourceCardClickHandlers()
    {
        if (!isActiveAndEnabled) return;
        var activeButtons = new HashSet<Button>();
        foreach (Card source in cardsInArea)
        {
            if (source == null) continue;
            Button button = source.GetComponent<Button>();
            if (button == null) continue;
            activeButtons.Add(button);
            if (sourceCardClickHandlers.ContainsKey(button)) continue;
            Card captured = source;
            UnityAction action = () => ShowPopup(captured);
            sourceCardClickHandlers.Add(button, action);
            button.onClick.AddListener(action);
        }
        foreach (Button stale in sourceCardClickHandlers.Keys.Where(button => button == null || !activeButtons.Contains(button)).ToList())
        {
            if (stale != null) stale.onClick.RemoveListener(sourceCardClickHandlers[stale]);
            sourceCardClickHandlers.Remove(stale);
        }
    }

    private void RefreshSourceCardClickHandlers()
    {
        if (isActiveAndEnabled) BindSourceCardClickHandlers();
        else UnbindSourceCardClickHandlers();
    }

    private void UnbindSourceCardClickHandlers()
    {
        foreach (KeyValuePair<Button, UnityAction> pair in sourceCardClickHandlers)
            pair.Key?.onClick.RemoveListener(pair.Value);
        sourceCardClickHandlers.Clear();
    }

    private void OnDisable() => UnbindSourceCardClickHandlers();
    private void OnDestroy() => UnbindSourceCardClickHandlers();
}
