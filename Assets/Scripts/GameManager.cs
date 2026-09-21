using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serialized Unity composition root for a game session.
/// Runtime orchestration lives in GameSessionFlow.
/// </summary>
public class GameManager : MonoBehaviour
{
    public GameState gameState = new GameState();

    [Header("Card Decks")]
    [SerializeField] private Cards allCards;

    [Header("Controllers")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CPUController cpuController;

    [Header("UI")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private ShowdownCutInPopup showdownCutInPopup;
    [SerializeField] private MatchResultPanel matchResultPanel;
    [SerializeField] private TitleUIManager titleUIManager;
    [SerializeField] private CharacterManager characterManager;

    [Header("Special Card Decks")]
    [SerializeField] private Cards specialCardsDeck1;
    [SerializeField] private Cards specialCardsDeck2;

    [Header("CPU Character Settings")]
    [Tooltip("Legacy fallback count. CPU mode progression now uses CpuLevelCatalog.")]
    [SerializeField] private int cpuSpecialCardCount = 4;
    [Tooltip("Per-character CPU difficulty settings. Character Index matches CPU level - 1.")]
    [SerializeField] private List<CpuCharacterSettings> cpuCharacterSettings =
        new List<CpuCharacterSettings>();

    private GameSessionFlow sessionFlow;
    private GameState sessionGameState;
    private Coroutine sessionCoroutine;
    private BattleGroundHud battleGroundHud;

    private void Start()
    {
        EnsureSessionFlow();
    }

    public void StartGameWithMode(GameModeData modeData)
    {
        EnsureSessionFlow();
        if (!sessionFlow.TryStart(modeData, out IEnumerator routine))
        {
            return;
        }

        battleGroundHud = BattleGroundHud.GetOrCreate(uiManager, this);
        battleGroundHud?.Configure(
            modeData.Mode == GameModeData.GameMode.BattleGroundMode,
            RequestBattleGroundSurrender);

        sessionCoroutine = StartCoroutine(RunSession(routine));
    }

    private IEnumerator RunSession(IEnumerator routine)
    {
        yield return routine;
        sessionCoroutine = null;
        battleGroundHud?.Configure(false, null);
    }

    public void RequestBattleGroundSurrender()
    {
        sessionFlow?.RequestBattleGroundSurrender();
    }

    private void EnsureSessionFlow()
    {
        if (gameState == null)
        {
            gameState = new GameState();
        }

        if (sessionFlow != null && ReferenceEquals(sessionGameState, gameState))
        {
            return;
        }

        if (sessionCoroutine != null)
        {
            StopCoroutine(sessionCoroutine);
            sessionCoroutine = null;
        }

        sessionFlow?.Cancel();
        sessionGameState = gameState;
        sessionFlow = new GameSessionFlow(
            this,
            gameState,
            allCards,
            playerController,
            cpuController,
            uiManager,
            characterManager,
            specialCardsDeck1,
            specialCardsDeck2,
            cpuSpecialCardCount,
            cpuCharacterSettings,
            showdownCutInPopup,
            matchResultPanel,
            titleUIManager,
            popup => showdownCutInPopup = popup,
            panel => matchResultPanel = panel);
    }

    private void OnDisable()
    {
        sessionFlow?.Cancel();
        battleGroundHud?.Configure(false, null);
        if (sessionCoroutine != null)
        {
            StopCoroutine(sessionCoroutine);
            sessionCoroutine = null;
        }
    }
}
