using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// UI全体の管理・演出調整を行うクラス
// デッキ・共通札・手札・捨て札などの表示を制御
public class UIManager : MonoBehaviour
{
    [SerializeField] public CardArea deck;
    [SerializeField] public CardArea common;
    [SerializeField] public LimitedSelectableCardArea player1;
    [SerializeField] public CardArea player2;
    [SerializeField] public CardArea player1Special;
    [SerializeField] public CardArea player2Special;
    [SerializeField] public CardArea trash;
    [SerializeField] public Cards allCards;
    [SerializeField] public Cards specialCards1;
    [SerializeField] public Cards specialCards2;
    [SerializeField] private TextMeshProUGUI r;
    [SerializeField] private PlayerRole H;
    [SerializeField] private TextMeshProUGUI L1;
    [SerializeField] private TextMeshProUGUI L2;
    [SerializeField] private PlayerRemainTrashCount playerRemainTrashCount;
    public float cardMoveSpeed = 800f;
    public float cardTurnSpeed = 720f;
    [SerializeField, Min(0.1f)] private float uiUpdateRecoveryTimeout = 10f;

    public bool UIUpdateInProgress { get; private set; } = false;
    private Coroutine uiUpdateCoroutine;
    private readonly HashSet<Card> recoveryCards = new HashSet<Card>();
    private readonly GameUiPresenter presenter = new GameUiPresenter();

    private void Awake()
    {
    }

    private void OnDestroy()
    {
        CancelPendingUiUpdate(true);
    }

    private void OnDisable()
    {
        CancelPendingUiUpdate(true);
    }

    public void UIUpdate(GameState state, float duration)
    {
        Render(presenter.CreateSnapshot(state), duration);
    }

    public void Render(GameUiSnapshot snapshot, float duration)
    {
        // A newer snapshot supersedes the previous animation. Finish its current
        // targets first so no coroutine can later overwrite the new layout.
        CancelPendingUiUpdate(true);
        if (snapshot == null)
        {
            Debug.LogWarning("Cannot render a null game UI snapshot.", this);
            return;
        }

        UIUpdateInProgress = true;

        r.text = "第" + snapshot.RoundNumber.ToString() + "局";
        L1.text = snapshot.PlayerLifePoints.ToString();
        L2.text = snapshot.CpuLifePoints.ToString();

        H.SetRole(snapshot.PlayerRoleName, duration);
        playerRemainTrashCount.UpdateRemainTrashCount(snapshot.RemainingTrashTurns);

        // 特殊札表示（プレイヤー状態が空なら GameModeData + sps から復元）
        UpdateSpecialCardArea(snapshot, 0, player1Special, specialCards1);
        UpdateSpecialCardArea(snapshot, 1, player2Special, specialCards2);

        deck.SetCardsBySpeed(snapshot.DeckCards, cardMoveSpeed, cardTurnSpeed);
        common.SetCardsBySpeed(snapshot.CommonCards, cardMoveSpeed, cardTurnSpeed);
        trash.SetCardsBySpeed(snapshot.TrashCards, cardMoveSpeed, cardTurnSpeed);

        if (snapshot.PlayerHandCards.Count == 5)
        {
            player1.SetCardsBySpeed(snapshot.PlayerHandCards, cardMoveSpeed, cardTurnSpeed);
        }
        if (snapshot.CpuHandCards.Count == 5)
        {
            player2.SetCardsBySpeed(snapshot.CpuHandCards, cardMoveSpeed, cardTurnSpeed);
        }

        foreach (var card in allCards.cardList)
        {
            card.IsSelectable = false;
        }
        if (specialCards1 != null && specialCards1.cardList != null)
        {
            foreach (var card in specialCards1.cardList)
            {
                if (card != null) card.IsSelectable = false;
            }
        }
        foreach (var card in player1.cardsInArea)
        {
            card.IsSelectable = true;
        }
        if (player1 != null)
        {
            player1.SetMaxSelectableCount(snapshot.MaxHandTrashCount);
        }

        uiUpdateCoroutine = StartCoroutine(CheckUIUpdateComplete());
    }

    public void SetPlayerSpecialCardInputEnabled(bool enabled)
    {
        if (player1Special is SpecialCardArea specialCardArea)
        {
            specialCardArea.SetInputEnabled(enabled);
        }
    }

    private void UpdateSpecialCardArea(GameUiSnapshot snapshot, int playerId, CardArea targetArea, Cards sourceDeck)
    {
        if (targetArea == null) return;

        List<Card> specialCards = playerId == 0
            ? snapshot.PlayerSpecialCards
            : snapshot.CpuSpecialCards;
        List<Card> usedSpecialCards = playerId == 0
            ? snapshot.PlayerUsedSpecialCards
            : snapshot.CpuUsedSpecialCards;

        if (targetArea is SpecialCardArea specialCardArea)
        {
            bool canSelectSpecialCard = playerId == 0 && snapshot.PlayerCanSelectSpecialCard;
            specialCardArea.SetInputEnabled(canSelectSpecialCard);
            specialCardArea.SetUsedCards(usedSpecialCards);
        }

        var cards = specialCards;
        if (cards != null && cards.Count > 0)
        {
            bool showCardFaces = playerId == 0;
            foreach (var card in cards)
            {
                if (card != null && card.IsFaceUp != showCardFaces)
                {
                    card.ForceSetFaceUp(showCardFaces);
                }
            }

            List<Card> displayCards = BuildSpecialCardDisplayCards(
                cards,
                sourceDeck,
                includeNoUseCard: playerId == 0);
            SetSpecialCardTooltipsEnabled(displayCards, playerId == 0);
            targetArea.SetCardsBySpeed(displayCards, cardMoveSpeed, cardTurnSpeed);
        }
        else
        {
            targetArea.SetCardsBySpeed(new List<Card>(), cardMoveSpeed, cardTurnSpeed);
        }
    }

    private static void SetSpecialCardTooltipsEnabled(
        IEnumerable<Card> cards,
        bool enabled)
    {
        if (cards == null)
        {
            return;
        }

        foreach (Card card in cards)
        {
            if (card == null)
            {
                continue;
            }

            SpecialCardTooltipTarget tooltipTarget =
                card.GetComponent<SpecialCardTooltipTarget>();
            if (tooltipTarget != null)
            {
                tooltipTarget.SetTooltipEnabled(enabled);
            }
        }
    }

    private List<Card> BuildSpecialCardDisplayCards(
        List<Card> specialCards,
        Cards sourceDeck,
        bool includeNoUseCard)
    {
        List<Card> displayCards = new List<Card>();
        if (specialCards == null || specialCards.Count == 0)
        {
            return displayCards;
        }

        foreach (Card card in specialCards)
        {
            if (card == null || SpecialCardResolver.IsNoUseSpecialCard(card.CardData))
            {
                continue;
            }

            displayCards.Add(card);
        }

        if (!includeNoUseCard)
        {
            return displayCards;
        }

        Card noUseCard = FindNoUseSpecialCard(sourceDeck);
        if (noUseCard == null)
        {
            return displayCards;
        }

        noUseCard.IsSelectable = false;
        noUseCard.ForceSetFaceUp(true);
        displayCards.Add(noUseCard);
        return displayCards;
    }

    private Card FindNoUseSpecialCard(Cards sourceDeck)
    {
        if (sourceDeck == null || sourceDeck.cardList == null)
        {
            return null;
        }

        foreach (var card in sourceDeck.cardList)
        {
            if (card == null) continue;
            if (SpecialCardResolver.IsNoUseSpecialCard(card.CardData)) return card;
        }

        return null;
    }

    IEnumerator CheckUIUpdateComplete()
    {
        float startedAt = Time.realtimeSinceStartup;
        yield return null;
        while (true)
        {
            bool allComplete = true;

            foreach (var card in allCards.cardList)
            {
                if (!card.MoveComplete)
                {
                    allComplete = false;
                    break;
                }
            }

            if (allComplete && specialCards1 != null && specialCards1.cardList != null)
            {
                allComplete = AreCardsMoveComplete(specialCards1.cardList);
            }

            if (allComplete && specialCards2 != null && specialCards2.cardList != null)
            {
                allComplete = AreCardsMoveComplete(specialCards2.cardList);
            }

            if (H != null && H.IsAnimating) allComplete = false;
            if (allComplete) break;

            if (Time.realtimeSinceStartup - startedAt >= Mathf.Max(0.1f, uiUpdateRecoveryTimeout))
            {
                Debug.LogWarning($"UI update exceeded {uiUpdateRecoveryTimeout:0.##} seconds. Snapping cards to their targets.", this);
                SnapAllKnownCardsToTargets();
                break;
            }

            yield return null;
        }

        UIUpdateInProgress = false;
        uiUpdateCoroutine = null;
    }

    public void RecoverFromStalledUpdate()
    {
        CancelPendingUiUpdate(true);
    }

    private void CancelPendingUiUpdate(bool snapCards)
    {
        if (uiUpdateCoroutine != null)
        {
            StopCoroutine(uiUpdateCoroutine);
            uiUpdateCoroutine = null;
        }

        if (snapCards && UIUpdateInProgress)
        {
            SnapAllKnownCardsToTargets();
        }

        UIUpdateInProgress = false;
    }

    private void SnapAllKnownCardsToTargets()
    {
        recoveryCards.Clear();
        AddCardsForRecovery(allCards != null ? allCards.cardList : null);
        AddCardsForRecovery(specialCards1 != null ? specialCards1.cardList : null);
        AddCardsForRecovery(specialCards2 != null ? specialCards2.cardList : null);
        AddCardsForRecovery(deck != null ? deck.cardsInArea : null);
        AddCardsForRecovery(common != null ? common.cardsInArea : null);
        AddCardsForRecovery(player1 != null ? player1.cardsInArea : null);
        AddCardsForRecovery(player2 != null ? player2.cardsInArea : null);
        AddCardsForRecovery(player1Special != null ? player1Special.cardsInArea : null);
        AddCardsForRecovery(player2Special != null ? player2Special.cardsInArea : null);
        AddCardsForRecovery(trash != null ? trash.cardsInArea : null);

        foreach (Card card in recoveryCards)
        {
            if (card != null)
            {
                card.SnapToTargetPosition();
            }
        }
    }

    private void AddCardsForRecovery(IEnumerable<Card> cards)
    {
        if (cards == null) return;
        foreach (Card card in cards)
        {
            if (card != null)
            {
                recoveryCards.Add(card);
            }
        }
    }

    private bool AreCardsMoveComplete(List<Card> cards)
    {
        foreach (var card in cards)
        {
            if (card != null && !card.MoveComplete)
            {
                return false;
            }
        }

        return true;
    }
}
