using Sirenix.OdinInspector;
using UnityEngine;

public class EnemySpawnArea : MonoBehaviour
{
    [Header("UI Area")]
    public RectTransform uiArea;

    [Header("Rows")]
    [Range(0f, 1f)]
    public float frontRowPercent = 0.25f;

    [Range(0f, 1f)]
    public float backRowPercent = 0.75f;

    [Header("Padding")]
    public Vector2 padding = new Vector2(40f, 40f);

    public bool HasValidUIArea =>
        uiArea != null;

    public bool HasValidArea =>
        HasValidUIArea;

    public Vector3 GetMinPoint()
    {
        if (uiArea == null)
            return Vector3.zero;

        Rect rect = uiArea.rect;
        return new Vector3(rect.xMin, rect.yMin, 0f);
    }

    public Vector3 GetMaxPoint()
    {
        if (uiArea == null)
            return Vector3.zero;

        Rect rect = uiArea.rect;
        return new Vector3(rect.xMax, rect.yMax, 0f);
    }

    public Vector3 GetPoint(float x01, float y01)
    {
        Vector3 min = GetMinPoint();
        Vector3 max = GetMaxPoint();

        float left =
            Mathf.Min(
                min.x + padding.x,
                max.x - padding.x
            );

        float right =
            Mathf.Max(
                min.x + padding.x,
                max.x - padding.x
            );

        float bottom =
            Mathf.Min(
                min.y + padding.y,
                max.y - padding.y
            );

        float top =
            Mathf.Max(
                min.y + padding.y,
                max.y - padding.y
            );

        float x =
            Mathf.Lerp(
                left,
                right,
                Mathf.Clamp01(x01)
            );

        float y =
            Mathf.Lerp(
                bottom,
                top,
                Mathf.Clamp01(y01)
            );

        return new Vector3(x, y, 0f);
    }

    public Vector3 GetFrontRowPoint(int index, int count)
    {
        float x01 =
            count <= 1
                ? 0.5f
                : (float)index / (count - 1);

        return GetPoint(x01, frontRowPercent);
    }

    public Vector3 GetBackRowPoint(int index, int count)
    {
        float x01 =
            count <= 1
                ? 0.5f
                : (float)index / (count - 1);

        return GetPoint(x01, backRowPercent);
    }

    public Vector3 GetRandomFrontRowPoint()
    {
        return GetPoint(
            Random.Range(0.08f, 0.92f),
            Random.Range(
                Mathf.Max(0f, frontRowPercent - 0.12f),
                Mathf.Min(1f, frontRowPercent + 0.12f)
            )
        );
    }

    public Vector3 GetRandomBackRowPoint()
    {
        return GetPoint(
            Random.Range(0.08f, 0.92f),
            Random.Range(
                Mathf.Max(0f, backRowPercent - 0.12f),
                Mathf.Min(1f, backRowPercent + 0.12f)
            )
        );
    }

    void OnDrawGizmosSelected()
    {
        if (!HasValidArea)
            return;

        Vector3 min = GetMinPoint();
        Vector3 max = GetMaxPoint();

        Vector3 p0 = uiArea.TransformPoint(new Vector3(min.x, min.y, 0f));
        Vector3 p1 = uiArea.TransformPoint(new Vector3(max.x, min.y, 0f));
        Vector3 p2 = uiArea.TransformPoint(new Vector3(max.x, max.y, 0f));
        Vector3 p3 = uiArea.TransformPoint(new Vector3(min.x, max.y, 0f));

        Gizmos.color = Color.cyan;

        Gizmos.DrawLine(p0, p1);
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p0);
    }
}
