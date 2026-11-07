using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Renders the HUD through authored references; owns no match state.</summary>
internal sealed class GameplayHudView
{
    private readonly MonoBehaviour owner;
    private readonly TextMeshProUGUI round, playerLife, cpuLife, streak;
    private readonly PlayerRole role;
    private readonly PlayerRemainTrashCount remaining;
    private readonly Image playerMarker, cpuMarker;
    private readonly GameObject ikkiView, streakView;

    public GameplayHudView(MonoBehaviour owner, TextMeshProUGUI round, PlayerRole role,
        TextMeshProUGUI playerLife, TextMeshProUGUI cpuLife, PlayerRemainTrashCount remaining,
        Image playerMarker, Image cpuMarker, GameObject ikkiView, GameObject streakView, TextMeshProUGUI streak)
    {
        this.owner = owner;
        this.round = round;
        this.role = role;
        this.playerLife = playerLife;
        this.cpuLife = cpuLife;
        this.remaining = remaining;
        this.playerMarker = playerMarker;
        this.cpuMarker = cpuMarker;
        this.ikkiView = ikkiView;
        this.streakView = streakView;
        this.streak = streak;
    }

    public void Render(GameUiSnapshot snapshot, bool battleGround, int wins, float duration)
    {
        SetMode(battleGround);
        if (battleGround) SetStreak(wins);
        else round.text = "第" + snapshot.RoundNumber.ToString() + "局";
        playerLife.text = snapshot.PlayerLifePoints.ToString();
        cpuLife.text = snapshot.CpuLifePoints.ToString();
        role.SetRole(snapshot.PlayerRoleName, duration);
        remaining.UpdateRemainTrashCount(snapshot.RemainingTrashTurns);
    }

    public void SetMode(bool battleGround)
    {
        if (ikkiView != null) ikkiView.SetActive(!battleGround);
        if (streakView != null) streakView.SetActive(battleGround);
    }

    public void SetStreak(int wins)
    {
        if (streak != null) streak.text = Mathf.Max(0, wins) + "人抜き";
    }

    public void SetParent(int parentIndex)
    {
        if (playerMarker == null || cpuMarker == null)
            Debug.LogError("UIManager: parent markers are not assigned in the scene.", owner);
        if (playerMarker != null) playerMarker.gameObject.SetActive(parentIndex == 0);
        if (cpuMarker != null) cpuMarker.gameObject.SetActive(parentIndex == 1);
    }
}
