using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
public class DiceBarycentric : MonoBehaviour
{
    void Awake()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;

        var verts = mesh.vertices;
        var tris = mesh.triangles;

        List<Vector3> newVerts = new();
        List<int> newTris = new();
        List<Vector3> bary = new();

        for (int i = 0; i < tris.Length; i += 3)
        {
            int baseIndex = newVerts.Count;

            newVerts.Add(verts[tris[i]]);
            newVerts.Add(verts[tris[i + 1]]);
            newVerts.Add(verts[tris[i + 2]]);

            bary.Add(new Vector3(1, 0, 0));
            bary.Add(new Vector3(0, 1, 0));
            bary.Add(new Vector3(0, 0, 1));

            newTris.Add(baseIndex);
            newTris.Add(baseIndex + 1);
            newTris.Add(baseIndex + 2);
        }

        Mesh newMesh = new Mesh();
        newMesh.vertices = newVerts.ToArray();
        newMesh.triangles = newTris.ToArray();
        newMesh.SetUVs(1, bary);

        GetComponent<MeshFilter>().mesh = newMesh;
    }
}