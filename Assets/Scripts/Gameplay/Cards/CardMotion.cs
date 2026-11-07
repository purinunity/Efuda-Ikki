using System;
using System.Collections;
using UnityEngine;

/// <summary>Runs one card's motion within its owning component's lifetime.</summary>
internal sealed class CardMotion
{
    private const float PositionTolerance = 0.1f;
    private readonly MonoBehaviour owner;
    private readonly Action<bool> setComplete;
    private readonly Action synchronize;
    private Coroutine routine;

    public CardMotion(MonoBehaviour owner, Action<bool> setComplete, Action synchronize)
    {
        this.owner = owner;
        this.setComplete = setComplete;
        this.synchronize = synchronize;
    }

    public void Start(IEnumerator motion)
    {
        if (!owner.isActiveAndEnabled || motion == null)
        {
            setComplete(true);
            return;
        }
        Stop(false);
        setComplete(false);
        routine = owner.StartCoroutine(Run(motion));
    }

    public void Stop(bool markComplete)
    {
        if (routine != null)
        {
            owner.StopCoroutine(routine);
            routine = null;
        }
        setComplete(markComplete);
    }

    private IEnumerator Run(IEnumerator motion)
    {
        yield return motion;
        synchronize();
        setComplete(true);
        routine = null;
    }

    public static IEnumerator Move(RectTransform rect, Vector2 target, float duration)
    {
        Vector2 start = rect.anchoredPosition;
        if (duration <= 0f || Vector2.Distance(start, target) <= PositionTolerance)
        {
            rect.anchoredPosition = target;
            yield break;
        }
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rect.anchoredPosition = Vector2.Lerp(start, target, 1f - Mathf.Pow(1f - t, 3f));
            yield return null;
        }
        rect.anchoredPosition = target;
    }

    public static IEnumerator MoveAlongArc(RectTransform rect, Vector2 target, float duration, float lift)
    {
        if (rect == null) yield break;
        Vector2 start = rect.anchoredPosition;
        float distance = Vector2.Distance(start, target);
        if (duration <= 0f || distance <= PositionTolerance)
        {
            rect.anchoredPosition = target;
            yield break;
        }
        float requestedLift = Mathf.Max(0f, lift);
        float effectiveLift = requestedLift <= 0f ? 0f : Mathf.Min(requestedLift, distance * 0.8f + 8f);
        Vector2 control = (start + target) * 0.5f + Vector2.up * effectiveLift;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
            rect.anchoredPosition = Vector2.Lerp(Vector2.Lerp(start, control, t), Vector2.Lerp(control, target, t), t);
            yield return null;
        }
        rect.anchoredPosition = target;
    }
}
