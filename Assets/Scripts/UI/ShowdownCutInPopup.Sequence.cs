using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class ShowdownCutInPopup
{
    public IEnumerator Play(Data data)
    {
        Initialize();
        transform.SetAsLastSibling();
        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        closeRequested = false;

        ShowBaseResult(data);
        yield return AnimateRoleFrameIn();
        yield return WaitForAdvanceInput();

        if (HasEffectSteps(data))
        {
            yield return ShowSpecialCall();
            yield return WaitForAdvanceInput();

            foreach (Data.EffectStepData step in data.EffectSteps)
            {
                yield return ShowEffectStep(step);
                yield return WaitForAdvanceInput();
            }
        }

        yield return ShowFinalResult(data);
        if (data != null && data.IsMatchDecided)
        {
            HideImmediately();
            yield break;
        }

        yield return new WaitUntil(() => closeRequested);

        HideImmediately();
    }

    private static bool HasEffectSteps(Data data)
    {
        return data != null && data.EffectSteps != null && data.EffectSteps.Count > 0;
    }

    private IEnumerator WaitForAdvanceInput()
    {
        yield return null;

        while (Input.GetMouseButton(0) || Input.touchCount > 0)
        {
            yield return null;
        }

        while (!IsAdvanceInputDown())
        {
            yield return null;
        }
    }

    private static bool IsAdvanceInputDown()
    {
        if (Input.GetMouseButtonDown(0) ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            return true;
        }

        if (Input.touchCount <= 0)
        {
            return false;
        }

        Touch touch = Input.GetTouch(0);
        return touch.phase == TouchPhase.Began;
    }
    private void ShowBaseResult(Data data)
    {
        SetBackground(assetSet != null ? assetSet.cutInBackground : null, new Color(0.82f, 0.82f, 0.82f, 1f));
        SetCharacters(data);
        SetCardImages(playerCardImages, data.PlayerCardSprites, data.PlayerCardHighlights);
        SetCardImages(cpuCardImages, data.CpuCardSprites, data.CpuCardHighlights);
        SetImage(playerSpecialCardImage, data.PlayerSpecialCardSprite);
        SetImage(cpuSpecialCardImage, data.CpuSpecialCardSprite);
        ResetSpecialCardHighlights();
        SetRole(playerRoleImage, null, true, data.PlayerBaseRoleName, data.PlayerBaseRoleRank);
        SetRole(cpuRoleImage, null, false, data.CpuBaseRoleName, data.CpuBaseRoleRank);
        SetScoresImmediately(data.PlayerBaseScore, data.CpuBaseScore);
        SetActive(specialActivationImage, false);
        SetActive(specialCallBackdropImage, false);
        SetActive(resultBackdropImage, false);
        SetActive(resultStampImage, false);
        SetActive(cpuResultStampImage, false);
        specialCallText.gameObject.SetActive(false);
        resultText.gameObject.SetActive(false);
        damageText.gameObject.SetActive(false);
        SetTextActive(playerLifeDeductionText, false);
        SetTextActive(cpuLifeDeductionText, false);
        closeButton.gameObject.SetActive(false);
    }

    private IEnumerator ShowSpecialCall()
    {
        Sprite activationSprite = assetSet != null ? assetSet.specialActivation : null;
        SetImage(specialActivationImage, activationSprite);
        SetActive(specialCallBackdropImage, false);
        SetActive(resultBackdropImage, false);
        SetActive(resultStampImage, false);
        SetActive(cpuResultStampImage, false);
        ResetSpecialCardHighlights();
        specialCallText.gameObject.SetActive(false);
        resultText.gameObject.SetActive(false);
        damageText.gameObject.SetActive(false);
        SetTextActive(playerLifeDeductionText, false);
        SetTextActive(cpuLifeDeductionText, false);
        closeButton.gameObject.SetActive(false);

        if (specialActivationImage == null || activationSprite == null)
        {
            yield break;
        }

        Color visibleColor = Color.white;
        specialActivationImage.color = new Color(
            visibleColor.r,
            visibleColor.g,
            visibleColor.b,
            0f);

        if (specialActivationFadeDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < specialActivationFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(elapsed / specialActivationFadeDuration));
                specialActivationImage.color = new Color(
                    visibleColor.r,
                    visibleColor.g,
                    visibleColor.b,
                    alpha);
                yield return null;
            }
        }

        specialActivationImage.color = visibleColor;
    }

    private IEnumerator ShowEffectStep(Data.EffectStepData step)
    {
        if (step == null)
        {
            yield break;
        }

        SetRole(playerRoleImage, null, true, step.PlayerRoleName, step.PlayerRoleRank);
        SetRole(cpuRoleImage, null, false, step.CpuRoleName, step.CpuRoleRank);
        HighlightSpecialCard(step.OwnerPlayerId);

        SetActive(specialActivationImage, false);
        specialCallText.text = $"{GetOwnerName(step.OwnerPlayerId)}の{step.EffectName}";
        SetActive(specialCallBackdropImage, true);
        SetActive(resultBackdropImage, false);
        SetActive(resultStampImage, false);
        SetActive(cpuResultStampImage, false);
        specialCallText.gameObject.SetActive(true);
        resultText.gameObject.SetActive(false);
        damageText.gameObject.SetActive(false);
        SetTextActive(playerLifeDeductionText, false);
        SetTextActive(cpuLifeDeductionText, false);
        closeButton.gameObject.SetActive(false);
        yield return AnimateScoresTo(step.PlayerScore, step.CpuScore);
    }

    private IEnumerator ShowFinalResult(Data data)
    {
        SetBackground(assetSet != null ? assetSet.cutInBackground : null, Color.black);
        SetRole(playerRoleImage, null, true, data.PlayerFinalRoleName, data.PlayerFinalRoleRank);
        SetRole(cpuRoleImage, null, false, data.CpuFinalRoleName, data.CpuFinalRoleRank);
        ResetSpecialCardHighlights();
        SetActive(specialActivationImage, false);
        SetActive(specialCallBackdropImage, false);
        SetActive(resultBackdropImage, false);
        SetImage(resultStampImage, GetPlayerResultStampSprite(data));
        SetImage(cpuResultStampImage, GetCpuResultStampSprite(data));
        specialCallText.gameObject.SetActive(false);
        resultText.gameObject.SetActive(false);
        damageText.gameObject.SetActive(false);
        SetTextActive(playerLifeDeductionText, false);
        SetTextActive(cpuLifeDeductionText, false);
        closeButton.gameObject.SetActive(false);
        yield return AnimateScoresTo(data.PlayerFinalScore, data.CpuFinalScore);
        yield return ShowLifeDeduction(data);
        if (data.IsMatchDecided)
        {
            yield break;
        }

        ConfigureCloseButtonVisual(false);
        closeButton.gameObject.SetActive(true);
        closeButton.Select();
    }

    private IEnumerator AnimateRoleFrameIn()
    {
        RectTransform cpuRect = GetActiveRect(cpuRoleImage);
        RectTransform playerRect = GetActiveRect(playerRoleImage);
        if (cpuRect == null && playerRect == null)
        {
            yield break;
        }

        Vector2 cpuEnd = cpuRect != null ? cpuRect.anchoredPosition : Vector2.zero;
        Vector2 playerEnd = playerRect != null ? playerRect.anchoredPosition : Vector2.zero;
        float slideDistance = GetStageSlideDistance();
        Vector2 cpuStart = cpuEnd + Vector2.right * slideDistance;
        Vector2 playerStart = playerEnd + Vector2.left * slideDistance;

        if (roleFrameInDuration <= 0f)
        {
            SetAnchoredPosition(cpuRect, cpuEnd);
            SetAnchoredPosition(playerRect, playerEnd);
            yield break;
        }

        SetAnchoredPosition(cpuRect, cpuStart);
        SetAnchoredPosition(playerRect, playerStart);

        float elapsed = 0f;
        while (elapsed < roleFrameInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / roleFrameInDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            SetAnchoredPosition(cpuRect, Vector2.LerpUnclamped(cpuStart, cpuEnd, t));
            SetAnchoredPosition(playerRect, Vector2.LerpUnclamped(playerStart, playerEnd, t));
            yield return null;
        }

        SetAnchoredPosition(cpuRect, cpuEnd);
        SetAnchoredPosition(playerRect, playerEnd);
    }

    private float GetStageSlideDistance()
    {
        if (stage != null && stage.rect.width > 0f)
        {
            return stage.rect.width;
        }

        return ReferenceWidth;
    }

    private void SetScoresImmediately(int playerScore, int cpuScore)
    {
        currentPlayerScore = playerScore;
        currentCpuScore = cpuScore;
        SetScoreText(playerScoreText, currentPlayerScore);
        SetScoreText(cpuScoreText, currentCpuScore);
    }

    private IEnumerator AnimateScoresTo(int targetPlayerScore, int targetCpuScore)
    {
        if (currentPlayerScore == targetPlayerScore && currentCpuScore == targetCpuScore)
        {
            yield break;
        }

        WaitForSecondsRealtime wait = scoreStepInterval > 0f
            ? new WaitForSecondsRealtime(scoreStepInterval)
            : null;

        while (currentPlayerScore != targetPlayerScore || currentCpuScore != targetCpuScore)
        {
            currentPlayerScore = MoveScoreOneStep(currentPlayerScore, targetPlayerScore);
            currentCpuScore = MoveScoreOneStep(currentCpuScore, targetCpuScore);
            SetScoreText(playerScoreText, currentPlayerScore);
            SetScoreText(cpuScoreText, currentCpuScore);

            if (wait != null)
            {
                yield return wait;
            }
            else
            {
                yield return null;
            }
        }
    }

    private static int MoveScoreOneStep(int current, int target)
    {
        if (current < target)
        {
            return current + 1;
        }

        if (current > target)
        {
            return current - 1;
        }

        return current;
    }

    private static void SetScoreText(TextMeshProUGUI text, int score)
    {
        if (text != null)
        {
            text.text = $"{score}点";
        }
    }

    private IEnumerator ShowLifeDeduction(Data data)
    {
        if (data == null || data.IsDraw || data.Damage <= 0)
        {
            yield break;
        }

        TextMeshProUGUI deductionText =
            data.WinnerIndex == 0 ? cpuLifeDeductionText : playerLifeDeductionText;
        if (deductionText == null)
        {
            yield break;
        }

        deductionText.text = $"-{data.Damage}点";
        SetTextActive(deductionText, true);

        RectTransform deductionRect = deductionText.rectTransform;
        Vector3 startScale = Vector3.one * 0.72f;
        Color visibleColor = deductionText.color;
        SetLocalScale(deductionRect, startScale);
        SetAlpha(deductionText, 0f);

        if (lifeDeductionInDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < lifeDeductionInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(elapsed / lifeDeductionInDuration));
                SetLocalScale(
                    deductionRect,
                    Vector3.LerpUnclamped(startScale, Vector3.one, t));
                SetAlpha(deductionText, visibleColor.a * t);
                yield return null;
            }
        }

        SetLocalScale(deductionRect, Vector3.one);
        deductionText.color = visibleColor;
    }

    private static string GetOwnerName(int ownerPlayerId)
    {
        return ownerPlayerId == 0 ? "プレイヤー" : "CPU";
    }

    private Sprite GetPlayerResultStampSprite(Data data)
    {
        if (assetSet == null || data == null)
        {
            return null;
        }

        if (data.IsDraw)
        {
            return assetSet.drawResult;
        }

        return data.WinnerIndex == 0 ? assetSet.winResult : assetSet.loseResult;
    }

    private Sprite GetCpuResultStampSprite(Data data)
    {
        if (assetSet == null || data == null)
        {
            return null;
        }

        if (data.IsDraw)
        {
            return assetSet.drawResult;
        }

        return data.WinnerIndex == 1 ? assetSet.winResult : assetSet.loseResult;
    }

}
