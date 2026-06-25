using Sirenix.OdinInspector;
using UnityEngine;

public enum EnemySpawnSpace
{
    UI,
    World,
}

public class EnemySpawnArea : MonoBehaviour
{
    [Header("Mode")]
    public EnemySpawnSpace spawnSpace = EnemySpawnSpace.UI;

    [Header("UI Area")]
    [ShowIf("UsesUIArea")]
    public RectTransform uiArea;

    [Header("World Area")]
    [ShowIf("UsesWorldArea")]
    public Transform worldAreaCenter;
    [ShowIf("UsesWorldArea")]
    public Vector2 worldAreaSize = new Vector2(6f, 3f);

    [Header("Rows")]
    [Range(0f, 1f)]
    public float frontRowPercent = 0.25f;

    [Range(0f, 1f)]
    public float backRowPercent = 0.75f;

    [Header("Padding")]
    public Vector2 padding = new Vector2(40f, 40f);
    public Vector2 worldPadding = new Vector2(0.25f, 0.25f);

    public bool HasValidUIArea => uiArea != null;
    public bool HasValidWorldArea => worldAreaCenter != null;
    public bool HasValidArea => spawnSpace == EnemySpawnSpace.World ? HasValidWorldArea : HasValidUIArea;

    bool UsesUIArea()
    {
        return spawnSpace == EnemySpawnSpace.UI;
    }

    bool UsesWorldArea()
    {
        return spawnSpace == EnemySpawnSpace.World;
    }

    public Vector3 GetMinPoint()
    {
        return spawnSpace == EnemySpawnSpace.World
            ? GetWorldMinPoint()
            : GetUIMinPoint();
    }

    public Vector3 GetMaxPoint()
    {
        return spawnSpace == EnemySpawnSpace.World
            ? GetWorldMaxPoint()
            : GetUIMaxPoint();
    }

    public Vector3 GetPoint(float x01, float y01)
    {
        return spawnSpace == EnemySpawnSpace.World
            ? GetWorldPoint(x01, y01)
            : GetUIPoint(x01, y01);
    }

    Vector3 GetUIMinPoint()
    {
        if (uiArea == null)
            return Vector3.zero;

        Rect rect = uiArea.rect;
        return new Vector3(rect.xMin, rect.yMin, 0f);
    }

    Vector3 GetUIMaxPoint()
    {
        if (uiArea == null)
            return Vector3.zero;

        Rect rect = uiArea.rect;
        return new Vector3(rect.xMax, rect.yMax, 0f);
    }

    Vector3 GetUIPoint(float x01, float y01)
    {
        Vector3 min = GetUIMinPoint();
        Vector3 max = GetUIMaxPoint();

        float left = Mathf.Min(min.x + padding.x, max.x - padding.x);
        float right = Mathf.Max(min.x + padding.x, max.x - padding.x);
        float bottom = Mathf.Min(min.y + padding.y, max.y - padding.y);
        float top = Mathf.Max(min.y + padding.y, max.y - padding.y);

        return new Vector3(
            Mathf.Lerp(left, right, Mathf.Clamp01(x01)),
            Mathf.Lerp(bottom, top, Mathf.Clamp01(y01)),
            0f
        );
    }

    Vector3 GetWorldMinPoint()
    {
        if (worldAreaCenter == null)
            return Vector3.zero;

        Vector3 center = worldAreaCenter.position;
        Vector3 extents = new Vector3(worldAreaSize.x, worldAreaSize.y, 0f) * 0.5f;
        return center - extents;
    }

    Vector3 GetWorldMaxPoint()
    {
        if (worldAreaCenter == null)
            return Vector3.zero;

        Vector3 center = worldAreaCenter.position;
        Vector3 extents = new Vector3(worldAreaSize.x, worldAreaSize.y, 0f) * 0.5f;
        return center + extents;
    }

    Vector3 GetWorldPoint(float x01, float y01)
    {
        Vector3 min = GetWorldMinPoint();
        Vector3 max = GetWorldMaxPoint();

        float left = Mathf.Min(min.x + worldPadding.x, max.x - worldPadding.x);
        float right = Mathf.Max(min.x + worldPadding.x, max.x - worldPadding.x);
        float bottom = Mathf.Min(min.y + worldPadding.y, max.y - worldPadding.y);
        float top = Mathf.Max(min.y + worldPadding.y, max.y - worldPadding.y);

        return new Vector3(
            Mathf.Lerp(left, right, Mathf.Clamp01(x01)),
            Mathf.Lerp(bottom, top, Mathf.Clamp01(y01)),
            worldAreaCenter != null ? worldAreaCenter.position.z : 0f
        );
    }

    public Vector3 GetFrontRowPoint(int index, int count)
    {
        float x01 = count <= 1 ? 0.5f : (float)index / (count - 1);
        return GetPoint(x01, frontRowPercent);
    }

    public Vector3 GetBackRowPoint(int index, int count)
    {
        float x01 = count <= 1 ? 0.5f : (float)index / (count - 1);
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

        Gizmos.color = Color.cyan;

        if (spawnSpace == EnemySpawnSpace.World)
        {
            Vector3 min = GetWorldMinPoint();
            Vector3 max = GetWorldMaxPoint();
            Gizmos.DrawWireCube((min + max) * 0.5f, max - min);
            return;
        }

        Vector3 minUI = GetUIMinPoint();
        Vector3 maxUI = GetUIMaxPoint();
        Vector3 p0 = uiArea.TransformPoint(new Vector3(minUI.x, minUI.y, 0f));
        Vector3 p1 = uiArea.TransformPoint(new Vector3(maxUI.x, minUI.y, 0f));
        Vector3 p2 = uiArea.TransformPoint(new Vector3(maxUI.x, maxUI.y, 0f));
        Vector3 p3 = uiArea.TransformPoint(new Vector3(minUI.x, maxUI.y, 0f));

        Gizmos.DrawLine(p0, p1);
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p0);
    }
}
