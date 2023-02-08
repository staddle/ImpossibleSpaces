using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Node : MonoBehaviour
{
    public List<Door> doors = new List<Door>();
    LinkedList<RoomSegment> segments;
    RoomGeneratorOptions options;
    MeshFilter meshFilter;
    Mesh mesh;
    System.Random random = new System.Random();
    bool firstShown = false;

    public void setupNode(LinkedList<RoomSegment> segments, Node previousNode, RoomGeneratorOptions options)
    {
        this.segments = segments;
        this.options = options;

        // first room doesn't have a back door
        if(previousNode != null)
        {
            Door door = gameObject.AddComponent<Door>();
            door.setupDoor(segments.First.Value, this);
            door.nextNode = previousNode;
            door.transform.parent = transform;
            doors.Add(door);
        }
    }

    public void generateDoors()
    {
        int numberOfDoors = random.Next(options.minNumberOfDoorsPerRoom, options.maxNumberOfDoorsPerRoom);
        double probPerSegment = (double)numberOfDoors / (double)segments.Count;

        for(int i=0; i<segments.Count; i++)
        {
            if (doors.Count == numberOfDoors)
                break;
            if(random.NextDouble() <= probPerSegment && segments.ElementAt(i).canContainDoor(options.doorWidth, LayoutCreator.playArea))
            {
                Door door = gameObject.AddComponent<Door>();
                door.setupDoor(segments.ElementAt(i), this);
                door.transform.parent = transform;

                doors.Add(door);
            }
        }
    }

    public void createMesh(float wallHeight, Material material)
    {
        if(segments.Count == 0)
        {
            Debug.LogError("No room segments found to generate mesh with");
            return;
        }
        meshFilter = gameObject.AddComponent<MeshFilter>();
        mesh = new Mesh();
        meshFilter.mesh = mesh;
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = material;

        List<List<int>> trianglesList = new List<List<int>>();
        List<Vector3[]> verticesList = new List<Vector3[]>();

        for (int i = 0; i < segments.Count; i++)
        {
            RoomSegment segment = segments.ElementAt(i);
            // render straight room segments
            if (segment.GetType() == typeof(StraightRoomSegment))
            {
                StraightRoomSegment straight = (StraightRoomSegment)segment;
                List<Door> doorOnSegment = doors.Where(x => x.roomSegment == segment).ToList();
                if (doorOnSegment.Count == 1)
                {
                    /*
                     * 1-----2
                     * | 6-5 |
                     * | | | |
                     * 0-7 4-3
                     */
                    Vector3[] vertices = new Vector3[8];
                    vertices[0] = new Vector3(straight.startPoint.x, 0, straight.startPoint.y);
                    vertices[1] = vertices[0] + Vector3.up * wallHeight;
                    vertices[3] = new Vector3(straight.endPoint.x, 0, straight.endPoint.y);
                    vertices[2] = vertices[3] + Vector3.up * wallHeight;
                    double doorStart = random.NextDouble() * (Math.Abs(Vector3.Distance(vertices[3], vertices[0])) - options.doorWidth);
                    Vector3 doorDirection = vertices[3] - vertices[0];
                    doorDirection.Normalize();
                    vertices[7] = vertices[0] + doorDirection * (float)doorStart;
                    vertices[4] = vertices[7] + doorDirection * options.doorWidth;
                    vertices[5] = vertices[4] + Vector3.up * options.doorHeight;
                    vertices[6] = vertices[7] + Vector3.up * options.doorHeight;
                    doorOnSegment[0].point1 = vertices[7];
                    doorOnSegment[0].point2 = vertices[4];

                    List<int> triangles = new List<int>() { 0, 1, 6,
                                                            6, 7, 0,
                                                            1, 2, 6,
                                                            6, 2, 5,
                                                            5, 2, 3,
                                                            3, 4, 5};
                    verticesList.Add(vertices);
                    trianglesList.Add(triangles);
                }
                else if (doorOnSegment.Count == 0)
                {
                    /*
                     * 1-----2
                     * |     |
                     * |     |
                     * 0-----3
                     */
                    Vector3[] vertices = new Vector3[4];
                    vertices[0] = new Vector3(straight.startPoint.x, 0, straight.startPoint.y);
                    vertices[1] = vertices[0] + Vector3.up * wallHeight;
                    vertices[3] = new Vector3(straight.endPoint.x, 0, straight.endPoint.y);
                    vertices[2] = vertices[3] + Vector3.up * wallHeight;

                    List<int> triangles = new List<int> { 0, 1, 2,
                                                          2, 3, 0 };
                    verticesList.Add(vertices);
                    trianglesList.Add(triangles);
                }
            }
        }

        List<Vector3> combinedVertices = new List<Vector3>();
        List<int> combinedTriangles = new List<int>();

        for(int i=0; i<verticesList.Count; i++)
        {
            combinedTriangles.AddRange(trianglesList[i].Select(x => x + combinedVertices.Count));
            combinedVertices.AddRange(verticesList[i]);
        }
        mesh.vertices = combinedVertices.ToArray();
        mesh.triangles = combinedTriangles.ToArray();

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
        if(!firstShown && gameObject.activeSelf)
        {
            firstShown = true;
            foreach(Door door in doors)
            {
                Vector2 p2ToP1 = door.getPoint2() - door.getPoint1();
                door.nextNode = LayoutCreator.createRandomRoom(door.getPoint1() + p2ToP1 / 2, new Vector2(p2ToP1.y, -p2ToP1.x).normalized, this, options);
            }
        }
    }
}
