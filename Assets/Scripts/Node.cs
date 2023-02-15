using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Node : MonoBehaviour
{
    public List<Door> doors = new List<Door>();
    public LinkedList<RoomSegment> segments;
    RoomGeneratorOptions options;
    MeshFilter meshFilter;
    Mesh mesh;
    System.Random random = new System.Random();
    bool firstShown = false;

    public void setupNode(LinkedList<RoomSegment> segments, Node previousNode, Door previousDoor, RoomGeneratorOptions options)
    {
        this.segments = segments;
        this.options = options;

        // first room doesn't have a back door
        if(previousNode != null && options.backDoorToPreviousRoom)
        {
            Door door = gameObject.AddComponent<Door>();
            door.setupDoor(segments.First.Value, this, previousDoor.position);
            door.nextNode = previousNode;
            door.transform.parent = transform;
            doors.Add(door);
        }
    }

    public void generateDoors()
    {
        int numberOfDoors = random.Next(options.minNumberOfDoorsPerRoom, options.maxNumberOfDoorsPerRoom);

        List<RoomSegment> segmentsThatAllowDoors = segments.Where(s => 
            s.canContainDoor(options.doorWidth, options.lengthInRhythmDirectionWherePlayAreaCannotEnd, LayoutCreator.playArea) &&
            doors.Where(x => x.roomSegment == s).Count() == 0).ToList(); //disallow multiple doors per segment (TODO)
        if(segmentsThatAllowDoors.Count == 0)
        {
            Debug.LogError("No segments that allow doors found!");
            return;
        }
        for(int i=0; i < (numberOfDoors>segmentsThatAllowDoors.Count ? segmentsThatAllowDoors.Count : numberOfDoors); i++)
        {
            RoomSegment segment = segmentsThatAllowDoors[random.Next(0, segmentsThatAllowDoors.Count)];
            segmentsThatAllowDoors.Remove(segment); //same segment can't have another door
            Door door = gameObject.AddComponent<Door>();
            door.setupDoor(segment, this, LayoutCreator.Vector2At(segment.getRandomDoorLocation(options), 0));
            door.transform.parent = transform;
            doors.Add(door);
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
                if (doorOnSegment.Count == 1) //TODO what if two or more doors on one segment?
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
                    Vector3 doorDirection = (vertices[3] - vertices[0]).normalized;
                    Vector3 doorMiddlePoint = doorOnSegment[0].position;
                    vertices[7] = doorMiddlePoint - doorDirection * options.doorWidth / 2;
                    vertices[4] = doorMiddlePoint + doorDirection * options.doorWidth / 2;
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
            List<Vector3> verticesInverted = verticesList[i].ToList();
            verticesInverted.Reverse();
            combinedTriangles.AddRange(trianglesList[i].Select(x => x + combinedVertices.Count));
            combinedVertices.AddRange(verticesInverted.ToArray());
        }
        mesh.vertices = combinedVertices.ToArray();
        mesh.triangles = combinedTriangles.ToArray();

        meshFilter.mesh = mesh;
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
                if(door.nextNode == null)
                {
                    Vector2 p2ToP1 = door.getPoint2() - door.getPoint1();
                    door.nextNode = LayoutCreator.createRandomRoom(new(door.position.x, door.position.z), new Vector2(p2ToP1.y, -p2ToP1.x).normalized, this, door, options);
                }
            }
        }
    }
}
