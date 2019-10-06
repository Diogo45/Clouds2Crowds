using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGeneration : MonoBehaviour
{

    [SerializeField] public int ySize;
    [SerializeField] public int xSize;

    public int yVertices;
    public int xVertices;

    private float xEdgeSize;
    private float yEdgeSize;

    public Vector3 Origin;

    private Vector3[] vertices;
    Mesh mesh;
    private void Awake()
    {
        Generate();
    }

    private void Generate()
    {
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "Procedural Grid";

        xEdgeSize = ((float)xSize / (xVertices));
        yEdgeSize = ((float)ySize / (yVertices));

        vertices = new Vector3[(xVertices + 1) * (yVertices + 1)];
        Vector2[] uv = new Vector2[vertices.Length];
        Vector4[] tangents = new Vector4[vertices.Length];
        Vector4 tangent = new Vector4(1f, 0f, 0f, -1f);
        int i = 0;

        for (float y=0; y <= ySize; y+= yEdgeSize)
        {
            for (float x = 0; x <= xSize; x+=xEdgeSize, i++)
            {
                vertices[i] = Origin + new Vector3(x, 0, y);
                uv[i] = new Vector2((float)((double)x / (double)xSize), (float)((double)y / (double)ySize));
                //Debug.Log(uv[i]);
                tangents[i] = tangent;
            }
        }
        mesh.vertices = vertices;

        int[] triangles = new int[xVertices * yVertices * 6];
        for (int ti = 0, vi = 0, y = 0; y < yVertices; y++, vi++)
        {
            for (int x = 0; x < xVertices; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + xVertices + 1;
                triangles[ti + 5] = vi + xVertices + 2;
            }
        }
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.tangents = tangents;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

    }
}
