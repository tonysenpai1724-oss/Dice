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

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Save Cached Decals"))
                SaveCachedDecals(dice);

            if (GUILayout.Button("Clear Auto Decals"))
                ClearAutoDecals(dice);
        }
    }


    static void SaveCachedDecals(Dice dice)
    {
        if (dice == null)
            return;

        DiceDecalMeshCache cache = dice.GetComponent<DiceDecalMeshCache>();
        if (cache == null || cache.decalMeshes == null || cache.decalMeshes.Count == 0)
        {
            Debug.LogWarning($"{dice.name}: Khong co cached decal meshes de luu.", dice);
            return;
        }

        int savedCount = 0;
        for (int i = 0; i < cache.decalMeshes.Count; i++)
        {
            MeshRenderer renderer = cache.decalMeshes[i];
            if (renderer == null)
                continue;

            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
                continue;

            string meshName = string.IsNullOrEmpty(renderer.name) ? $"CachedDecal_{i:00}" : renderer.name;
            meshFilter.sharedMesh = PersistGeneratedMesh(dice, Object.Instantiate(meshFilter.sharedMesh), meshName);
            EditorUtility.SetDirty(meshFilter);
            PrefabUtility.RecordPrefabInstancePropertyModifications(meshFilter);
            savedCount++;
        }

        EditorUtility.SetDirty(cache);
        EditorUtility.SetDirty(dice);
        PrefabUtility.RecordPrefabInstancePropertyModifications(cache);
        PrefabUtility.RecordPrefabInstancePropertyModifications(dice);
        AssetDatabase.SaveAssets();
        Debug.Log($"{dice.name}: Saved {savedCount} cached decal meshes to prefab asset.", dice);
    }
    static void BuildManualFaceDecals(Dice dice)
    {
        if (dice == null)
            return;

        if (clearExisting)
            ClearAutoDecals(dice);

        Transform group = GetOrCreateAutoGroup(dice.transform);
        List<MeshRenderer> primary = new List<MeshRenderer>();

        BuildManualFace(group, dice, "Front", Vector3.forward, Quaternion.identity, primary);
        BuildManualFace(group, dice, "Back", Vector3.back, Quaternion.Euler(0f, 180f, 0f), primary);
        BuildManualFace(group, dice, "Right", Vector3.right, Quaternion.Euler(0f, 90f, 0f), primary);
        BuildManualFace(group, dice, "Left", Vector3.left, Quaternion.Euler(0f, -90f, 0f), primary);
        BuildManualFace(group, dice, "Top", Vector3.up, Quaternion.Euler(-90f, 0f, 0f), primary);
        BuildManualFace(group, dice, "Bottom", Vector3.down, Quaternion.Euler(90f, 0f, 0f), primary);

        if (assignToDiceLists)
            AssignMeshLists(dice, primary);

        dice.preferMeshDecals = true;
        EditorUtility.SetDirty(dice);
        PrefabUtility.RecordPrefabInstancePropertyModifications(dice);
        AssetDatabase.SaveAssets();
        Debug.Log($"{dice.name}: Built 6 manual mesh decal faces.", dice);
    }

    static void BuildManualFace(Transform parent, Dice dice, string faceName, Vector3 localNormal, Quaternion localRotation, List<MeshRenderer> primary)
    {
        MeshRenderer renderer = CreateManualFaceRenderer(parent, dice, faceName, "Primary", localNormal, localRotation, 0f);
        ApplyPreviewMaterial(dice, renderer);
        primary.Add(renderer);
    }

    static MeshRenderer CreateManualFaceRenderer(Transform parent, Dice dice, string faceName, string suffix, Vector3 localNormal, Quaternion localRotation, float extraOffset)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Undo.RegisterCreatedObjectUndo(go, "Create manual dice face mesh decal");
        go.name = $"{faceName}_{suffix}";
        go.transform.SetParent(parent, false);
        go.transform.localRotation = localRotation;
        go.transform.localPosition = localNormal * (manualFaceOffset + extraOffset);
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

        for (int i = 0; i < faces.Count; i++)
        {
            MeshRenderer renderer = CreateMeshDecal(group, dice, faces[i], i + 1, "Primary", 0f, faces.Count);
            ApplyPreviewMaterial(dice, renderer);
            primary.Add(renderer);
        }

        if (assignToDiceLists)
            AssignMeshLists(dice, primary);

        EditorUtility.SetDirty(dice);
        PrefabUtility.RecordPrefabInstancePropertyModifications(dice);
        AssetDatabase.SaveAssets();
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

        for (int i = 0; i < faces.Count; i++)
        {
            FaceBuildData face = faces[i].Build(facePadding);
            DecalProjector projector = CreateProjector(group, face, i + 1, "Primary", 0f);
            ApplyPreviewMaterial(dice, projector);
            primary.Add(projector);
        }

        if (assignToDiceLists)
            AssignLists(dice, primary);

        EditorUtility.SetDirty(dice);
        PrefabUtility.RecordPrefabInstancePropertyModifications(dice);
        AssetDatabase.SaveAssets();
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

        int triangleCount = triangles.Length / 3;
        Vector3[] normals = new Vector3[triangleCount];
        float[] distances = new float[triangleCount];
        Dictionary<int, List<int>> vertexToTriangles = new Dictionary<int, List<int>>();

        for (int i = 0; i < triangleCount; i++)
        {
            int index = i * 3;
            int ia = triangles[index];
            int ib = triangles[index + 1];
            int ic = triangles[index + 2];

            Vector3 a = ToDiceLocal(dice, meshFilter, meshVertices[ia]);
            Vector3 b = ToDiceLocal(dice, meshFilter, meshVertices[ib]);
            Vector3 c = ToDiceLocal(dice, meshFilter, meshVertices[ic]);

            Vector3 normal = Vector3.Cross(b - a, c - a);
            if (normal.sqrMagnitude > 0.000001f)
                normal.Normalize();

            Vector3 center = (a + b + c) / 3f;
            normals[i] = normal;
            distances[i] = Vector3.Dot(normal, center);

            AddTriangleLink(vertexToTriangles, ia, i);
            AddTriangleLink(vertexToTriangles, ib, i);
            AddTriangleLink(vertexToTriangles, ic, i);
        }

        List<FaceGroup> faces = new List<FaceGroup>();
        bool[] visited = new bool[triangleCount];
        Queue<int> queue = new Queue<int>();

        for (int triangleIndex = 0; triangleIndex < triangleCount; triangleIndex++)
        {
            if (visited[triangleIndex])
                continue;

            Vector3 seedNormal = normals[triangleIndex];
            if (seedNormal.sqrMagnitude < 0.001f)
                continue;

            visited[triangleIndex] = true;
            queue.Enqueue(triangleIndex);
            FaceGroup group = new FaceGroup(seedNormal, distances[triangleIndex]);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                int triBase = current * 3;
                int ia = triangles[triBase];
                int ib = triangles[triBase + 1];
                int ic = triangles[triBase + 2];

                group.AddTriangle(
                    ToDiceLocal(dice, meshFilter, meshVertices[ia]),
                    ToDiceLocal(dice, meshFilter, meshVertices[ib]),
                    ToDiceLocal(dice, meshFilter, meshVertices[ic]));

                for (int corner = 0; corner < 3; corner++)
                {
                    int vertexIndex = triangles[triBase + corner];
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

    static MeshRenderer CreateMeshDecal(Transform parent, Dice dice, FaceGroup face, int faceIndex, string label, float offset, int faceCount)
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

        Mesh mesh = face.CreateWorldSpaceMesh($"{go.name} Mesh", meshInset, offset, faceIndex - 1, faceCount);
        meshFilter.sharedMesh = PersistGeneratedMesh(dice, mesh, go.name);
        return renderer;
    }

    static Mesh PersistGeneratedMesh(Dice dice, Mesh mesh, string meshName)
    {
        if (mesh == null)
            return null;

        mesh.name = meshName;
        string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(dice.gameObject);
        if (string.IsNullOrEmpty(prefabPath))
            return mesh;

        Object prefabAsset = AssetDatabase.LoadMainAssetAtPath(prefabPath);
        if (prefabAsset == null)
            return mesh;

        Mesh existingMesh = FindSubAssetMesh(prefabPath, meshName);
        if (existingMesh != null)
            AssetDatabase.RemoveObjectFromAsset(existingMesh);

        mesh.hideFlags = HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(mesh, prefabAsset);
        EditorUtility.SetDirty(mesh);
        AssetDatabase.SaveAssets();

        Mesh savedMesh = FindSubAssetMesh(prefabPath, meshName);
        return savedMesh != null ? savedMesh : mesh;
    }

    static Mesh FindSubAssetMesh(string assetPath, string meshName)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        for (int i = 0; i < assets.Length; i++)
        {
            Mesh candidate = assets[i] as Mesh;
            if (candidate != null && candidate.name == meshName)
                return candidate;
        }

        return null;
    }

    static void ApplyPreviewMaterial(Dice dice, DecalProjector projector)
    {
        if (dice.data == null || dice.data.decalMaterial == null || dice.data.decalMaterial.Count == 0)
            return;

        projector.material = dice.data.decalMaterial[0];
    }

    static void ApplyPreviewMaterial(Dice dice, MeshRenderer renderer)
    {
        if (dice.data == null || dice.data.decalMaterial == null || dice.data.decalMaterial.Count == 0)
            return;

        renderer.sharedMaterial = dice.data.decalMaterial[0];
    }

    static void AssignLists(Dice dice, List<DecalProjector> primary)
    {
        EditorUtility.SetDirty(dice);
        PrefabUtility.RecordPrefabInstancePropertyModifications(dice);
    }

    static void AssignMeshLists(Dice dice, List<MeshRenderer> primary)
    {
        dice.decalMeshes = primary;
        dice.decalMeshes2 = new List<MeshRenderer>();
        dice.decalMeshes3 = new List<MeshRenderer>();
        dice.preferMeshDecals = true;
        EditorUtility.SetDirty(dice);
        PrefabUtility.RecordPrefabInstancePropertyModifications(dice);
    }


    static void CacheGeneratedMeshes(Dice dice, List<MeshRenderer> renderers)
    {
        DiceDecalMeshCache cache = dice.GetComponent<DiceDecalMeshCache>();
        if (cache == null)
            cache = Undo.AddComponent<DiceDecalMeshCache>(dice.gameObject);

        cache.decalMeshes.Clear();
        for (int i = 0; i < renderers.Count; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (renderer == null)
                continue;

            cache.decalMeshes.Add(renderer);
        }

        EditorUtility.SetDirty(cache);
        PrefabUtility.RecordPrefabInstancePropertyModifications(cache);
    }
    static void CollectAutoDecals(Dice dice)
    {
        if (dice == null)
            return;

        Transform group = FindAutoGroup(dice.transform);
        if (group == null)
        {
            Debug.LogWarning($"{dice.name}: Khong tim thay '{AutoGroupName}' de collect.", dice);
            return;
        }

        List<DecalProjector> primary = new List<DecalProjector>();
        List<MeshRenderer> meshPrimary = new List<MeshRenderer>();
        DecalProjector[] projectors = group.GetComponentsInChildren<DecalProjector>(true);
        MeshRenderer[] meshRenderers = group.GetComponentsInChildren<MeshRenderer>(true);

        for (int i = 0; i < projectors.Length; i++)
            primary.Add(projectors[i]);

        for (int i = 0; i < meshRenderers.Length; i++)
            meshPrimary.Add(meshRenderers[i]);

        primary.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        meshPrimary.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

        AssignLists(dice, primary);
        AssignMeshLists(dice, meshPrimary);

        Debug.Log($"{dice.name}: Collected {meshPrimary.Count} mesh decals, {primary.Count} projector decals.", dice);
    }

    static void ClearAutoDecals(Dice dice)
    {
        if (dice == null)
            return;

        Transform group = FindAutoGroup(dice.transform);
        if (group != null)
            Undo.DestroyObjectImmediate(group.gameObject);

        dice.decalMeshes.Clear();
        dice.decalMeshes2.Clear();
        dice.decalMeshes3.Clear();
        EditorUtility.SetDirty(dice);
        PrefabUtility.RecordPrefabInstancePropertyModifications(dice);
    }

    static Transform GetOrCreateAutoGroup(Transform parent)
    {
        Transform existing = FindAutoGroup(parent);
        if (existing != null)
            return existing;

        GameObject group = new GameObject(AutoGroupName);
        Undo.RegisterCreatedObjectUndo(group, "Create auto decal group");
        group.transform.SetParent(parent, false);
        group.transform.localPosition = Vector3.zero;
        group.transform.localRotation = Quaternion.identity;
        group.transform.localScale = Vector3.one;
        return group.transform;
    }

    static Transform FindAutoGroup(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == AutoGroupName)
                return child;
        }

        return null;
    }

    sealed class FaceGroup
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<int> triangles = new List<int>();
        readonly Dictionary<Vector3Key, int> vertexLookup = new Dictionary<Vector3Key, int>();

        public Vector3 Normal { get; }
        public float Distance { get; }
        public Vector3 Center { get; private set; }

        public FaceGroup(Vector3 normal, float distance)
        {
            Normal = normal.normalized;
            Distance = distance;
            Center = Vector3.zero;
        }

        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            int ia = AddVertex(a);
            int ib = AddVertex(b);
            int ic = AddVertex(c);

            triangles.Add(ia);
            triangles.Add(ib);
            triangles.Add(ic);
            Center = GetProjectedCenter();
        }

        int AddVertex(Vector3 vertex)
        {
            Vector3Key key = new Vector3Key(vertex);
            if (vertexLookup.TryGetValue(key, out int existingIndex))
                return existingIndex;

            int newIndex = vertices.Count;
            vertexLookup.Add(key, newIndex);
            vertices.Add(vertex);
            return newIndex;
        }

        public FaceBuildData Build(float padding)
        {
            Vector3 center = GetProjectedCenter();
            GetFrame(out Vector3 tangent, out Vector3 up);
            float width = GetAxisSize(center, tangent) * padding;
            float height = GetAxisSize(center, up) * padding;

            if (useInscribedFit)
            {
                float innerFit = GetInnerFitSize(center, tangent, up);
                if (innerFit > 0.0001f)
                {
                    width = Mathf.Min(width, innerFit * facePadding);
                    height = Mathf.Min(height, innerFit * facePadding);
                }
            }

            width = Mathf.Max(width, 0.001f);
            height = Mathf.Max(height, 0.001f);
            return new FaceBuildData(center + Normal * surfaceOffset, Normal, up, width, height);
        }

        float GetAxisSize(Vector3 center, Vector3 axis)
        {
            float min = float.MaxValue;
            float max = float.MinValue;

            for (int i = 0; i < vertices.Count; i++)
            {
                float distance = Vector3.Dot(vertices[i] - center, axis);
                min = Mathf.Min(min, distance);
                max = Mathf.Max(max, distance);
            }

            return max - min;
        }

        public Mesh CreateWorldSpaceMesh(string meshName, float inset, float offset, int faceIndex, int faceCount)
        {
            Vector3 center = GetProjectedCenter();
            GetFrame(out Vector3 tangent, out Vector3 up);

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
                Vector2 point = new Vector2(Vector3.Dot(delta, tangent), Vector3.Dot(delta, up));
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
                points.Add(new Vector2(Vector3.Dot(delta, tangent), Vector3.Dot(delta, up)));
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
            points.Sort((a, b) => Mathf.Atan2(a.y, a.x).CompareTo(Mathf.Atan2(b.y, b.x)));
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

    readonly struct Vector3Key
    {
        readonly int x;
        readonly int y;
        readonly int z;

        public Vector3Key(Vector3 vector)
        {
            x = Mathf.RoundToInt(vector.x * 10000f);
            y = Mathf.RoundToInt(vector.y * 10000f);
            z = Mathf.RoundToInt(vector.z * 10000f);
        }
    }
}





