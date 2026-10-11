using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MatchRoundResult
{
    public int RoundNumber { get; }
    public int WinnerIndex { get; }
    public int Damage { get; }
    public int PlayerLifeBefore { get; }
    public int PlayerLifeAfter { get; }
    public int CpuLifeBefore { get; }
    public int CpuLifeAfter { get; }
    public string PlayerRoleName { get; }
    public int PlayerScore { get; }
    public string CpuRoleName { get; }
    public int CpuScore { get; }
    public IReadOnlyList<Sprite> PlayerHandSprites { get; }
    public IReadOnlyList<Sprite> CpuHandSprites { get; }
    public IReadOnlyList<Sprite> CommonCardSprites { get; }
    public Sprite PlayerSpecialCardSprite { get; }
    public Sprite CpuSpecialCardSprite { get; }

    public MatchRoundResult(
        int roundNumber,
        int winnerIndex,
        int damage,
        int playerLifeBefore,
        int playerLifeAfter,
        int cpuLifeBefore,
        int cpuLifeAfter,
        string playerRoleName = "なし",
        int playerScore = 0,
        string cpuRoleName = "なし",
        int cpuScore = 0,
        IReadOnlyList<Sprite> playerHandSprites = null,
        IReadOnlyList<Sprite> cpuHandSprites = null,
        IReadOnlyList<Sprite> commonCardSprites = null,
        Sprite playerSpecialCardSprite = null,
        Sprite cpuSpecialCardSprite = null)
    {
        RoundNumber = roundNumber;
        WinnerIndex = winnerIndex;
        Damage = damage;
        PlayerLifeBefore = playerLifeBefore;
        PlayerLifeAfter = playerLifeAfter;
        CpuLifeBefore = cpuLifeBefore;
        CpuLifeAfter = cpuLifeAfter;
        PlayerRoleName = playerRoleName ?? "なし";
        PlayerScore = Mathf.Max(0, playerScore);
        CpuRoleName = cpuRoleName ?? "なし";
        CpuScore = Mathf.Max(0, cpuScore);
        PlayerHandSprites = playerHandSprites ?? new Sprite[0];
        CpuHandSprites = cpuHandSprites ?? new Sprite[0];
        CommonCardSprites = commonCardSprites ?? new Sprite[0];
        PlayerSpecialCardSprite = playerSpecialCardSprite;
        CpuSpecialCardSprite = cpuSpecialCardSprite;
    }
}

/// <summary>
/// Displays match results using slots authored in latest.unity. Layout belongs
/// to the scene; runtime code changes content and visibility only.
/// </summary>
public sealed class MatchResultPanel : MonoBehaviour
{
    private static readonly Color BackdropColor = new Color(0.03f, 0.035f, 0.04f, 0.92f);
    private static readonly Color AccentColor = new Color(0.88f, 0.72f, 0.32f, 1f);

    [Header("Scene References")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI summaryText;
    [SerializeField] private Button continueButton;
    [SerializeField] private TextMeshProUGUI continueButtonText;
    [SerializeField] private Image resultBackground;
    [SerializeField] private MatchResultVisualAssets visualAssets;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI playerRoleText;
    [SerializeField] private TextMeshProUGUI playerScoreText;
    [SerializeField] private TextMeshProUGUI cpuNameText;
    [SerializeField] private TextMeshProUGUI cpuRoleText;
    [SerializeField] private TextMeshProUGUI cpuScoreText;
    [SerializeField] private Image playerCharacterImage;
    [SerializeField] private Image cpuCharacterImage;
    [SerializeField] private List<Image> playerHandImages = new List<Image>();
    [SerializeField] private List<Image> cpuHandImages = new List<Image>();
    [SerializeField] private List<Image> playerCommonImages = new List<Image>();
    [SerializeField] private List<Image> cpuCommonImages = new List<Image>();
    [SerializeField] private Image playerSpecialImage;
    [SerializeField] private Image cpuSpecialImage;

    private Button subscribedContinueButton;
    private Sprite playerCharacterSprite;
    private Sprite cpuCharacterSprite;
    private bool isWaiting;
    private bool initialized;

    private void OnEnable() => WireContinueButton();

    private void OnDisable()
    {
        UnwireContinueButton();
        isWaiting = false;
    }

    private void OnDestroy() => UnwireContinueButton();

    public void SetCharacterSprites(Sprite playerSprite, Sprite cpuSprite)
    {
        playerCharacterSprite = playerSprite;
        cpuCharacterSprite = cpuSprite;
    }

    public IEnumerator Show(
        int cpuLevel,
        IReadOnlyList<MatchRoundResult> results,
        bool playerWon,
        string buttonLabel,
        string summaryOverride = null)
    {
        Initialize();
        if (!initialized)
        {
            yield break;
        }

        Populate(results, playerWon, buttonLabel, summaryOverride);
        gameObject.SetActive(true);
        isWaiting = true;

        while (isWaiting)
        {
            yield return null;
        }

        gameObject.SetActive(false);
    }

    public void CancelDisplay()
    {
        isWaiting = false;
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    private void Initialize()
    {
        if (initialized) return;
        if (visualAssets == null)
        {
            visualAssets = Resources.Load<MatchResultVisualAssets>("MatchResultVisualAssets");
        }

        if (!HasRequiredSceneReferences())
        {
            Debug.LogError("MatchResultPanel: scene references are incomplete.", this);
            return;
        }

        initialized = true;
        WireContinueButton();
    }

#if UNITY_EDITOR
    public void ApplyEditorPreview()
    {
        if (visualAssets == null)
        {
            visualAssets = Resources.Load<MatchResultVisualAssets>("MatchResultVisualAssets");
        }

        if (resultBackground != null)
        {
            resultBackground.sprite = visualAssets != null ? visualAssets.winBackground : null;
            resultBackground.color = resultBackground.sprite != null ? Color.white : BackdropColor;
        }
        if (titleText != null) titleText.text = "勝利";
        if (summaryText != null) summaryText.text = "対戦結果";
        if (playerNameText != null) playerNameText.text = "プレイヤー";
        if (cpuNameText != null) cpuNameText.text = "CPU";
        if (playerRoleText != null) playerRoleText.text = "最高得点役";
        if (cpuRoleText != null) cpuRoleText.text = "最高得点役";
        if (playerScoreText != null) playerScoreText.text = "0点";
        if (cpuScoreText != null) cpuScoreText.text = "0点";
        if (continueButtonText != null)
        {
            continueButtonText.text = "キャラクター選択へ";
            continueButtonText.gameObject.SetActive(true);
        }
    }
#endif

    private bool HasRequiredSceneReferences()
    {
        return panel != null && resultBackground != null && summaryText != null &&
               continueButton != null && continueButtonText != null &&
               playerCharacterImage != null && cpuCharacterImage != null &&
               playerHandImages != null && playerHandImages.Count == 5 &&
               cpuHandImages != null && cpuHandImages.Count == 5 &&
               playerCommonImages != null && playerCommonImages.Count == 2 &&
               cpuCommonImages != null && cpuCommonImages.Count == 2 &&
               playerSpecialImage != null && cpuSpecialImage != null;
    }

    private void Populate(
        IReadOnlyList<MatchRoundResult> results,
        bool playerWon,
        string buttonLabel,
        string summaryOverride)
    {
        resultBackground.sprite = visualAssets != null
            ? (playerWon ? visualAssets.winBackground : visualAssets.loseBackground)
            : null;
        resultBackground.color = resultBackground.sprite != null ? Color.white : BackdropColor;

        GameModeData modeData = GameModeManager.GetGameModeData();
        bool battleGround = modeData != null && modeData.Mode == GameModeData.GameMode.BattleGroundMode;
        summaryText.text = !string.IsNullOrEmpty(summaryOverride)
            ? summaryOverride
            : battleGround
                ? $"今回 {modeData.CurrentWinStreak}人抜き　最高 {GameProgressStore.BestBattleGroundStreak}人抜き"
                : string.Empty;
        summaryText.gameObject.SetActive(!string.IsNullOrEmpty(summaryText.text));

        continueButtonText.text = buttonLabel;
        ConfigureContinueButtonVisual(buttonLabel);

        MatchRoundResult playerBest = FindBestResult(results, true);
        MatchRoundResult cpuBest = FindBestResult(results, false);
        SetArtworkSprite(playerCharacterImage, playerCharacterSprite);
        SetArtworkSprite(cpuCharacterImage, cpuCharacterSprite);
        PopulateCardRow(playerBest, true, playerHandImages, playerCommonImages, playerSpecialImage);
        PopulateCardRow(cpuBest, false, cpuHandImages, cpuCommonImages, cpuSpecialImage);
    }

    private void ConfigureContinueButtonVisual(string buttonLabel)
    {
        Image image = continueButton.GetComponent<Image>();
        bool characterSelect = !string.IsNullOrEmpty(buttonLabel) && buttonLabel.Contains("キャラクター選択");
        ShowdownCutInAssetSet assets = Resources.Load<ShowdownCutInAssetSet>("ShowdownCutInAssets");
        Sprite normal = characterSelect && assets != null ? assets.characterSelectButton : null;
        Sprite pressed = characterSelect && assets != null ? assets.characterSelectButtonPressed : null;

        if (image != null)
        {
            image.sprite = normal;
            image.color = normal != null ? Color.white : AccentColor;
            image.preserveAspect = normal != null;
        }

        SpriteState state = continueButton.spriteState;
        state.highlightedSprite = pressed;
        state.pressedSprite = pressed;
        state.selectedSprite = pressed;
        continueButton.spriteState = state;
        continueButton.transition = pressed != null
            ? Selectable.Transition.SpriteSwap
            : Selectable.Transition.ColorTint;
        continueButtonText.gameObject.SetActive(!characterSelect);
    }

    private void WireContinueButton()
    {
        UnwireContinueButton();
        if (continueButton == null) return;
        subscribedContinueButton = continueButton;
        subscribedContinueButton.onClick.AddListener(HandleContinueClicked);
    }

    private void UnwireContinueButton()
    {
        if (subscribedContinueButton != null)
        {
            subscribedContinueButton.onClick.RemoveListener(HandleContinueClicked);
        }
        subscribedContinueButton = null;
    }

    private void HandleContinueClicked() => isWaiting = false;

    private static void PopulateCardRow(
        MatchRoundResult result,
        bool player,
        IReadOnlyList<Image> handImages,
        IReadOnlyList<Image> commonImages,
        Image specialImage)
    {
        SetArtworkSprites(handImages, result != null
            ? (player ? result.PlayerHandSprites : result.CpuHandSprites)
            : null);
        SetArtworkSprites(commonImages, result != null ? result.CommonCardSprites : null);
        SetArtworkSprite(specialImage, result != null
            ? (player ? result.PlayerSpecialCardSprite : result.CpuSpecialCardSprite)
            : null);
    }

    private static void SetArtworkSprites(IReadOnlyList<Image> images, IReadOnlyList<Sprite> sprites)
    {
        if (images == null) return;
        for (int i = 0; i < images.Count; i++)
        {
            SetArtworkSprite(images[i], sprites != null && i < sprites.Count ? sprites[i] : null);
        }
    }

    private static void SetArtworkSprite(Image image, Sprite sprite)
    {
        if (image == null) return;
        image.sprite = sprite;
        image.color = sprite != null ? Color.white : Color.clear;
    }

    private static MatchRoundResult FindBestResult(IReadOnlyList<MatchRoundResult> results, bool player)
    {
        MatchRoundResult best = null;
        if (results == null) return null;
        foreach (MatchRoundResult result in results)
        {
            if (result == null) continue;
            int score = player ? result.PlayerScore : result.CpuScore;
            int bestScore = best == null ? -1 : player ? best.PlayerScore : best.CpuScore;
            if (score > bestScore) best = result;
        }
        return best;
    }

    private void OnValidate()
    {
        if (Application.isPlaying || !gameObject.scene.IsValid()) return;
        if (!HasRequiredSceneReferences())
        {
            Debug.LogWarning("MatchResultPanel: assign all fixed result slots in the Inspector.", this);
        }
    }
}
