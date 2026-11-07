using UnityEngine;

/// <summary>An editor-authored card destination. Position, size and order belong to this transform.</summary>
[DisallowMultipleComponent]
public sealed class UiCardSlot : MonoBehaviour
{
    [SerializeField, Min(0)] private int index;
    [SerializeField] private bool fitCardInside = true;

    public int Index => index;
    public RectTransform RectTransform => transform as RectTransform;
    public bool FitCardInside => fitCardInside;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        RectTransform rect = RectTransform;
        if (rect == null) return;
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.8f);
        for (int i = 0; i < 4; i++) Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
        UnityEditor.Handles.Label(rect.TransformPoint(rect.rect.center), (index + 1).ToString());
    }
#endif
}
