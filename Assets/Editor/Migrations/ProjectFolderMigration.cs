using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Explicit, one-time STR-07 revision 1 migration. Never runs at startup or on validation.</summary>
public static class ProjectFolderMigration
{
    private sealed class Move
    {
        public readonly string from, to, guid;
        public Move(string from, string to, string guid = null) { this.from = from; this.to = to; this.guid = guid; }
    }

    [Serializable] public sealed class FileState
    {
        public string from, to, guid, sourceHash, metaHash, originalText;
    }
    [Serializable] public sealed class Operation { public string from, to, guid; }
    [Serializable] public sealed class DeletedFolder { public string path, metaBase64; }
    [Serializable] public sealed class Record
    {
        public string design = "STR-07 revision 1", startedAt, error;
        public bool completed, rolledBack;
        public List<FileState> files = new List<FileState>();
        public List<Operation> operations = new List<Operation>();
        public List<string> createdFolders = new List<string>();
        public List<DeletedFolder> deletedFolders = new List<DeletedFolder>();
    }

    private const string UiMigrationSource = "Assets/Editor/UiPrefabMigration.cs";
    private static string RecordPath => Path.Combine(Directory.GetParent(Application.dataPath).FullName,
        "docs/design/evidence/str-07/migration-record.json");

    // Self-contained approved map: normal Editor use and builds never read an ignored docs manifest.
    private static readonly Move[] Moves =
    {
        new Move("Assets/Scripts/GameManager.cs", "Assets/Scripts/Gameplay/Session/GameManager.cs", "fe9c00b4e7643d145a7bd47923f90ecd"),
        new Move("Assets/Scripts/GameSessionFlow.cs", "Assets/Scripts/Gameplay/Session/GameSessionFlow.cs", "11e7770a85a64a61a8de65949e336299"),
        new Move("Assets/Scripts/MatchFlow.cs", "Assets/Scripts/Gameplay/Session/MatchFlow.cs", "098f29cec8299b0438c0c9763ebb2c3d"),
        new Move("Assets/Scripts/RoundFlowService.cs", "Assets/Scripts/Gameplay/Session/RoundFlowService.cs", "df891c92a4d742838905cfb3d0c9b5a9"),
        new Move("Assets/Scripts/ShowdownFlowService.cs", "Assets/Scripts/Gameplay/Session/ShowdownFlowService.cs", "70d4c71dcb3d438892f360b47ea766a5"),
        new Move("Assets/Scripts/ISessionPresentation.cs", "Assets/Scripts/Gameplay/Session/ISessionPresentation.cs", "b5089f8169b3e5f4d80f79581ab69092"),
        new Move("Assets/Scripts/ISessionProgress.cs", "Assets/Scripts/Gameplay/Session/ISessionProgress.cs", "93c7b10d5f47a5a43b71601f5d65ba4e"),
        new Move("Assets/Scripts/HandEvaluator.cs", "Assets/Scripts/Gameplay/Rules/HandEvaluator.cs", "83ac2b3ab9107014ca435ce2b2bc44c5"),
        new Move("Assets/Scripts/HandRoleCatalog.cs", "Assets/Scripts/Gameplay/Rules/HandRoleCatalog.cs", "56be7268751fbe347916bdb1826cbd72"),
        new Move("Assets/Scripts/SpecialCardResolver.cs", "Assets/Scripts/Gameplay/Rules/SpecialCardResolver.cs", "5de834cf25dd466ba038cd95636096b3"),
        new Move("Assets/Scripts/Card/Card.cs", "Assets/Scripts/Gameplay/Cards/Card.cs", "70558b6d1aa642a46b24f33ebda40dfc"),
        new Move("Assets/Scripts/Card/CardData.cs", "Assets/Scripts/Gameplay/Cards/CardData.cs", "f058b57e079b844468980c2e1cb266bb"),
        new Move("Assets/Scripts/Card/CardMotion.cs", "Assets/Scripts/Gameplay/Cards/CardMotion.cs", "a6d1a297e76205b43a5c2fe079f36da6"),
        new Move("Assets/Scripts/Card/CardPatternUtility.cs", "Assets/Scripts/Gameplay/Cards/CardPatternUtility.cs", "48016fc2c8394aa5bc3f88cb20d1cde7"),
        new Move("Assets/Scripts/Card/CardRuntimeState.cs", "Assets/Scripts/Gameplay/Cards/CardRuntimeState.cs", "9afc9a3af4b04314b87f9c79375d9346"),
        new Move("Assets/Scripts/Card/CardSelectionUtility.cs", "Assets/Scripts/Gameplay/Cards/CardSelectionUtility.cs", "3bf7fd9427ad485e8f413d5843bcac82"),
        new Move("Assets/Scripts/Cards/Cards.cs", "Assets/Scripts/Gameplay/Cards/Cards.cs", "dda9a6cb17179e5488180aa037ccf573"),
        new Move("Assets/Scripts/Cards/CardsData.cs", "Assets/Scripts/Gameplay/Cards/CardsData.cs", "1b80e18fe90778f4896d3ef8ce09cc5e"),
        new Move("Assets/Scripts/Data/SpecialCardCatalog.cs", "Assets/Scripts/Gameplay/Cards/SpecialCardCatalog.cs", "091a927464e94a888558717cff528107"),
        new Move("Assets/Scripts/Controller/Controller.cs", "Assets/Scripts/Gameplay/Controllers/Controller.cs", "71bf07d3a0e18d246a8c4d534f60d6bb"),
        new Move("Assets/Scripts/Controller/CPUController.cs", "Assets/Scripts/Gameplay/Controllers/CPUController.cs", "19d79c6325bc3ae41bbea24ee0240cf1"),
        new Move("Assets/Scripts/Controller/PlayerController.cs", "Assets/Scripts/Gameplay/Controllers/PlayerController.cs", "bd7794a3bda984449b562c5c30aa08eb"),
        new Move("Assets/Scripts/Controller/CpuCharacterSettings.cs", "Assets/Scripts/Gameplay/Controllers/CpuCharacterSettings.cs", "cef9d99f3d06486cbaf9a22b08d42c15"),
        new Move("Assets/Scripts/Controller/CpuDifficultySettings.cs", "Assets/Scripts/Gameplay/Controllers/CpuDifficultySettings.cs", "8f1d7b9c2a6e4f39aa2d4cb7e83591ad"),
        new Move("Assets/Scripts/Controller/CpuLevelDefinition.cs", "Assets/Scripts/Gameplay/Controllers/CpuLevelDefinition.cs", "3fdd8a30f4b44af2a3c6835d04ff5cd2"),
        new Move("Assets/Scripts/Controller/CpuSetupService.cs", "Assets/Scripts/Gameplay/Controllers/CpuSetupService.cs", "9c19f59e5fa144ae901dfcd79c076a37"),
        new Move("Assets/Scripts/State/GameState.cs", "Assets/Scripts/Gameplay/State/GameState.cs", "8dfd3f30b253fc24a8c84a2fab334d44"),
        new Move("Assets/Scripts/State/PlayerState.cs", "Assets/Scripts/Gameplay/State/PlayerState.cs", "21d050ee1176f9341b01739e01170092"),
        new Move("Assets/Scripts/UI/Menu/GameModeData.cs", "Assets/Scripts/Gameplay/State/GameModeData.cs", "5b26686b0827f8c41af3a7e5ac6f9664"),
        new Move("Assets/Scripts/UI/Menu/GameModeManager.cs", "Assets/Scripts/Gameplay/State/GameModeManager.cs", "b6a00655337424c429da663c342aa302"),
        new Move("Assets/Scripts/BattleGround/BattleGroundFlow.cs", "Assets/Scripts/Gameplay/BattleGround/BattleGroundFlow.cs", "51918e9712e80784aaded335dbf341cf"),
        new Move("Assets/Scripts/BattleGround/BattleGroundOpponentSelector.cs", "Assets/Scripts/Gameplay/BattleGround/BattleGroundOpponentSelector.cs", "070f45b73b9b4a4a9e2b149ff59fcfa2"),
        new Move("Assets/Scripts/BattleGround/BattleGroundRewardService.cs", "Assets/Scripts/Gameplay/BattleGround/BattleGroundRewardService.cs", "d72d44d68a1880146b5fb15b2c44d4d0"),
        new Move("Assets/Scripts/BattleGround/BattleGroundRunState.cs", "Assets/Scripts/Gameplay/BattleGround/BattleGroundRunState.cs", "fb323bc5d7b74394c81da0ba80e4a4c9"),
        new Move("Assets/Scripts/GameplaySessionPresentation.cs", "Assets/Scripts/UI/Presentation/GameplaySessionPresentation.cs", "a9549fa4f7541b24cb984355bffe36b8"),
        new Move("Assets/Scripts/GameUiPresenter.cs", "Assets/Scripts/UI/Presentation/GameUiPresenter.cs", "ee31426944db456cb64c90e115851c2a"),
        new Move("Assets/Scripts/GameUiSnapshot.cs", "Assets/Scripts/UI/Presentation/GameUiSnapshot.cs", "f8afe067f68e4e14af9decdde8244dc1"),
        new Move("Assets/Scripts/GameUiUpdateService.cs", "Assets/Scripts/UI/Presentation/GameUiUpdateService.cs", "f621ed1f7c60498094ac401c25cd9f2a"),
        new Move("Assets/Scripts/ShowdownPresentationService.cs", "Assets/Scripts/UI/Presentation/ShowdownPresentationService.cs", "42c74054e74b4ae48bd4a0b762b533a7"),
        new Move("Assets/Scripts/GameEndNavigationService.cs", "Assets/Scripts/UI/Presentation/GameEndNavigationService.cs", "4ef18a465135452cbf6df2afbe73f351"),
        new Move("Assets/Scripts/UI/UIManager.cs", "Assets/Scripts/UI/Gameplay/UIManager.cs", "690094bef49fea7428761d4e90a0724d"),
        new Move("Assets/Scripts/UI/GameplayHudView.cs", "Assets/Scripts/UI/Gameplay/GameplayHudView.cs", "6962876c8a50d0b4197974be121bcb25"),
        new Move("Assets/Scripts/UI/GameplayUiReferences.cs", "Assets/Scripts/UI/Gameplay/GameplayUiReferences.cs", "2ded7bf058c72ad498cc0586c149b7b8"),
        new Move("Assets/Scripts/UI/PlayerRemainTrashCount.cs", "Assets/Scripts/UI/Gameplay/PlayerRemainTrashCount.cs", "15b838ca1373b4546891b14e92afa505"),
        new Move("Assets/Scripts/UI/PlayerRole.cs", "Assets/Scripts/UI/Gameplay/PlayerRole.cs", "5d277557048561f43a4e64c686134e07"),
        new Move("Assets/Scripts/UI/CardArea.cs", "Assets/Scripts/UI/Cards/CardArea.cs", "8879545e23118d0458c993800a6ddaf1"),
        new Move("Assets/Scripts/UI/CardRowView.cs", "Assets/Scripts/UI/Cards/CardRowView.cs", "a0d8b753e05168240966c76226846521"),
        new Move("Assets/Scripts/UI/LimitedSelectableCardArea.cs", "Assets/Scripts/UI/Cards/LimitedSelectableCardArea.cs", "381ee1aa6976dc947b8341c72c28f608"),
        new Move("Assets/Scripts/UI/PopupPreviewCardArea.cs", "Assets/Scripts/UI/Cards/PopupPreviewCardArea.cs", "542806ae0a11769428daa9de078a926a"),
        new Move("Assets/Scripts/UI/SpecialCardArea.cs", "Assets/Scripts/UI/Cards/SpecialCardArea.cs", "be8245a22e0e15947a4ddb1111ca1fa9"),
        new Move("Assets/Scripts/UI/SpecialCardSelection.cs", "Assets/Scripts/UI/Cards/SpecialCardSelection.cs", "4708705122b15d7458a8d3bdc4c6c5b8"),
        new Move("Assets/Scripts/UI/SpecialCardStackView.cs", "Assets/Scripts/UI/Cards/SpecialCardStackView.cs", "7344d6f56b5f5c7448ed79532ee46810"),
        new Move("Assets/Scripts/UI/UiCardSlot.cs", "Assets/Scripts/UI/Cards/UiCardSlot.cs", "bc57318fbb0f0be42a741fe1160737b9"),
        new Move("Assets/Scripts/UI/CoinTossPanel.cs", "Assets/Scripts/UI/Overlays/CoinTossPanel.cs", "5351422c1adb8f34c86556e17eea38f9"),
        new Move("Assets/Scripts/UI/DiscardPreviewPanel.cs", "Assets/Scripts/UI/Overlays/DiscardPreviewPanel.cs", "a7e00c82265363846abb67fa37fa6a07"),
        new Move("Assets/Scripts/UI/HandRevealPanel.cs", "Assets/Scripts/UI/Overlays/HandRevealPanel.cs", "32b7844ea879b3a44a744660db587f0b"),
        new Move("Assets/Scripts/UI/MatchResultPanel.cs", "Assets/Scripts/UI/Overlays/MatchResultPanel.cs", "8c291b33532242e38468ad27dc01a6e7"),
        new Move("Assets/Scripts/UI/MatchResultVisualAssets.cs", "Assets/Scripts/UI/Overlays/MatchResultVisualAssets.cs", "7bdb70fd9e058fc408c56e4b603efd38"),
        new Move("Assets/Scripts/UI/RoleListPanelController.cs", "Assets/Scripts/UI/Overlays/RoleListPanelController.cs", "ec7f72adc3d7ac644993778d2b843955"),
        new Move("Assets/Scripts/UI/ShowdownCutInAssetSet.cs", "Assets/Scripts/UI/Showdown/ShowdownCutInAssetSet.cs", "7f21b763ed1541cf8f94bb4ee9f955cb"),
        new Move("Assets/Scripts/UI/ShowdownCutInDataBuilder.cs", "Assets/Scripts/UI/Showdown/ShowdownCutInDataBuilder.cs", "3b35a3fcf42e419fa79c02d7d3a72b52"),
        new Move("Assets/Scripts/UI/ShowdownCutInPopup.cs", "Assets/Scripts/UI/Showdown/ShowdownCutInPopup.cs", "8c3dfba0c3a34d1289a9f3e42f8f3d12"),
        new Move("Assets/Scripts/UI/ShowdownCutInPopup.Data.cs", "Assets/Scripts/UI/Showdown/ShowdownCutInPopup.Data.cs", "006c6c342e994a1fb6370d012418f6c6"),
        new Move("Assets/Scripts/UI/ShowdownCutInPopup.Layout.cs", "Assets/Scripts/UI/Showdown/ShowdownCutInPopup.Layout.cs", "1bcb4dc496df4c23a6d65da5a00f84a4"),
        new Move("Assets/Scripts/UI/ShowdownCutInPopup.Sequence.cs", "Assets/Scripts/UI/Showdown/ShowdownCutInPopup.Sequence.cs", "df5f008cf9fa42bd93ca63263dff6dfc"),
        new Move("Assets/Scripts/UI/ShowdownCutInPopup.View.cs", "Assets/Scripts/UI/Showdown/ShowdownCutInPopup.View.cs", "fe1bf29d704d433996be00e51a4c2ae2"),
        new Move("Assets/Scripts/UI/ShowdownCutInPopupProvider.cs", "Assets/Scripts/UI/Showdown/ShowdownCutInPopupProvider.cs", "b871b68fca1647a7bd34a1d629af25db"),
        new Move("Assets/Scripts/UI/SpecialCardTooltip.cs", "Assets/Scripts/UI/Tooltips/SpecialCardTooltip.cs", "b36ddff6fb3a4fbaa8dca245986d0d0e"),
        new Move("Assets/Scripts/UI/SpecialCardTooltipImageTarget.cs", "Assets/Scripts/UI/Tooltips/SpecialCardTooltipImageTarget.cs", "3b4d37855128f3a468534d37bb788b2f"),
        new Move("Assets/Scripts/UI/SpecialCardTooltipTarget.cs", "Assets/Scripts/UI/Tooltips/SpecialCardTooltipTarget.cs", "2c6c5e6067c6477c8fe7056abd77a7a8"),
        new Move("Assets/Scripts/BattleGround/BattleGroundHud.cs", "Assets/Scripts/UI/BattleGround/BattleGroundHud.cs", "5efedb8b909502f4eaec827a7d4f9841"),
        new Move("Assets/Scripts/BattleGround/BattleGroundRewardPanel.cs", "Assets/Scripts/UI/BattleGround/BattleGroundRewardPanel.cs", "e971dea2cba063a499f098d350324189"),
        new Move("Assets/Scripts/BattleGround/BattleGroundVisualAssets.cs", "Assets/Scripts/UI/BattleGround/BattleGroundVisualAssets.cs", "fb2528f4fc558e24faaeeb276d8c7292"),
        new Move("Assets/Scripts/BattleGround/BattleGroundVisualTheme.cs", "Assets/Scripts/UI/BattleGround/BattleGroundVisualTheme.cs", "2adedcfe0f2c09f42b393ce702349821"),
        new Move("Assets/Scripts/Character/CharacterManager.cs", "Assets/Scripts/UI/Characters/CharacterManager.cs", "1bb19d552cd612447878e72a8917b92e"),
        new Move("Assets/Scripts/Option/FullscreenToggle.cs", "Assets/Scripts/UI/Settings/FullscreenToggle.cs", "ad2cacd1ee37aea4db1bcc0f77a9404e"),
        new Move("Assets/Scripts/Option/VolumeBar.cs", "Assets/Scripts/UI/Settings/VolumeBar.cs", "0d80156ad1bd02f489612dd24208e8cb"),
        new Move("Assets/Scripts/Option/LanguageSelector.cs", "Assets/Scripts/UI/Settings/LanguageSelector.cs", "0690ef961dbb3f045af2dfe6f2417384"),
        new Move("Assets/Scripts/Option/LocalizeSprite.cs", "Assets/Scripts/UI/Localization/LocalizeSprite.cs", "98c1c6119537ac4459f2f18b7aedf230"),
        new Move("Assets/Scripts/Option/LocalizeTmpFont.cs", "Assets/Scripts/UI/Localization/LocalizeTmpFont.cs", "004deb2bdb8667a45aea29be7286e467"),
        new Move("Assets/Scripts/UI/CloseButtonStyle.cs", "Assets/Scripts/UI/Shared/CloseButtonStyle.cs", "0ed3fd7c98ec25446aecbdd35d966002"),
        new Move("Assets/Scripts/UI/RuntimeUiFactory.cs", "Assets/Scripts/UI/Shared/RuntimeUiFactory.cs", "e69df59bf1b84445ac747433a0bb90fa"),
        new Move("Assets/Scripts/UI/UiRootCoordinator.cs", "Assets/Scripts/UI/Shared/UiRootCoordinator.cs", "2edad3ec22ef96d46ba53c2dd7aba0e3"),
        new Move("Assets/Scripts/GameProgressStore.cs", "Assets/Scripts/Infrastructure/Persistence/GameProgressStore.cs", "0e717082cf6f45f99041936b4bb711ec"),
        new Move("Assets/Scripts/Data/PlayerPrefsProgressRepository.cs", "Assets/Scripts/Infrastructure/Persistence/PlayerPrefsProgressRepository.cs", "92dad06310c24671bbbe850aa6906e4c"),
        new Move("Assets/Scripts/Option/GameSettings.cs", "Assets/Scripts/Infrastructure/Settings/GameSettings.cs", "b8381d5e7ee6ba74cab0d5aeacec4dd3"),
        new Move("Assets/Scripts/Option/FirstLaunchLanguage.cs", "Assets/Scripts/Infrastructure/Localization/FirstLaunchLanguage.cs", "0d9eddc37789e3b40a23b2b5d4424dcf"),
        new Move("Assets/Scripts/Option/LanguageManager.cs", "Assets/Scripts/Infrastructure/Localization/LanguageManager.cs", "42377e364eb915b44a2b1f3f8880f6e4"),
        new Move("Assets/Scripts/UnityRandomSource.cs", "Assets/Scripts/Infrastructure/Random/UnityRandomSource.cs", "083e9fcd8e054c5483629f8e2b3dbd41"),
        new Move("Assets/ScriptableObject/Cards.asset", "Assets/Data/Cards/Decks/Cards.asset", "6541becdf85d597418f6b0eee90a0abe"),
        new Move("Assets/ScriptableObject/Bird/Card_B 1.asset", "Assets/Data/Cards/Standard/Bird/Card_B 1.asset", "5479054bf6070594584afe24597355a9"),
        new Move("Assets/ScriptableObject/Bird/Card_B 10.asset", "Assets/Data/Cards/Standard/Bird/Card_B 10.asset", "0b2707214529e0d4ab2e3471c85a8b43"),
        new Move("Assets/ScriptableObject/Bird/Card_B 11.asset", "Assets/Data/Cards/Standard/Bird/Card_B 11.asset", "896e298c4956c094a83bcc13f0b488fb"),
        new Move("Assets/ScriptableObject/Bird/Card_B 12.asset", "Assets/Data/Cards/Standard/Bird/Card_B 12.asset", "f6e59fe33967bbd4d8ddf54328278301"),
        new Move("Assets/ScriptableObject/Bird/Card_B 13.asset", "Assets/Data/Cards/Standard/Bird/Card_B 13.asset", "e0913f9f18493c44da355c98d7b7d610"),
        new Move("Assets/ScriptableObject/Bird/Card_B 2.asset", "Assets/Data/Cards/Standard/Bird/Card_B 2.asset", "7ebb5913b6724dc4985c55a6e28be69f"),
        new Move("Assets/ScriptableObject/Bird/Card_B 3.asset", "Assets/Data/Cards/Standard/Bird/Card_B 3.asset", "7760f26d7189d1744b437b25f1c1eae9"),
        new Move("Assets/ScriptableObject/Bird/Card_B 4.asset", "Assets/Data/Cards/Standard/Bird/Card_B 4.asset", "bb2139b54552a504980ec1950c429b15"),
        new Move("Assets/ScriptableObject/Bird/Card_B 5.asset", "Assets/Data/Cards/Standard/Bird/Card_B 5.asset", "a3ed1d2395f94514eaa9a472e870976e"),
        new Move("Assets/ScriptableObject/Bird/Card_B 6.asset", "Assets/Data/Cards/Standard/Bird/Card_B 6.asset", "34c79aa39eb4b0c4eb94cff016e31e05"),
        new Move("Assets/ScriptableObject/Bird/Card_B 7.asset", "Assets/Data/Cards/Standard/Bird/Card_B 7.asset", "04ce425a7f62de545824377f1e2862a9"),
        new Move("Assets/ScriptableObject/Bird/Card_B 8.asset", "Assets/Data/Cards/Standard/Bird/Card_B 8.asset", "df223ed9ea3022344adf8aa0cb24d347"),
        new Move("Assets/ScriptableObject/Bird/Card_B 9.asset", "Assets/Data/Cards/Standard/Bird/Card_B 9.asset", "461e82c8d6778cc42b62af92b7d8e2bd"),
        new Move("Assets/ScriptableObject/Flower/Card_F 1.asset", "Assets/Data/Cards/Standard/Flower/Card_F 1.asset", "3138bd13af5adc944a6178f6497df53c"),
        new Move("Assets/ScriptableObject/Flower/Card_F 10.asset", "Assets/Data/Cards/Standard/Flower/Card_F 10.asset", "b97fb495b98753b4f8e06beda13cb637"),
        new Move("Assets/ScriptableObject/Flower/Card_F 11.asset", "Assets/Data/Cards/Standard/Flower/Card_F 11.asset", "2b4a0522ca37fe542aad8e6459949bf7"),
        new Move("Assets/ScriptableObject/Flower/Card_F 12.asset", "Assets/Data/Cards/Standard/Flower/Card_F 12.asset", "dba823f3050802440b2ebc19e2240505"),
        new Move("Assets/ScriptableObject/Flower/Card_F 13.asset", "Assets/Data/Cards/Standard/Flower/Card_F 13.asset", "6011fa6ced10af940acce0d92da817f6"),
        new Move("Assets/ScriptableObject/Flower/Card_F 2.asset", "Assets/Data/Cards/Standard/Flower/Card_F 2.asset", "4e2867b762a4e9549b619dd46e563a91"),
        new Move("Assets/ScriptableObject/Flower/Card_F 3.asset", "Assets/Data/Cards/Standard/Flower/Card_F 3.asset", "16974954ee102764f9af95c3f8cbd466"),
        new Move("Assets/ScriptableObject/Flower/Card_F 4.asset", "Assets/Data/Cards/Standard/Flower/Card_F 4.asset", "246a26906fcc0984f81327e238aee553"),
        new Move("Assets/ScriptableObject/Flower/Card_F 5.asset", "Assets/Data/Cards/Standard/Flower/Card_F 5.asset", "3a0122ca3a7ae6840896418f9d8f5397"),
        new Move("Assets/ScriptableObject/Flower/Card_F 6.asset", "Assets/Data/Cards/Standard/Flower/Card_F 6.asset", "602e4e2969fcff844a7f6a5a86bbd537"),
        new Move("Assets/ScriptableObject/Flower/Card_F 7.asset", "Assets/Data/Cards/Standard/Flower/Card_F 7.asset", "1e94a905f5ca70e458d3d54ea4975407"),
        new Move("Assets/ScriptableObject/Flower/Card_F 8.asset", "Assets/Data/Cards/Standard/Flower/Card_F 8.asset", "d66863e8345f63a4b8fcaadb2454b06c"),
        new Move("Assets/ScriptableObject/Flower/Card_F 9.asset", "Assets/Data/Cards/Standard/Flower/Card_F 9.asset", "7aed42f601256b741847db0d6f4172e7"),
        new Move("Assets/ScriptableObject/Moon/Card_M 1.asset", "Assets/Data/Cards/Standard/Moon/Card_M 1.asset", "d7faf705412993e48beb8795611fed06"),
        new Move("Assets/ScriptableObject/Moon/Card_M 10.asset", "Assets/Data/Cards/Standard/Moon/Card_M 10.asset", "9895012b43d134c44a6023648a599854"),
        new Move("Assets/ScriptableObject/Moon/Card_M 11.asset", "Assets/Data/Cards/Standard/Moon/Card_M 11.asset", "80c25e5e7bbeb344181d92764c38a770"),
        new Move("Assets/ScriptableObject/Moon/Card_M 12.asset", "Assets/Data/Cards/Standard/Moon/Card_M 12.asset", "b7f8da44b26c0c84ebb8b656883043e7"),
        new Move("Assets/ScriptableObject/Moon/Card_M 13.asset", "Assets/Data/Cards/Standard/Moon/Card_M 13.asset", "a7330bf1492c99548a5083236421ee85"),
        new Move("Assets/ScriptableObject/Moon/Card_M 2.asset", "Assets/Data/Cards/Standard/Moon/Card_M 2.asset", "337b1270f1497ba4f82c7ba8571e6ba9"),
        new Move("Assets/ScriptableObject/Moon/Card_M 3.asset", "Assets/Data/Cards/Standard/Moon/Card_M 3.asset", "5c33d376d90a4554ea041c6d8b14058a"),
        new Move("Assets/ScriptableObject/Moon/Card_M 4.asset", "Assets/Data/Cards/Standard/Moon/Card_M 4.asset", "e14eefb675dd6af4ba6cf500a8f51af7"),
        new Move("Assets/ScriptableObject/Moon/Card_M 5.asset", "Assets/Data/Cards/Standard/Moon/Card_M 5.asset", "986bc399697a5ca448e6848c1f20e493"),
        new Move("Assets/ScriptableObject/Moon/Card_M 6.asset", "Assets/Data/Cards/Standard/Moon/Card_M 6.asset", "e64d107d85242a14582fdb6bf5aad137"),
        new Move("Assets/ScriptableObject/Moon/Card_M 7.asset", "Assets/Data/Cards/Standard/Moon/Card_M 7.asset", "87f9396186fa93140ae350c8ddcd5fa5"),
        new Move("Assets/ScriptableObject/Moon/Card_M 8.asset", "Assets/Data/Cards/Standard/Moon/Card_M 8.asset", "7c8b22d80f19941499a99e870f5f7f0e"),
        new Move("Assets/ScriptableObject/Moon/Card_M 9.asset", "Assets/Data/Cards/Standard/Moon/Card_M 9.asset", "ed9dc0022cf13134e8fd2401e8d9cc09"),
        new Move("Assets/ScriptableObject/SpecialCard/sp 1.asset", "Assets/Data/Cards/Special/sp 1.asset", "42718300bc4fb57408eacde7af22e8b7"),
        new Move("Assets/ScriptableObject/SpecialCard/sp 10.asset", "Assets/Data/Cards/Special/sp 10.asset", "93bbd85a68d13154ba166046e70118fe"),
        new Move("Assets/ScriptableObject/SpecialCard/sp 11.asset", "Assets/Data/Cards/Special/sp 11.asset", "327136ae90bfec245ada420e3abf1fe0"),
        new Move("Assets/ScriptableObject/SpecialCard/sp 12.asset", "Assets/Data/Cards/Special/sp 12.asset", "4043c6ba29b51244ebf6d1ca6d6849ee"),
        new Move("Assets/ScriptableObject/SpecialCard/sp 2.asset", "Assets/Data/Cards/Special/sp 2.asset", "0bd2c67e771a54141be2a02f0e3cad21"),
        new Move("Assets/ScriptableObject/SpecialCard/sp 3.asset", "Assets/Data/Cards/Special/sp 3.asset", "ab6d102ca9490a34ca9d0a60c1bd62c6"),
        new Move("Assets/ScriptableObject/SpecialCard/sp 4.asset", "Assets/Data/Cards/Special/sp 4.asset", "11568acdc6084c44d8fb20bf1d54dbfe"),
        new Move("Assets/ScriptableObject/SpecialCard/sp 5.asset", "Assets/Data/Cards/Special/sp 5.asset", "bb40d60b7c9f59643bac80e618875011"),
        new Move("Assets/ScriptableObject/SpecialCard/sp 6.asset", "Assets/Data/Cards/Special/sp 6.asset", "9ff05326ffc160147950820b8fe0a23a"),
        new Move("Assets/ScriptableObject/SpecialCard/sp 7.asset", "Assets/Data/Cards/Special/sp 7.asset", "6a7f3e881ef53d84099b4f026debbcb0"),
        new Move("Assets/ScriptableObject/SpecialCard/sp 8.asset", "Assets/Data/Cards/Special/sp 8.asset", "1563bf00fcce24a44a3adcc5813b74bd"),
        new Move("Assets/ScriptableObject/SpecialCard/sp 9.asset", "Assets/Data/Cards/Special/sp 9.asset", "aebad18162086394f96b136e6dba819b"),
        new Move("Assets/ScriptableObject/SpecialCard/sps.asset", "Assets/Data/Cards/Special/sps.asset", "85d06bdaa52d61246abef1c91cf4fcdf"),
        new Move("Assets/ScriptableObject/SpecialCard/sp_no_use.asset", "Assets/Data/Cards/Special/sp_no_use.asset", "840487add834418e918ffce26cf05f53"),
        new Move("Assets/ScriptableObject/SpecialCard/sp_oni.asset", "Assets/Data/Cards/Special/sp_oni.asset", "d810d82037ae42d58cc80edcbcf7f406"),
        new Move("Assets/ScriptableObject/Wind/Card_W 1.asset", "Assets/Data/Cards/Standard/Wind/Card_W 1.asset", "1ea946ace796fa8489cfe3e73a2cc750"),
        new Move("Assets/ScriptableObject/Wind/Card_W 10.asset", "Assets/Data/Cards/Standard/Wind/Card_W 10.asset", "33e90f2b1af9609448cbef0cb433d268"),
        new Move("Assets/ScriptableObject/Wind/Card_W 11.asset", "Assets/Data/Cards/Standard/Wind/Card_W 11.asset", "727ce30701a77774d8345b117dfa5e06"),
        new Move("Assets/ScriptableObject/Wind/Card_W 12.asset", "Assets/Data/Cards/Standard/Wind/Card_W 12.asset", "e3df714ed7bb09744b484719f089e054"),
        new Move("Assets/ScriptableObject/Wind/Card_W 13.asset", "Assets/Data/Cards/Standard/Wind/Card_W 13.asset", "eaf55c711abfb384fa91109601c92f85"),
        new Move("Assets/ScriptableObject/Wind/Card_W 2.asset", "Assets/Data/Cards/Standard/Wind/Card_W 2.asset", "453e6ef3938f0eb4ea04b3db227a39f2"),
        new Move("Assets/ScriptableObject/Wind/Card_W 3.asset", "Assets/Data/Cards/Standard/Wind/Card_W 3.asset", "5f59a071641b7f24c8da8b88bce8eeb0"),
        new Move("Assets/ScriptableObject/Wind/Card_W 4.asset", "Assets/Data/Cards/Standard/Wind/Card_W 4.asset", "ec3a0deba962db9499b108ab9a0efc2c"),
        new Move("Assets/ScriptableObject/Wind/Card_W 5.asset", "Assets/Data/Cards/Standard/Wind/Card_W 5.asset", "9674106cecc0e094fb251b0bb59c4fed"),
        new Move("Assets/ScriptableObject/Wind/Card_W 6.asset", "Assets/Data/Cards/Standard/Wind/Card_W 6.asset", "c24eb0bf099a2b54e99af0f153454b2a"),
        new Move("Assets/ScriptableObject/Wind/Card_W 7.asset", "Assets/Data/Cards/Standard/Wind/Card_W 7.asset", "3efb5eeaf0d0a5b42986048913121f34"),
        new Move("Assets/ScriptableObject/Wind/Card_W 8.asset", "Assets/Data/Cards/Standard/Wind/Card_W 8.asset", "f69cb5aba48d3704db3e039c9748124c"),
        new Move("Assets/ScriptableObject/Wind/Card_W 9.asset", "Assets/Data/Cards/Standard/Wind/Card_W 9.asset", "049a370b4bf36dd4893b18351114e69d"),
        new Move("Assets/effect/一双エフェクト.prefab", "Assets/Prefabs/Effects/HandRoles/一双エフェクト.prefab", "279738b84b835244983abd884f24a44f"),
        new Move("Assets/effect/七光エフェクト.prefab", "Assets/Prefabs/Effects/HandRoles/七光エフェクト.prefab", "46c4df3dd67ad9d4cbe5fe2c3b894925"),
        new Move("Assets/effect/七筋エフェクト.prefab", "Assets/Prefabs/Effects/HandRoles/七筋エフェクト.prefab", "29ef6ca20a76b614d92afadaeaf61225"),
        new Move("Assets/effect/三珠エフェクト.prefab", "Assets/Prefabs/Effects/HandRoles/三珠エフェクト.prefab", "f3f627e168bc6de4e8aa2425412340e0"),
        new Move("Assets/effect/不見エフェクト.prefab", "Assets/Prefabs/Effects/HandRoles/不見エフェクト.prefab", "2b472be926c8cb241940afa896284db7"),
        new Move("Assets/effect/二双エフェクト.prefab", "Assets/Prefabs/Effects/HandRoles/二双エフェクト.prefab", "dcaa98d8167dd4d4e9e135098b742e03"),
        new Move("Assets/effect/光エフェクト.prefab", "Assets/Prefabs/Effects/HandRoles/光エフェクト.prefab", "cbdc1f37d05f0dd4e9f534379f370cc7"),
        new Move("Assets/effect/四珠エフェクト.prefab", "Assets/Prefabs/Effects/HandRoles/四珠エフェクト.prefab", "39a64d67e111571408294a152e387702"),
        new Move("Assets/effect/天守エフェクト.prefab", "Assets/Prefabs/Effects/HandRoles/天守エフェクト.prefab", "877ac0b86e651404bbc6e73765ddb7fc"),
        new Move("Assets/effect/天守閣エフェクト.prefab", "Assets/Prefabs/Effects/HandRoles/天守閣エフェクト.prefab", "1361b169691ab1d4a97f26ed0c50790a"),
        new Move("Assets/effect/筋エフェクト.prefab", "Assets/Prefabs/Effects/HandRoles/筋エフェクト.prefab", "8e8f70c1ea7e3e149a3806cc44549ee8"),
        new Move("Assets/Resources/MatchResultVisualAssets.asset", "Assets/Data/UI/MatchResultVisualAssets.asset", "1d994a9fd949486995769022d0bb8489"),
        new Move("Assets/Resources/ShowdownCutInAssets.asset", "Assets/Data/UI/ShowdownCutInAssets.asset", "c6dc7d8f5f8342db8ab97cfe7921740f"),
        new Move("Assets/Resources/coin/child_coin.png", "Assets/Sprite/production/ui/coin/child_coin.png", "38d2ee33b07604a489ad5d9b28e2331f"),
        new Move("Assets/Resources/coin/coin_background.png", "Assets/Sprite/production/ui/coin/coin_background.png", "4ae9ad3fbc64c4e4a9dafb632971c027"),
        new Move("Assets/Resources/coin/parent_coin.png", "Assets/Sprite/production/ui/coin/parent_coin.png", "ca1e991007e0c7b419a5bba00bacbc1a"),
        new Move("Assets/Resources/Prefabs/Card.prefab", "Assets/Prefabs/Cards/Card.prefab", "54329b15563792e40a3a8bc60ff6b025"),
        new Move("Assets/Resources/Prefabs/CardArea.prefab", "Assets/Prefabs/Cards/CardArea.prefab", "6379f799ab651a542940a2415dcac5c0"),
        new Move("Assets/Resources/Prefabs/Cards.prefab", "Assets/Prefabs/Cards/Cards.prefab", "37e754d6ba8817a4ca2febaaa1ffd5c1"),
        new Move("Assets/Resources/Prefabs/Player.prefab", "Assets/Prefabs/Characters/Player.prefab", "622c8cddb4b578343b941eef25ab7f6b"),
        new Move("Assets/Resources/Prefabs/TextArea.prefab", "Assets/Prefabs/UI/Shared/TextArea.prefab", "9c5cd32967b00b94eb9ff171c7a33616"),
        new Move("Assets/Resources/Prefabs/TextPanel.prefab", "Assets/Prefabs/UI/Shared/TextPanel.prefab", "02ee9d38932ecb546a43c49ef0394655"),
        new Move("Assets/Resources/SpecialCardSelection/aiko_s.png", "Assets/Sprite/production/ui/special_card_selection/aiko_s.png", "3d52d5f23b801d54d995169da90b359b"),
        new Move("Assets/Resources/SpecialCardSelection/bet_s.png", "Assets/Sprite/production/ui/special_card_selection/bet_s.png", "c6f057a5dda0b09488efb082253aa3de"),
        new Move("Assets/Resources/SpecialCardSelection/bonus_05_s.png", "Assets/Sprite/production/ui/special_card_selection/bonus_05_s.png", "015118dd46da9f2469c9e2363d7d5217"),
        new Move("Assets/Resources/SpecialCardSelection/bonus_10_s.png", "Assets/Sprite/production/ui/special_card_selection/bonus_10_s.png", "c61ade17c67d5424bb9ca49d36ab98a1"),
        new Move("Assets/Resources/SpecialCardSelection/bonus_15_s.png", "Assets/Sprite/production/ui/special_card_selection/bonus_15_s.png", "dde56c5965afd1846b3d1017e51a63d4"),
        new Move("Assets/Resources/SpecialCardSelection/curse_s.png", "Assets/Sprite/production/ui/special_card_selection/curse_s.png", "3ec4863716e4e7141adf811efca43165"),
        new Move("Assets/Resources/SpecialCardSelection/double_score_s.png", "Assets/Sprite/production/ui/special_card_selection/double_score_s.png", "a698b299ccf687f4a9b0563c1857e9f7"),
        new Move("Assets/Resources/SpecialCardSelection/festival_s.png", "Assets/Sprite/production/ui/special_card_selection/festival_s.png", "504f31f1c8fd6ce4ba07d8ab97735e22"),
        new Move("Assets/Resources/SpecialCardSelection/rain_s.png", "Assets/Sprite/production/ui/special_card_selection/rain_s.png", "952b4b00d2657eb45be114fc9e4a8f0b"),
        new Move("Assets/Resources/SpecialCardSelection/seal_s.png", "Assets/Sprite/production/ui/special_card_selection/seal_s.png", "60d633b8583089748a37f4a0a19be059"),
        new Move("Assets/Resources/SpecialCardSelection/sunny_s.png", "Assets/Sprite/production/ui/special_card_selection/sunny_s.png", "0eb8d8e4a7ebd6a46968aa5b61b146cd"),
        new Move("Assets/Resources/SpecialCardSelection/swap_s.png", "Assets/Sprite/production/ui/special_card_selection/swap_s.png", "3387c07c1d4e22f4f82afc707249ab9a"),
        new Move("Assets/Editor/CardsSceneAuthoringEditor.cs", "Assets/Editor/Authoring/CardsSceneAuthoringEditor.cs", "91e123a429ad4bc5a258f4315dba69e2"),
        new Move("Assets/Editor/UiAuthoringWindow.cs", "Assets/Editor/Authoring/UiAuthoringWindow.cs", "7f2de4425250e2e44b38fbbe8dac7406"),
        new Move("Assets/Editor/UiPrefabMigration.cs", "Assets/Editor/Migrations/UiPrefabMigration.cs", "a3b1840bec1ee6847889eefb47eb8417"),
        new Move("Assets/Editor/UiComponentMigration.cs", "Assets/Editor/Migrations/UiComponentMigration.cs", "9528ec3377f895c4e80b5d4f08592323"),
        new Move("Assets/Editor/NovemberUiAuthoring.cs", "Assets/Editor/Migrations/NovemberUiAuthoring.cs", "a2accf892b41ae142bb0aad353fd2e66"),
        new Move("Assets/Editor/UiPrefabBuildValidator.cs", "Assets/Editor/Validation/UiPrefabBuildValidator.cs", "a2c0937d2b856ce469a57692d78ee534"),
        new Move("Assets/Materials/一双Material.mat", "Assets/Materials/Effects/HandRoles/一双Material.mat", "20aa62f8f87024c42ba4259ae689427b"),
        new Move("Assets/Materials/七光Material.mat", "Assets/Materials/Effects/HandRoles/七光Material.mat", "4b2eb40763d8b704d808064e2ba50414"),
        new Move("Assets/Materials/七筋Material.mat", "Assets/Materials/Effects/HandRoles/七筋Material.mat", "50fe871d231843944ab6b46d1cf2f5b8"),
        new Move("Assets/Materials/三珠Material.mat", "Assets/Materials/Effects/HandRoles/三珠Material.mat", "2b76d0ce536c92d47ad6cb7c6012fdf0"),
        new Move("Assets/Materials/不見Material.mat", "Assets/Materials/Effects/HandRoles/不見Material.mat", "108ca3be22ed9604ba6adf0cc280f0fd"),
        new Move("Assets/Materials/二双Material.mat", "Assets/Materials/Effects/HandRoles/二双Material.mat", "75b9eb417c11c134f9f7ec553c77d87e"),
        new Move("Assets/Materials/光Material.mat", "Assets/Materials/Effects/HandRoles/光Material.mat", "bb4dd8735e00b2e419533540f4a74880"),
        new Move("Assets/Materials/四珠Material.mat", "Assets/Materials/Effects/HandRoles/四珠Material.mat", "e7ced329e4731984ab86c3a482de1820"),
        new Move("Assets/Materials/天守Material.mat", "Assets/Materials/Effects/HandRoles/天守Material.mat", "e549b173b8ea1d94bb03d5fbb7b9e6f0"),
        new Move("Assets/Materials/天守閣Material.mat", "Assets/Materials/Effects/HandRoles/天守閣Material.mat", "d02158593581f4e46a586aed534c97c1"),
        new Move("Assets/Materials/筋Material.mat", "Assets/Materials/Effects/HandRoles/筋Material.mat", "798fd16e620639f43adf53ace117201f"),
    };

    // Move folders first to preserve their GUIDs. Individual assets are then resolved by GUID.
    private static readonly Move[] Folders =
    {
        new Move("Assets/Scripts/Card", "Assets/Scripts/Gameplay/Cards"),
        new Move("Assets/Scripts/Controller", "Assets/Scripts/Gameplay/Controllers"),
        new Move("Assets/Scripts/State", "Assets/Scripts/Gameplay/State"),
        new Move("Assets/Scripts/BattleGround", "Assets/Scripts/Gameplay/BattleGround"),
        new Move("Assets/Scripts/Character", "Assets/Scripts/UI/Characters"),
        new Move("Assets/Scripts/Data", "Assets/Scripts/Infrastructure/Persistence"),
        new Move("Assets/Scripts/Option", "Assets/Scripts/Infrastructure/Settings"),
        new Move("Assets/ScriptableObject", "Assets/Data/Cards"),
        new Move("Assets/Data/Cards/Bird", "Assets/Data/Cards/Standard/Bird"),
        new Move("Assets/Data/Cards/Flower", "Assets/Data/Cards/Standard/Flower"),
        new Move("Assets/Data/Cards/Moon", "Assets/Data/Cards/Standard/Moon"),
        new Move("Assets/Data/Cards/Wind", "Assets/Data/Cards/Standard/Wind"),
        new Move("Assets/Data/Cards/SpecialCard", "Assets/Data/Cards/Special"),
        new Move("Assets/effect", "Assets/Prefabs/Effects/HandRoles"),
        new Move("Assets/Resources/coin", "Assets/Sprite/production/ui/coin"),
        new Move("Assets/Resources/SpecialCardSelection", "Assets/Sprite/production/ui/special_card_selection"),
        new Move("Assets/Resources/Prefabs", "Assets/Prefabs/Cards")
    };

    [MenuItem("Tools/Efuda/Project Folders/Preflight STR-07")]
    public static void Preflight()
    {
        RequireReady();
        if (File.Exists(RecordPath)) throw new InvalidOperationException("A migration record already exists. Validate or roll back instead of applying again.");
        foreach (Move move in Moves)
        {
            if (AssetDatabase.AssetPathToGUID(move.from) != move.guid || !File.Exists(move.from))
                throw new InvalidOperationException("Source identity changed: " + move.from);
            if (File.Exists(move.to) || Directory.Exists(move.to))
                throw new InvalidOperationException("Destination is occupied: " + move.to);
        }
        // This is the only old folder that will be merged and removed, rather than relocated.
        AssertFolderUnreferenced("Assets/Scripts/Cards");
    }

    [MenuItem("Tools/Efuda/Project Folders/Apply STR-07")]
    public static void Apply()
    {
        Preflight();
        var record = new Record { startedAt = DateTimeOffset.Now.ToString("o") };
        foreach (Move move in Moves)
            record.files.Add(new FileState { from = move.from, to = move.to, guid = move.guid,
                sourceHash = Hash(move.from), metaHash = Hash(move.from + ".meta"),
                originalText = move.from == UiMigrationSource ? File.ReadAllText(move.from) : null });
        Save(record);
        EditAssets(() =>
        {
            foreach (Move folder in Folders) Relocate(folder.from, folder.to, record);
            foreach (Move move in Moves)
            {
                string current = AssetDatabase.GUIDToAssetPath(move.guid);
                if (current != move.to) Relocate(current, move.to, record);
            }
            const string emptyFolder = "Assets/Scripts/Cards";
            if (Directory.EnumerateFileSystemEntries(emptyFolder).Any())
                throw new InvalidOperationException("Merged folder is not empty: " + emptyFolder);
            var removed = new DeletedFolder { path = emptyFolder,
                metaBase64 = Convert.ToBase64String(File.ReadAllBytes(emptyFolder + ".meta")) };
            record.deletedFolders.Add(removed);
            Save(record);
            if (!AssetDatabase.DeleteAsset(emptyFolder)) throw new IOException("Could not delete empty folder.");
        }, record);
        Validate();
        record.completed = true;
        Save(record);
    }

    [MenuItem("Tools/Efuda/Project Folders/Validate STR-07")]
    public static void Validate()
    {
        Record record = Read();
        if (record.rolledBack) throw new InvalidOperationException("Migration has been rolled back.");
        foreach (FileState file in record.files)
        {
            if (AssetDatabase.GUIDToAssetPath(file.guid) != file.to || !File.Exists(file.to))
                throw new InvalidOperationException("Moved identity mismatch: " + file.to);
            if (Hash(file.to + ".meta") != file.metaHash)
                throw new InvalidOperationException("Importer or GUID changed: " + file.to);
            if (Hash(file.to) != file.sourceHash && !(file.from == UiMigrationSource &&
                File.ReadAllText(file.to) == UpdatePaths(file.originalText)))
                throw new InvalidOperationException("Unexpected content change: " + file.to);
        }
    }

    // Used by the caller to restrict the approved Editor path update to exact replacements.
    public static string UpdatePaths(string source)
    {
        foreach (Move move in Moves) source = source.Replace(move.from, move.to);
        return source.Replace("Assets/Resources/SpecialCardSelection/", "Assets/Sprite/production/ui/special_card_selection/");
    }

    [MenuItem("Tools/Efuda/Project Folders/Roll Back STR-07")]
    public static void RollBack()
    {
        RequireReady();
        Record record = Read();
        if (record.rolledBack) throw new InvalidOperationException("Already rolled back.");
        // Refuse to overwrite any user changes made after the migration.
        foreach (FileState file in record.files)
        {
            string current = AssetDatabase.GUIDToAssetPath(file.guid);
            if (Hash(current) != file.sourceHash && !(file.from == UiMigrationSource &&
                File.ReadAllText(current) == UpdatePaths(file.originalText)))
                throw new InvalidOperationException("New changes must be reviewed before rollback: " + current);
        }
        EditAssets(() =>
        {
            foreach (DeletedFolder folder in record.deletedFolders)
            {
                EnsureFolder(folder.path, null);
                // Restore the exact backed-up folder meta, rather than assigning a new identity.
                File.WriteAllBytes(folder.path + ".meta", Convert.FromBase64String(folder.metaBase64));
                AssetDatabase.ImportAsset(folder.path);
            }
            for (int i = record.operations.Count - 1; i >= 0; --i)
            {
                Operation operation = record.operations[i];
                string current = AssetDatabase.GUIDToAssetPath(operation.guid);
                if (current != operation.to) throw new InvalidOperationException("Rollback path changed: " + current);
                EnsureFolder(Path.GetDirectoryName(operation.from).Replace('\\', '/'), null);
                MoveAsset(current, operation.from);
            }
            FileState editorFile = record.files.Single(f => f.from == UiMigrationSource);
            if (Hash(editorFile.from) != editorFile.sourceHash)
            {
                // Exact bytes remain in the implementation-backup archive; caller restores them.
                Debug.Log("Restore UiPrefabMigration.cs and path-following test edits from the STR-07 backup.");
            }
            for (int i = record.createdFolders.Count - 1; i >= 0; --i)
            {
                string path = record.createdFolders[i];
                if (Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any())
                    if (!AssetDatabase.DeleteAsset(path)) throw new IOException("Could not remove empty new folder: " + path);
            }
            record.rolledBack = true;
            Save(record);
        }, record);
    }

    private static void RequireReady()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Editor must be idle and outside Play Mode.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save the current Scene before migration.");
    }

    private static void AssertFolderUnreferenced(string path)
    {
        string guid = AssetDatabase.AssetPathToGUID(path);
        if (string.IsNullOrEmpty(guid)) throw new InvalidOperationException("Missing folder: " + path);
        var extensions = new HashSet<string> { ".asset", ".prefab", ".unity", ".json", ".cs", ".asmdef", ".asmref" };
        foreach (string root in new[] { "Assets", "Packages", "ProjectSettings" })
            foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                if (extensions.Contains(Path.GetExtension(file)) && File.ReadAllText(file).Contains(guid))
                    throw new InvalidOperationException("Folder GUID is referenced by " + file);
    }

    private static void Relocate(string from, string to, Record record)
    {
        if (string.IsNullOrEmpty(from)) throw new InvalidOperationException("Missing asset for " + to);
        EnsureFolder(Path.GetDirectoryName(to).Replace('\\', '/'), record);
        string guid = AssetDatabase.AssetPathToGUID(from);
        MoveAsset(from, to);
        record.operations.Add(new Operation { from = from, to = to, guid = guid });
        Save(record);
    }

    private static void EnsureFolder(string path, Record record)
    {
        if (path == "Assets" || AssetDatabase.IsValidFolder(path)) return;
        if (!path.StartsWith("Assets/", StringComparison.Ordinal)) throw new InvalidOperationException("Outside Assets: " + path);
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent, record);
        string guid = AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        if (string.IsNullOrEmpty(guid) || AssetDatabase.GUIDToAssetPath(guid) != path)
            throw new IOException("Could not create " + path);
        if (record != null) { record.createdFolders.Add(path); Save(record); }
    }

    private static void EditAssets(Action action, Record record)
    {
        AssetDatabase.DisallowAutoRefresh();
        EditorApplication.LockReloadAssemblies();
        try { action(); }
        catch (Exception exception) { record.error = exception.ToString(); Save(record); throw; }
        finally
        {
            EditorApplication.UnlockReloadAssemblies();
            AssetDatabase.AllowAutoRefresh();
        }
    }

    // Finish the import for each operation before creating a child under a relocated folder.
    // A single batch across nested folder moves leaves their paths unregistered in Unity 2022.
    private static void MoveAsset(string from, string to)
    {
        AssetDatabase.StartAssetEditing();
        try
        {
            string error = AssetDatabase.MoveAsset(from, to);
            if (!string.IsNullOrEmpty(error)) throw new IOException(from + " -> " + to + ": " + error);
        }
        finally { AssetDatabase.StopAssetEditing(); }
    }

    private static string Hash(string path)
    {
        using (var stream = File.OpenRead(path))
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
    }
    private static Record Read() => JsonUtility.FromJson<Record>(File.ReadAllText(RecordPath));
    private static void Save(Record record)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(RecordPath));
        File.WriteAllText(RecordPath + ".tmp", JsonUtility.ToJson(record, true));
        File.Copy(RecordPath + ".tmp", RecordPath, true);
        File.Delete(RecordPath + ".tmp");
    }
}
