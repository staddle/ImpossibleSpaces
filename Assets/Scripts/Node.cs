using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Node : MonoBehaviour
{
    List<Door> doors;
    LinkedList<RoomSegment> segments;
    MeshFilter meshFilter;
    Mesh mesh;

    public void createNode(LinkedList<RoomSegment> segments, float wallHeight, Material material)
    {
        this.segments = segments;
        meshFilter = gameObject.AddComponent<MeshFilter>();
        mesh = new Mesh();
        meshFilter.mesh = mesh;
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = material;
        
        for(int i = 0; i < segments.Count; i++)
        {
            RoomSegment segment = segments.ElementAt(i);
            if(segment.GetType() == typeof(StraightRoomSegment))
            {
                StraightRoomSegment straight = (StraightRoomSegment)segment;
                Vector3[] vertices = new Vector3[4];
                vertices[0] = new Vector3(straight.startPoint.x, 0, straight.startPoint.y);
                vertices[1] = vertices[0] + Vector3.up * wallHeight;
                vertices[3] = new Vector3(straight.endPoint.x, 0, straight.endPoint.y);
                vertices[2] = vertices[3] + Vector3.up * wallHeight;
                mesh.vertices = mesh.vertices.Concat(vertices).ToArray();
                List<int> triangles = new List<int> { 0, 1, 2, 2, 3, 0 };
                mesh.triangles = mesh.triangles.Concat(triangles.Select(x => x + mesh.vertices.Length - 4)).ToArray();
            }
        }
        /*mesh.vertices = mesh.vertices.Concat(mesh.vertices).ToArray();
        int[] trianglesCopy = mesh.triangles;
        for(int i=0; i<trianglesCopy.Length; i+=3)
        {
            int t = trianglesCopy[i];
            trianglesCopy[i] = trianglesCopy[i + 2];
            trianglesCopy[i + 2] = t;
        }
        mesh.triangles.Concat(trianglesCopy);
        mesh.RecalculateNormals();
        for(int i = mesh.normals.Length / 2; i < mesh.normals.Length; i++)
        {
            mesh.normals[i] = mesh.normals[i] * -1;
        }*/ //TODO try to have doubled mesh with one inverted and one not inverted to see walls also from outside
        meshFilter.mesh = invertMesh(mesh);
    }

    private Mesh invertMesh(Mesh mesh)
    {
        Vector3[] normals = mesh.normals;
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = -normals[i];
        }
        mesh.normals = normals;

        int[] triangles = mesh.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int t = triangles[i];
            triangles[i] = triangles[i + 2];
            triangles[i + 2] = t;
        }

        mesh.triangles = triangles;
        return mesh;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
