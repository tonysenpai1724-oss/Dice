using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DiceRoll))]
public class DiceRollEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DiceRoll roll = (DiceRoll)target;
        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Face Marker Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("Generate Face Markers From Decals"))
            GenerateFaceMarkersFromDecals(roll);
    }

    static void GenerateFaceMarkersFromDecals(DiceRoll roll)
    {
        if (roll == null)
            return;

        Transform autoDecals = roll.transform.Find("Auto Decals");
        if (autoDecals == null)
        {
            Debug.LogWarning($"{roll.name}: Khong tim thay 'Auto Decals'.", roll);
            return;
        }

        Transform existingGroup = roll.transform.Find("Face Markers");
        if (existingGroup != null)
            Undo.DestroyObjectImmediate(existingGroup.gameObject);

        GameObject markerGroup = new GameObject("Face Markers");
        Undo.RegisterCreatedObjectUndo(markerGroup, "Create face markers");
        markerGroup.transform.SetParent(roll.transform, false);
        markerGroup.transform.localPosition = Vector3.zero;
        markerGroup.transform.localRotation = Quaternion.identity;
        markerGroup.transform.localScale = Vector3.one;

        MeshRenderer[] renderers = autoDecals.GetComponentsInChildren<MeshRenderer>(true);
        System.Collections.Generic.List<Transform> markers = new System.Collections.Generic.List<Transform>();

        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (renderer == null)
                continue;

            string lowerName = renderer.name.ToLowerInvariant();
            if (!lowerName.Contains("face"))
                continue;

            Bounds bounds = renderer.bounds;
            if (bounds.size.sqrMagnitude <= 0.000001f)
                continue;

            Vector3 worldCenter = bounds.center;
            Vector3 direction = (worldCenter - roll.transform.position).normalized;
            if (direction.sqrMagnitude < 0.0001f)
                direction = renderer.transform.up;

            string markerName = renderer.name
                .Replace("Primary Mesh", "Marker")
                .Replace("Secondary Mesh", "Marker")
                .Trim();

            GameObject marker = new GameObject(markerName);
            Undo.RegisterCreatedObjectUndo(marker, "Create face marker");
            marker.transform.SetParent(markerGroup.transform, false);
            marker.transform.position = worldCenter + direction * 0.02f;
            marker.transform.rotation = Quaternion.identity;

            markers.Add(marker.transform);
        }

        markers.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        roll.diceFaces = markers.ToArray();

        EditorUtility.SetDirty(roll);
        PrefabUtility.RecordPrefabInstancePropertyModifications(roll);
        Debug.Log($"{roll.name}: Generated {markers.Count} face markers from decals.", roll);
    }
}
