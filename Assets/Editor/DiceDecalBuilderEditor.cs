using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[CustomEditor(typeof(Dice))]
public class DiceDecalBuilderEditor : Editor
{
    const string AutoGroupName = "Auto Decals";

    static bool clearExisting = true;
    static bool assignToDiceLists = true;
    static float facePadding = 1.08f;
    static float surfaceOffset = 0.03f;
    static float projectionDepth = 0.45f;
    static bool useInscribedFit = true;
    static float meshInset = 0.82f;
    static float angleFadeStart = 12f;
    static float angleFadeEnd = 28f;
    static float normalDotTolerance = 0.985f;
    static float planeDistanceTolerance = 0.025f;
    static float manualFaceSize = 0.7f;
    static float manualFaceOffset = 0.53f;
    static float manualFaceLayerOffset = 0.0025f;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Dice Decal Builder", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Build Mesh Face Decals tao mesh bam dung tung mat. Hay dung material render duoc tren MeshRenderer, vi URP Decal shader rieng cho DecalProjector co the khong hien tren mesh.",
            MessageType.Info);

        clearExisting = EditorGUILayout.Toggle("Clear Existing Auto Decals", clearExisting);
        assignToDiceLists = EditorGUILayout.Toggle("Assign To Dice Lists", assignToDiceLists);
        useInscribedFit = EditorGUILayout.Toggle("Use Safe Inner Fit", useInscribedFit);
        meshInset = EditorGUILayout.Slider("Mesh Decal Inset", meshInset, 0.4f, 1f);
        facePadding = EditorGUILayout.Slider("Face Padding", facePadding, 0.75f, 1.5f);
        surfaceOffset = EditorGUILayout.Slider("Surface Offset", surfaceOffset, 0f, 0.25f);
        projectionDepth = EditorGUILayout.Slider("Projection Depth", projectionDepth, 0.05f, 2f);
        angleFadeStart = EditorGUILayout.Slider("Angle Fade Start", angleFadeStart, 0f, 180f);
        angleFadeEnd = EditorGUILayout.Slider("Angle Fade End", angleFadeEnd, 0f, 180f);
        normalDotTolerance = EditorGUILayout.Slider("Normal Group Tolerance", normalDotTolerance, 0.9f, 0.999f);
        planeDistanceTolerance = EditorGUILayout.Slider("Plane Group Tolerance", planeDistanceTolerance, 0.001f, 0.2f);
        manualFaceSize = EditorGUILayout.Slider("Manual Face Size", manualFaceSize, 0.1f, 2f);
        manualFaceOffset = EditorGUILayout.Slider("Manual Face Offset", manualFaceOffset, 0.01f, 2f);
        manualFaceLayerOffset = EditorGUILayout.Slider("Manual Layer Offset", manualFaceLayerOffset, 0f, 0.05f);

        Dice dice = (Dice)target;

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Build Mesh Face Decals"))
                BuildMeshFaceDecals(dice);

            if (GUILayout.Button("Build Face Decals"))
                BuildFaceDecals(dice);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Build Manual 6 Faces"))
                BuildManualFaceDecals(dice);

            if (GUILayout.Button("Collect Auto Decals"))
                CollectAutoDecals(dice);
        }

        if (GUILayout.Button("Clear Auto Decals"))
            ClearAutoDecals(dice);
    }


    static void BuildManualFaceDecals(Dice dice)
    {
        if (dice == null)
            return;

        if (clearExisting)
            ClearAutoDecals(dice);

        Transform group = GetOrCreateAutoGroup(dice.transform);
        List<MeshRenderer> primary = new List<MeshRenderer>();
        List<MeshRenderer> secondary = new List<MeshRenderer>();

        BuildManualFace(group, dice, "Front", Vector3.forward, Quaternion.identity, primary, secondary);
        BuildManualFace(group, dice, "Back", Vector3.back, Quaternion.Euler(0f, 180f, 0f), primary, secondary);
        BuildManualFace(group, dice, "Right", Vector3.right, Quaternion.Euler(0f, 90f, 0f), primary, secondary);
        BuildManualFace(group, dice, "Left", Vector3.left, Quaternion.Euler(0f, -90f, 0f), primary, secondary);
        BuildManualFace(group, dice, "Top", Vector3.up, Quaternion.Euler(-90f, 0f, 0f), primary, secondary);
        BuildManualFace(group, dice, "Bottom", Vector3.down, Quaternion.Euler(90f, 0f, 0f), primary, secondary);

        if (assignToDiceLists)
            AssignMeshLists(dice, primary, secondary);

        dice.preferMeshDecals = true;
        EditorUtility.SetDirty(dice);
        PrefabUtility.RecordPrefabInstancePropertyModifications(dice);
        Debug.Log($"{dice.name}: Built 6 manual mesh decal faces.", dice);
    }

    static void BuildManualFace(Transform parent, Dice dice, string faceName, Vector3 localNormal, Quaternion localRotation, List<MeshRenderer> primary, List<MeshRenderer> secondary)
    {
        MeshRenderer first = CreateManualFaceRenderer(parent, dice, faceName, "Primary", localNormal, localRotation, 0f);
        MeshRenderer second = CreateManualFaceRenderer(parent, dice, faceName, "Secondary", localNormal, localRotation, manualFaceLayerOffset);
        ApplyPreviewMaterials(dice, first, second);
        primary.Add(first);
        secondary.Add(second);
    }

    static MeshRenderer CreateManualFaceRenderer(Transform parent, Dice dice, string faceName, string suffix, Vector3 localNormal, Quaternion localRotation, float extraOffset)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = $"{faceName}_{suffix}";
        Undo.RegisterCreatedObjectUndo(go, "Create manual face decal");
        go.transform.SetParent(parent, false);
        go.transform.localRotation = localRotation;
        go.transform.localPosition = localNormal.normalized * (manualFaceOffset + extraOffset);
        go.transform.localScale = Vector3.one * manualFaceSize;

        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);

        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.allowOcclusionWhenDynamic = false;

        return renderer;
    }
    static void BuildMeshFaceDecals(Dice dice)
    {
        if (dice == null)
            return;

        if (clearExisting)
            ClearAutoDecals(dice);

        MeshFilter meshFilter = GetMeshFilter(dice);
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogWarning($"{dice.name}: Khong tim thay MeshFilter/sharedMesh de tao mesh decal.", dice);
            return;
        }

        List<FaceGroup> faces = BuildFaceGroups(dice, meshFilter);
        if (faces.Count == 0)
        {
            Debug.LogWarning($"{dice.name}: Mesh khong co face nao de tao mesh decal.", dice);
            return;
        }

        Transform group = GetOrCreateAutoGroup(dice.transform);
        List<MeshRenderer> primary = new List<MeshRenderer>();
        List<MeshRenderer> secondary = new List<MeshRenderer>();

        for (int i = 0; i < faces.Count; i++)
        {
            MeshRenderer first = CreateMeshDecal(group, faces[i], i + 1, "Primary", 0f, faces.Count);
            MeshRenderer second = CreateMeshDecal(group, faces[i], i + 1, "Secondary", 0.0005f, faces.Count);
            ApplyPreviewMaterials(dice, first, second);
            primary.Add(first);
            secondary.Add(second);
        }

        if (assignToDiceLists)
            AssignMeshLists(dice, primary, secondary);

        EditorUtility.SetDirty(dice);
        PrefabUtility.RecordPrefabInstancePropertyModifications(dice);
        Debug.Log($"{dice.name}: Built {faces.Count} mesh decal faces from '{meshFilter.name}'.", dice);
    }

    static void BuildFaceDecals(Dice dice)
    {
        if (dice == null)
            return;

        if (clearExisting)
            ClearAutoDecals(dice);

        MeshFilter meshFilter = GetMeshFilter(dice);
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogWarning($"{dice.name}: Khong tim thay MeshFilter/sharedMesh de tao decal.", dice);
            return;
        }

        List<FaceGroup> faces = BuildFaceGroups(dice, meshFilter);
        if (faces.Count == 0)
        {
            Debug.LogWarning($"{dice.name}: Mesh khong co face nao de tao decal.", dice);
            return;
        }

        Transform group = GetOrCreateAutoGroup(dice.transform);
        List<DecalProjector> primary = new List<DecalProjector>();
        List<DecalProjector> secondary = new List<DecalProjector>();

        for (int i = 0; i < faces.Count; i++)
        {
            FaceBuildData face = faces[i].Build(facePadding);
            DecalProjector first = CreateProjector(group, face, i + 1, "Primary", 0f);
            DecalProjector second = CreateProjector(group, face, i + 1, "Secondary", 0.01f);
            ApplyPreviewMaterials(dice, first, second);
            primary.Add(first);
            secondary.Add(second);
        }

        if (assignToDiceLists)
            AssignLists(dice, primary, secondary);

        EditorUtility.SetDirty(dice);
        PrefabUtility.RecordPrefabInstancePropertyModifications(dice);
        Debug.Log($"{dice.name}: Built {faces.Count} projector decal faces from '{meshFilter.name}'.", dice);
    }

    static MeshFilter GetMeshFilter(Dice dice)
    {
        if (dice.meshRenderer != null)
        {
            MeshFilter meshFilter = dice.meshRenderer.GetComponent<MeshFilter>();
            if (meshFilter != null && !IsAutoDecalTransform(meshFilter.transform))
                return meshFilter;
        }

        MeshFilter[] meshFilters = dice.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < meshFilters.Length; i++)
        {
            MeshFilter meshFilter = meshFilters[i];
            if (meshFilter == null || meshFilter.sharedMesh == null)
                continue;

            if (IsAutoDecalTransform(meshFilter.transform))
                continue;

            if (meshFilter.GetComponent<DecalProjector>() != null)
                continue;

            return meshFilter;
        }

        return null;
    }

    static bool IsAutoDecalTransform(Transform transform)
    {
        while (transform != null)
        {
            if (transform.name == AutoGroupName)
                return true;

            transform = transform.parent;
        }

        return false;
    }

    static List<FaceGroup> BuildFaceGroups(Dice dice, MeshFilter meshFilter)
    {
        Mesh mesh = meshFilter.sharedMesh;
        Vector3[] meshVertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector3 meshCenter = ToDiceLocal(dice, meshFilter, mesh.bounds.center);

        int triangleCount = triangles.Length / 3;
        Vector3[] aPoints = new Vector3[triangleCount];
        Vector3[] bPoints = new Vector3[triangleCount];
        Vector3[] cPoints = new Vector3[triangleCount];
        Vector3[] normals = new Vector3[triangleCount];
        float[] distances = new float[triangleCount];

        Dictionary<int, List<int>> vertexToTriangles = new Dictionary<int, List<int>>();
        for (int triangleIndex = 0; triangleIndex < triangleCount; triangleIndex++)
        {
            int baseIndex = triangleIndex * 3;
            int indexA = triangles[baseIndex];
            int indexB = triangles[baseIndex + 1];
            int indexC = triangles[baseIndex + 2];

            Vector3 a = ToDiceLocal(dice, meshFilter, meshVertices[indexA]);
            Vector3 b = ToDiceLocal(dice, meshFilter, meshVertices[indexB]);
            Vector3 c = ToDiceLocal(dice, meshFilter, meshVertices[indexC]);

            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            if (normal.sqrMagnitude < 0.001f)
                continue;

            Vector3 triangleCenter = (a + b + c) / 3f;
            if (Vector3.Dot(normal, triangleCenter - meshCenter) < 0f)
                normal = -normal;

            aPoints[triangleIndex] = a;
            bPoints[triangleIndex] = b;
            cPoints[triangleIndex] = c;
            normals[triangleIndex] = normal;
            distances[triangleIndex] = Vector3.Dot(normal, a);

            AddTriangleLink(vertexToTriangles, indexA, triangleIndex);
            AddTriangleLink(vertexToTriangles, indexB, triangleIndex);
            AddTriangleLink(vertexToTriangles, indexC, triangleIndex);
        }

        List<FaceGroup> faces = new List<FaceGroup>();
        bool[] visited = new bool[triangleCount];

        for (int triangleIndex = 0; triangleIndex < triangleCount; triangleIndex++)
        {
            if (visited[triangleIndex])
                continue;

            Vector3 seedNormal = normals[triangleIndex];
            if (seedNormal.sqrMagnitude < 0.001f)
                continue;

            FaceGroup group = new FaceGroup(seedNormal, distances[triangleIndex]);
            Queue<int> queue = new Queue<int>();
            queue.Enqueue(triangleIndex);
            visited[triangleIndex] = true;

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                group.AddTriangle(aPoints[current], bPoints[current], cPoints[current]);

                int currentBase = current * 3;
                for (int corner = 0; corner < 3; corner++)
                {
                    int vertexIndex = triangles[currentBase + corner];
                    if (!vertexToTriangles.TryGetValue(vertexIndex, out List<int> neighbors))
                        continue;

                    for (int i = 0; i < neighbors.Count; i++)
                    {
                        int neighbor = neighbors[i];
                        if (visited[neighbor])
                            continue;

                        if (normals[neighbor].sqrMagnitude < 0.001f)
                            continue;

                        if (Vector3.Dot(seedNormal, normals[neighbor]) < normalDotTolerance)
                            continue;

                        if (Mathf.Abs(distances[neighbor] - distances[triangleIndex]) > planeDistanceTolerance)
                            continue;

                        visited[neighbor] = true;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            faces.Add(group);
        }

        SortFaces(faces);
        return faces;
    }

    static Vector3 ToDiceLocal(Dice dice, MeshFilter meshFilter, Vector3 meshLocalPosition)
    {
        Vector3 worldPosition = meshFilter.transform.TransformPoint(meshLocalPosition);
        return dice.transform.InverseTransformPoint(worldPosition);
    }

    static void AddTriangleLink(Dictionary<int, List<int>> vertexToTriangles, int vertexIndex, int triangleIndex)
    {
        if (!vertexToTriangles.TryGetValue(vertexIndex, out List<int> linkedTriangles))
        {
            linkedTriangles = new List<int>();
            vertexToTriangles.Add(vertexIndex, linkedTriangles);
        }

        linkedTriangles.Add(triangleIndex);
    }

    static void SortFaces(List<FaceGroup> faces)
    {
        faces.Sort((a, b) =>
        {
            Vector3 ac = a.Center;
            Vector3 bc = b.Center;
            int y = -ac.y.CompareTo(bc.y);
            if (y != 0)
                return y;

            int z = -ac.z.CompareTo(bc.z);
            if (z != 0)
                return z;

            return ac.x.CompareTo(bc.x);
        });
    }

    static DecalProjector CreateProjector(Transform parent, FaceBuildData face, int faceIndex, string label, float extraOffset)
    {
        GameObject go = new GameObject($"Face {faceIndex:00} {label}");
        Undo.RegisterCreatedObjectUndo(go, "Create dice decal projector");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = face.Center + face.Normal * (projectionDepth * 0.5f + surfaceOffset + extraOffset);
        go.transform.localRotation = Quaternion.LookRotation(-face.Normal, face.Up);
        go.transform.localScale = Vector3.one;

        DecalProjector projector = go.AddComponent<DecalProjector>();
        projector.size = new Vector3(face.Width, face.Height, projectionDepth);
        projector.pivot = Vector3.zero;
        projector.scaleMode = DecalScaleMode.InheritFromHierarchy;
        projector.startAngleFade = angleFadeStart;
        projector.endAngleFade = Mathf.Max(angleFadeStart, angleFadeEnd);
        return projector;
    }

    static MeshRenderer CreateMeshDecal(Transform parent, FaceGroup face, int faceIndex, string label, float offset, int faceCount)
    {
        GameObject go = new GameObject($"Face {faceIndex:00} {label} Mesh");
        Undo.RegisterCreatedObjectUndo(go, "Create dice mesh decal");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        MeshFilter meshFilter = go.AddComponent<MeshFilter>();
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

        meshFilter.sharedMesh = face.CreateWorldSpaceMesh($"{go.name} Mesh", meshInset, offset, faceIndex - 1, faceCount);
        return renderer;
    }

    static void ApplyPreviewMaterials(Dice dice, DecalProjector first, DecalProjector second)
    {
        if (dice.data == null || dice.data.decalMaterial == null)
            return;

        if (dice.data.decalMaterial.Count > 0)
            first.material = dice.data.decalMaterial[0];

        if (dice.data.decalMaterial.Count > 1)
            second.material = dice.data.decalMaterial[1];
    }

    static void ApplyPreviewMaterials(Dice dice, MeshRenderer first, MeshRenderer second)
    {
        if (dice.data == null || dice.data.decalMaterial == null)
            return;

        if (dice.data.decalMaterial.Count > 0)
            first.sharedMaterial = dice.data.decalMaterial[0];

        if (dice.data.decalMaterial.Count > 1)
            second.sharedMaterial = dice.data.decalMaterial[1];
    }

    static Transform GetOrCreateAutoGroup(Transform diceTransform)
    {
        Transform existing = diceTransform.Find(AutoGroupName);
        if (existing != null)
            return existing;

        GameObject group = new GameObject(AutoGroupName);
        Undo.RegisterCreatedObjectUndo(group, "Create dice decal group");
        group.transform.SetParent(diceTransform, false);
        group.transform.localPosition = Vector3.zero;
        group.transform.localRotation = Quaternion.identity;
        group.transform.localScale = Vector3.one;
        return group.transform;
    }

    static void AssignLists(Dice dice, List<DecalProjector> primary, List<DecalProjector> secondary)
    {
        Undo.RecordObject(dice, "Assign dice decals");
        //  dice.decals = primary;
        // dice.decals2 = secondary;
        EditorUtility.SetDirty(dice);
        PrefabUtility.RecordPrefabInstancePropertyModifications(dice);
    }

    static void AssignMeshLists(Dice dice, List<MeshRenderer> primary, List<MeshRenderer> secondary)
    {
        Undo.RecordObject(dice, "Assign dice mesh decals");
        dice.decalMeshes = primary;
        dice.decalMeshes2 = secondary;
        EditorUtility.SetDirty(dice);
        PrefabUtility.RecordPrefabInstancePropertyModifications(dice);
    }

    static void CollectAutoDecals(Dice dice)
    {
        Transform group = dice.transform.Find(AutoGroupName);
        if (group == null)
        {
            Debug.LogWarning($"{dice.name}: Khong co group '{AutoGroupName}' de collect.", dice);
            return;
        }

        List<DecalProjector> primary = new List<DecalProjector>();
        List<DecalProjector> secondary = new List<DecalProjector>();
        List<MeshRenderer> meshPrimary = new List<MeshRenderer>();
        List<MeshRenderer> meshSecondary = new List<MeshRenderer>();
        DecalProjector[] projectors = group.GetComponentsInChildren<DecalProjector>(true);
        MeshRenderer[] meshRenderers = group.GetComponentsInChildren<MeshRenderer>(true);

        for (int i = 0; i < projectors.Length; i++)
        {
            DecalProjector projector = projectors[i];
            if (projector.name.Contains("Secondary"))
                secondary.Add(projector);
            else
                primary.Add(projector);
        }

        for (int i = 0; i < meshRenderers.Length; i++)
        {
            MeshRenderer meshRenderer = meshRenderers[i];
            if (meshRenderer.name.Contains("Secondary"))
                meshSecondary.Add(meshRenderer);
            else
                meshPrimary.Add(meshRenderer);
        }

        AssignLists(dice, primary, secondary);
        AssignMeshLists(dice, meshPrimary, meshSecondary);
    }

    static void ClearAutoDecals(Dice dice)
    {
        Transform group = dice.transform.Find(AutoGroupName);
        Undo.RecordObject(dice, "Clear dice decals");
        //  dice.decals.Clear();
        //  dice.decals2.Clear();
        dice.decalMeshes.Clear();
        dice.decalMeshes2.Clear();
        EditorUtility.SetDirty(dice);
        PrefabUtility.RecordPrefabInstancePropertyModifications(dice);

        if (group != null)
            Undo.DestroyObjectImmediate(group.gameObject);
    }

    static int GetQuarterTurnForFace(int faceIndex, int faceCount)
    {
        if (faceCount != 8)
            return 0;

        int[] turns = { 0, 2, 1, 1, 3, 3, 2, 0 };
        return turns[Mathf.Clamp(faceIndex, 0, turns.Length - 1)];
    }

    static Vector2 RotateUv(Vector2 uv, int quarterTurn)
    {
        switch ((quarterTurn % 4 + 4) % 4)
        {
            case 0:
                return new Vector2(1f - uv.x, uv.y);
            case 1:
                return new Vector2(1f - uv.y, 1f - uv.x);
            case 2:
                return new Vector2(uv.x, 1f - uv.y);
            case 3:
                return new Vector2(uv.y, uv.x);
            default:
                return uv;
        }
    }

    class FaceGroup
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<int> triangles = new List<int>();

        public Vector3 Normal { get; private set; }
        public float Distance { get; private set; }
        public Vector3 Center
        {
            get
            {
                Vector3 center = Vector3.zero;
                for (int i = 0; i < vertices.Count; i++)
                    center += vertices[i];

                return vertices.Count > 0 ? center / vertices.Count : Vector3.zero;
            }
        }

        public FaceGroup(Vector3 normal, float distance)
        {
            Normal = normal.normalized;
            Distance = distance;
        }

        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            int indexA = AddVertex(a);
            int indexB = AddVertex(b);
            int indexC = AddVertex(c);
            triangles.Add(indexA);
            triangles.Add(indexB);
            triangles.Add(indexC);
        }

        int AddVertex(Vector3 vertex)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                if ((vertices[i] - vertex).sqrMagnitude < 0.000001f)
                    return i;
            }

            vertices.Add(vertex);
            return vertices.Count - 1;
        }

        public FaceBuildData Build(float padding)
        {
            Vector3 center = GetProjectedCenter();
            Vector3 tangent;
            Vector3 up;
            GetFrame(out tangent, out up);

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 delta = vertices[i] - center;
                float x = Vector3.Dot(delta, tangent);
                float y = Vector3.Dot(delta, up);
                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);
            }

            float width = Mathf.Max(0.01f, maxX - minX) * padding;
            float height = Mathf.Max(0.01f, maxY - minY) * padding;

            if (useInscribedFit)
            {
                float innerSize = GetInnerFitSize(center, tangent, up) * padding;
                if (innerSize > 0.01f)
                {
                    width = innerSize;
                    height = innerSize;
                }
            }

            return new FaceBuildData(center, Normal, up, width, height);
        }

        public Mesh CreateWorldSpaceMesh(string meshName, float inset, float offset, int faceIndex, int faceCount)
        {
            Vector3 center = GetProjectedCenter();
            Vector3 tangent;
            Vector3 up;
            GetFrame(out tangent, out up);

            Vector3[] meshVertices = new Vector3[vertices.Count];
            Vector2[] uvs = new Vector2[vertices.Count];
            int[] meshTriangles = triangles.ToArray();

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            Vector2[] points2D = new Vector2[vertices.Count];

            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 delta = vertices[i] - center;
                Vector2 point = new Vector2(
                    Vector3.Dot(delta, tangent),
                    Vector3.Dot(delta, up));
                points2D[i] = point;
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minY = Mathf.Min(minY, point.y);
                maxY = Mathf.Max(maxY, point.y);
            }

            float clampedInset = Mathf.Clamp01(inset);
            int quarterTurn = GetQuarterTurnForFace(faceIndex, faceCount);
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector2 point = points2D[i] * clampedInset;
                meshVertices[i] = center + tangent * point.x + up * point.y + Normal * offset;
                Vector2 uv = new Vector2(
                    Mathf.InverseLerp(minX, maxX, points2D[i].x),
                    Mathf.InverseLerp(minY, maxY, points2D[i].y));
                uvs[i] = RotateUv(uv, quarterTurn);
            }

            Mesh mesh = new Mesh
            {
                name = meshName,
                vertices = meshVertices,
                uv = uvs,
                triangles = meshTriangles
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        Vector3 GetProjectedCenter()
        {
            Vector3 center = Center;
            float signedDistance = Vector3.Dot(Normal, center) - Distance;
            return center - Normal * signedDistance;
        }

        void GetFrame(out Vector3 tangent, out Vector3 up)
        {
            tangent = Vector3.ProjectOnPlane(Vector3.right, Normal);
            if (tangent.sqrMagnitude < 0.001f)
                tangent = Vector3.ProjectOnPlane(Vector3.forward, Normal);
            tangent.Normalize();
            up = Vector3.Cross(Normal, tangent).normalized;
        }

        float GetInnerFitSize(Vector3 center, Vector3 tangent, Vector3 up)
        {
            if (vertices.Count < 3)
                return 0f;

            List<Vector2> points = new List<Vector2>();
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 delta = vertices[i] - center;
                points.Add(new Vector2(
                    Vector3.Dot(delta, tangent),
                    Vector3.Dot(delta, up)));
            }

            SortClockwise(points);

            float minDistance = float.MaxValue;
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[(i + 1) % points.Count];
                minDistance = Mathf.Min(minDistance, DistancePointToSegment(Vector2.zero, a, b));
            }

            return Mathf.Max(0f, minDistance * 2f);
        }

        static void SortClockwise(List<Vector2> points)
        {
            points.Sort((a, b) =>
            {
                float angleA = Mathf.Atan2(a.y, a.x);
                float angleB = Mathf.Atan2(b.y, b.x);
                return angleA.CompareTo(angleB);
            });
        }

        static float DistancePointToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float sqrMagnitude = ab.sqrMagnitude;
            if (sqrMagnitude < 0.000001f)
                return Vector2.Distance(point, a);

            float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / sqrMagnitude);
            Vector2 closest = a + ab * t;
            return Vector2.Distance(point, closest);
        }
    }

    readonly struct FaceBuildData
    {
        public readonly Vector3 Center;
        public readonly Vector3 Normal;
        public readonly Vector3 Up;
        public readonly float Width;
        public readonly float Height;

        public FaceBuildData(Vector3 center, Vector3 normal, Vector3 up, float width, float height)
        {
            Center = center;
            Normal = normal.normalized;
            Up = up.normalized;
            Width = width;
            Height = height;
        }
    }
}



