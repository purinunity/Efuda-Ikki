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

    public bool UIUpdateInProgress { get; private set; } = false;

    private void Awake()
    {
    }

    private void OnDestroy()
    {
    }

    public void UIUpdate(GameState state, float duration)
    {
        UIUpdateInProgress = true;

        r.text = "第" + state.RoundNumber.ToString() + "局";
        L1.text = state.PlayerStates[0].LifePoints.ToString();
        L2.text = state.PlayerStates[1].LifePoints.ToString();

        var now = HandEvaluator.EvaluateHand(state.PlayerStates[0].HandCards, state.commonCards);
        H.SetRole(now.Name, duration);
        playerRemainTrashCount.UpdateRemainTrashCount(state.maxHandTrashTurn - state.PlayerStates[0].HandTrashTurnsUsed);

        // 特殊札表示（プレイヤー状態が空なら GameModeData + sps から復元）
        UpdateSpecialCardArea(state, 0, player1Special, specialCards1);
        UpdateSpecialCardArea(state, 1, player2Special, specialCards2);

        deck.SetCardsBySpeed(state.deckCards, cardMoveSpeed, cardTurnSpeed);
        common.SetCardsBySpeed(state.commonCards, cardMoveSpeed, cardTurnSpeed);
        trash.SetCardsBySpeed(state.trashCards, cardMoveSpeed, cardTurnSpeed);

        foreach (var playerState in state.PlayerStates)
        {
            if (playerState.PlayerId == 0 && playerState.HandCards.Count == 5)
            {
                player1.SetCardsBySpeed(playerState.HandCards, cardMoveSpeed, cardTurnSpeed);
            }
            else if (playerState.PlayerId == 1 && playerState.HandCards.Count == 5)
            {
                player2.SetCardsBySpeed(playerState.HandCards, cardMoveSpeed, cardTurnSpeed);
            }
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
            player1.SetMaxSelectableCount(state.maxHandTrashCount);
        }

        StartCoroutine(CheckUIUpdateComplete());
    }

    public void SetPlayerSpecialCardInputEnabled(bool enabled)
    {
        if (player1Special is SpecialCardArea specialCardArea)
        {
            specialCardArea.SetInputEnabled(enabled);
        }
    }

    private void UpdateSpecialCardArea(GameState state, int playerId, CardArea targetArea, Cards sourceDeck)
    {
        if (targetArea == null) return;

        PlayerState playerState = state != null &&
                                  state.PlayerStates != null &&
                                  playerId >= 0 &&
                                  playerId < state.PlayerStates.Count
            ? state.PlayerStates[playerId]
            : null;

        if (targetArea is SpecialCardArea specialCardArea)
        {
            bool canSelectSpecialCard = playerId == 0 &&
                                        playerState != null &&
                                        playerState.HandTrashTurnsUsed < Mathf.Max(0, state.maxHandTrashTurn);
            specialCardArea.SetInputEnabled(canSelectSpecialCard);
            specialCardArea.SetUsedCards(playerState != null ? playerState.UsedSpecialCards : null);
        }

        var cards = ResolveSpecialCardsForPlayer(state, playerId);
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
            targetArea.SetCardsBySpeed(displayCards, cardMoveSpeed, cardTurnSpeed);
        }
        else
        {
            targetArea.SetCardsBySpeed(new List<Card>(), cardMoveSpeed, cardTurnSpeed);
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

    private List<Card> ResolveSpecialCardsForPlayer(GameState state, int playerId)
    {
        if (state == null || state.PlayerStates == null) return null;
        if (playerId < 0 || playerId >= state.PlayerStates.Count) return null;

        var specials = state.PlayerStates[playerId].SpecialCards;
        if (specials != null && specials.Count > 0) return specials;

        return null;
    }

    IEnumerator CheckUIUpdateComplete()
    {
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

            yield return null;
        }

        UIUpdateInProgress = false;
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
