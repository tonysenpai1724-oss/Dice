using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BoardService
{
    [Header("Board Settings")]
    [SerializeField] private Collider boardCollider;
    [SerializeField] private float spawnPadding = 4f;
    [SerializeField] private float sideSpawnPercent = 0.9f;
    [SerializeField] private float topSpawnPercent = 0.6f;
    [SerializeField] private float startSpawnGridJitter = 0.35f;
    [SerializeField] private int spawnSearchSteps = 18;
    [SerializeField] private float spawnSearchRadiusStep = 0.6f;

    // Constructor
    public BoardService(Collider boardCollider)
    {
        this.boardCollider = boardCollider;
    }

    // Properties
    public Collider BoardCollider
    {
        get => boardCollider;
        set => boardCollider = value;
    }

    public float SpawnPadding => spawnPadding;
    public float SideSpawnPercent => sideSpawnPercent;
    public float TopSpawnPercent => topSpawnPercent;

    public Bounds GetBoardBounds()
    {
        if (boardCollider == null)
            return new Bounds(Vector3.zero, Vector3.one);
        return boardCollider.bounds;
    }

    public float GetBoardSurfaceY()
    {
        if (boardCollider == null)
            return 0.5f;
        return boardCollider.bounds.max.y + 1.5f;
    }

    public Vector3 GetRandomPositionOnBoard()
    {
        if (boardCollider == null)
            return Vector3.zero;

        Bounds b = boardCollider.bounds;
        float boardY = GetBoardSurfaceY();
        float sideMarginPercent = (1f - sideSpawnPercent) * 0.5f;

        float minX = Mathf.Lerp(b.min.x + spawnPadding, b.max.x - spawnPadding, sideMarginPercent);
        float maxX = Mathf.Lerp(b.min.x + spawnPadding, b.max.x - spawnPadding, 1f - sideMarginPercent);
        float minZ = Mathf.Lerp(b.min.z + spawnPadding, b.max.z - spawnPadding, 1f - topSpawnPercent);
        float maxZ = Mathf.Max(b.min.z + spawnPadding, b.max.z - spawnPadding);

        return new Vector3(
            Random.Range(minX, maxX),
            boardY,
            Random.Range(minZ, maxZ)
        );
    }

    public List<Vector3> BuildSpreadSpawnPositions(int targetSpawnCount)
    {
        List<Vector3> result = new List<Vector3>();
        if (targetSpawnCount <= 0 || boardCollider == null)
            return result;

        Bounds b = boardCollider.bounds;
        float boardY = GetBoardSurfaceY();
        float sideMarginPercent = (1f - sideSpawnPercent) * 0.5f;

        float minX = Mathf.Lerp(b.min.x + spawnPadding, b.max.x - spawnPadding, sideMarginPercent);
        float maxX = Mathf.Lerp(b.min.x + spawnPadding, b.max.x - spawnPadding, 1f - sideMarginPercent);
        float minZ = Mathf.Lerp(b.min.z + spawnPadding, b.max.z - spawnPadding, 1f - topSpawnPercent);
        float maxZ = Mathf.Max(b.min.z + spawnPadding, b.max.z - spawnPadding);

        int columns = Mathf.CeilToInt(Mathf.Sqrt(targetSpawnCount));
        int rows = Mathf.CeilToInt(targetSpawnCount / (float)columns);

        float width = Mathf.Max(0.1f, maxX - minX);
        float depth = Mathf.Max(0.1f, maxZ - minZ);
        float cellWidth = width / columns;
        float cellDepth = depth / rows;

        List<Vector2Int> cells = new List<Vector2Int>();
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                cells.Add(new Vector2Int(col, row));
            }
        }

        // Shuffle cells
        for (int i = 0; i < cells.Count; i++)
        {
            int swapIndex = Random.Range(i, cells.Count);
            Vector2Int temp = cells[i];
            cells[i] = cells[swapIndex];
            cells[swapIndex] = temp;
        }

        int count = Mathf.Min(targetSpawnCount, cells.Count);
        for (int i = 0; i < count; i++)
        {
            Vector2Int cell = cells[i];
            float centerX = minX + (cell.x + 0.5f) * cellWidth;
            float centerZ = minZ + (cell.y + 0.5f) * cellDepth;

            float jitterX = Random.Range(-cellWidth * startSpawnGridJitter, cellWidth * startSpawnGridJitter);
            float jitterZ = Random.Range(-cellDepth * startSpawnGridJitter, cellDepth * startSpawnGridJitter);

            Vector3 candidate = new Vector3(
                Mathf.Clamp(centerX + jitterX, minX, maxX),
                boardY,
                Mathf.Clamp(centerZ + jitterZ, minZ, maxZ)
            );

            result.Add(candidate);
        }

        return result;
    }

    public bool IsOccupied(Vector3 position, Dice ignore = null, float radius = 1f)
    {
        Vector3 halfExtents = new Vector3(radius, 0.45f, radius);

        Collider[] hits = Physics.OverlapBox(
            position,
            halfExtents,
            Quaternion.identity
        );

        foreach (Collider hit in hits)
        {
            Dice d = hit.GetComponent<Dice>();
            if (d == null)
                continue;
            if (d == ignore)
                continue;
            if (!d.gameObject.activeInHierarchy)
                continue;
            return true;
        }

        return false;
    }

    public Vector3 FindClearPosition(Vector3 center, Dice ignore = null, float radius = 1f)
    {
        center.y = GetBoardSurfaceY();

        if (!IsOccupied(center, ignore, radius))
            return center;

        if (boardCollider == null)
            return center;

        Bounds b = boardCollider.bounds;

        for (int ring = 1; ring <= spawnSearchSteps; ring++)
        {
            float searchRadius = ring * spawnSearchRadiusStep;

            for (int i = 0; i < 16; i++)
            {
                float angle = i / 16f * Mathf.PI * 2f;
                Vector3 candidate = center + new Vector3(Mathf.Cos(angle) * searchRadius, 0f, Mathf.Sin(angle) * searchRadius);

                candidate.x = Mathf.Clamp(candidate.x, b.min.x + spawnPadding, b.max.x - spawnPadding);
                candidate.z = Mathf.Clamp(candidate.z, b.min.z + spawnPadding, b.max.z - spawnPadding);
                candidate.y = GetBoardSurfaceY();

                if (!IsOccupied(candidate, ignore, radius))
                    return candidate;
            }
        }

        return center;
    }

    public Vector3 FindRandomClearPositionWithinRadius(Vector3 origin, float maxRadius, Dice ignore = null, float radius = 1f)
    {
        origin.y = GetBoardSurfaceY();

        if (boardCollider == null)
            return origin;

        Bounds b = boardCollider.bounds;
        Vector3 fallback = origin;
        float bestScore = float.MinValue;

        for (int i = 0; i < 24; i++)
        {
            Vector2 circle = Random.insideUnitCircle.normalized * Random.Range(0.4f, 1f);

            if (circle.sqrMagnitude < 0.001f)
                continue;

            Vector3 candidate = origin + new Vector3(circle.x, 0f, circle.y) * maxRadius;

            candidate.x = Mathf.Clamp(candidate.x, b.min.x + spawnPadding, b.max.x - spawnPadding);
            candidate.z = Mathf.Clamp(candidate.z, b.min.z + spawnPadding, b.max.z - spawnPadding);
            candidate.y = GetBoardSurfaceY();

            if (!IsOccupied(candidate, ignore, radius))
                return candidate;

            float score = (candidate - origin).sqrMagnitude;
            if (score > bestScore)
            {
                bestScore = score;
                fallback = candidate;
            }
        }

        return fallback;
    }

    public bool IsPositionOnBoard(Vector3 position)
    {
        if (boardCollider == null)
            return false;

        Bounds b = boardCollider.bounds;
        float margin = 0.5f;

        return position.x >= b.min.x + margin &&
               position.x <= b.max.x - margin &&
               position.z >= b.min.z + margin &&
               position.z <= b.max.z - margin;
    }

    public Vector3 ClampToBoard(Vector3 position)
    {
        if (boardCollider == null)
            return position;

        Bounds b = boardCollider.bounds;
        position.x = Mathf.Clamp(position.x, b.min.x + spawnPadding, b.max.x - spawnPadding);
        position.z = Mathf.Clamp(position.z, b.min.z + spawnPadding, b.max.z - spawnPadding);
        position.y = GetBoardSurfaceY();

        return position;
    }

    public Vector3 GetCenterOfBoard()
    {
        if (boardCollider == null)
            return Vector3.zero;

        Bounds b = boardCollider.bounds;
        return new Vector3(b.center.x, GetBoardSurfaceY(), b.center.z);
    }
}