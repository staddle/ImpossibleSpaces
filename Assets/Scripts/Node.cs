using Assets.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static LayoutCreator;

public class Node : MonoBehaviour
{
    public List<Door> doors = new List<Door>();
    public LinkedList<RoomSegment> segments;
    public int depth;
    public RoomDebug roomDebug;
    public int LayerNumber { get { return 9 + depth % 10; } }
    RoomGeneratorOptions options;
    MeshFilter meshFilter;
    Mesh mesh;
    System.Random random = new System.Random();
    Door.OnCollisionEnterDel onCollisionEnter;
    Door.OnCollisionExitDel onCollisionExit;

    public void setupNode(LinkedList<RoomSegment> segments, Node previousNode, Door previousDoor, RoomDebug roomDebug, RoomGeneratorOptions options, Door.OnCollisionEnterDel callback, Door.OnCollisionExitDel callbackExit)
    {
        this.segments = segments;
        this.options = options;
        this.roomDebug = roomDebug;
        depth = previousNode == null ? 0 : previousNode.depth + 1;
        gameObject.layer = LayerNumber;
        onCollisionEnter = callback;
        onCollisionExit = callbackExit;

        // first room doesn't have a back door
        if(previousNode != null && options.backDoorToPreviousRoom)
        {
            GameObject doorGO = new GameObject("Door 0");
            doorGO.transform.parent = transform;
            doorGO.transform.position = previousDoor.position;
            doorGO.layer = previousNode.LayerNumber;
            Door door = doorGO.AddComponent<Door>();
            door.setupDoor(segments.First.Value, this, previousDoor.position, options.doorHeight, options.doorArea, options.doorWidth, onCollisionEnter, onCollisionExit, true);
            door.nextNode = previousNode;
            doors.Add(door);
        }
    }

    public void sendToShader(List<Vector3> vertices)
    {
        MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();
        Material material = renderer.material;
        if(vertices.Count > 0 )
        {
            List<Vector4> vector4s = vertices.Select(x => new Vector4(x.x, x.y, x.z, 0)).ToList();
            vector4s.AddRange(new Vector4[1000 - vertices.Count].ToList()); // fill up to 1000 to keep shader array the same size (reducing its size would maybe limit future array sizes)
            material.SetVectorArray("_RoomVertices", vector4s);
        }
        material.SetInt("_RoomVertexCount", vertices.Count);
        renderer.material = material;
    }

    public void setAllDoorsActive(bool active)
    {
        doors.ForEach(door => door.gameObject.SetActive(active));
    }

    public void generateDoors(List<Node> visibleRooms)
    {

        List<RoomSegment> segmentsThatAllowDoors = segments.Where(s => 
            s.canContainDoor(options.doorWidth, options.lengthInRhythmDirectionWherePlayAreaCannotEnd, playArea, visibleRooms) &&
            doors.Where(x => x.roomSegment == s).Count() == 0).ToList(); //disallow multiple doors per segment (TODO)

        int numberOfDoors = random.Next(options.minNumberOfDoorsPerRoom, Math.Min(options.maxNumberOfDoorsPerRoom, segmentsThatAllowDoors.Count));
        if(segmentsThatAllowDoors.Count == 0)
        {
            Debug.LogError("No segments that allow doors found!");
            return;
        }
        for(int i=0; i < (numberOfDoors>segmentsThatAllowDoors.Count ? segmentsThatAllowDoors.Count : numberOfDoors); i++)
        {
            RoomSegment segment = segmentsThatAllowDoors[random.Next(0, segmentsThatAllowDoors.Count)];
            segmentsThatAllowDoors.Remove(segment); //same segment can't have another door
            GameObject doorGO = new GameObject("Door "+(i+1));
            var position = Vector2At(segment.getRandomDoorLocation(options), 0);
            doorGO.transform.parent = transform;
            doorGO.transform.position = position;
            Door door = doorGO.AddComponent<Door>();
            door.setupDoor(segment, this, position, options.doorHeight, options.doorArea, options.doorWidth, onCollisionEnter, onCollisionExit);
            doors.Add(door);
        }
    }

    private List<Tuple<Vector2, float>> addToWallLocations(List<Tuple<Vector2, float>> wallLocations, Vector2 startPoint, Vector3 endPoint, Vector2 direction)
    {
        float dist = (endPoint - Vector2At(startPoint, 0)).magnitude;
        for (int p = 0; p < (int)dist; p++)
        {
            wallLocations.Add(new Tuple<Vector2, float>(startPoint + direction.normalized * p, 1));
        }
        float rest = dist - (int)dist;
        if (rest > 0)
        {
            wallLocations.Add(new Tuple<Vector2, float>(startPoint + direction.normalized * (int)dist, rest));
        }
        return wallLocations;
    }

    public void createMesh(float wallHeight, Material material)
    {
        if(segments.Count == 0)
        {
            Debug.LogError("No room segments found to generate mesh with");
            return;
        }
        //segments approach
        if (options.useSegments)
        {
            Debug.Log(roomDebug.bigRoom.vertices.ToArray().ToSeparatedString(" - "));
            for (int i = 0; i < segments.Count; i++)
            {
                RoomSegment segment = segments.ElementAt(i);
                List<Door> doorOnSegment = doors.Where(x => x.roomSegment == segment).ToList();
                var direction = segment.endPoint - segment.startPoint;
                var angle = Vector2.Angle(direction, Vector2.right);
                float sign = Mathf.Sign(Vector3.Dot(Vector3.down, Vector3.Cross(Vector2At(direction, 0), Vector2At(Vector2.right, 0))));
                float angle360 = angle * sign;
                List<Tuple<Vector2, float>> wallLocations = new List<Tuple<Vector2, float>>();
                Vector2 iPoint = segment.startPoint;

                foreach (Door door in doorOnSegment)
                {
                    wallLocations = addToWallLocations(wallLocations, iPoint, door.point1, direction);
                    GameObject doorGO = Instantiate(options.doorPrefab, transform);
                    doorGO.transform.position = door.point1;
                    doorGO.transform.rotation = Quaternion.Euler(0, angle360, 0);
                    doorGO.transform.GetChild(0).gameObject.layer = LayerNumber;
                    iPoint = door.getPoint2();
                }
                wallLocations = addToWallLocations(wallLocations, iPoint, Vector2At(segment.endPoint,0), direction);

                foreach (var loc in wallLocations)
                {
                    GameObject wall = Instantiate(options.wallSegmentPrefab, transform);
                    wall.transform.position = Vector2At(loc.Item1, 0);
                    wall.transform.rotation = Quaternion.Euler(0, angle360, 0);
                    wall.transform.localScale = new Vector3(loc.Item2, 1, 1);
                    wall.transform.GetChild(0).gameObject.layer = LayerNumber;
                }
            }

            // ceiling
            if (!options.useCeilings) return;
            var firstSegment = segments.ElementAt(0); 
            var point = firstSegment.startPoint;
            bool keepGoing = true;
            GameObject ceiling = options.ceilingPrefab;
            /*int pointIndex = 0;
            Vector2[] getPoint = new Vector2[4] { new(0, 0), new(1, 0), new(1, 1), new(0, 1) };
            int[] getRotation = new int[4] { 180, 270, 0, 90 };
            while(keepGoing)
            {
                GameObject ceil = Instantiate(ceiling, transform);
                ceil.transform.position = Vector2At(point, wallHeight);
                for (int i = 0; i < 4; i++)
                {
                    pointIndex = (pointIndex + 1) % 4;
                    if (roomDebug.bigRoom.isInside(getPoint[pointIndex]) || roomDebug.bigRoom.isOnEdge(getPoint[pointIndex]))
                    {
                        point = getPoint[(pointIndex - 1) % 4];
                        break;
                    }
                }
            }*/
            foreach(GeneralLayoutRoom room in roomDebug.generalLayoutRooms)
            {
                if (room == null) continue;
                if (room.vertices.Count != 4) continue;

                int cols = (int)Math.Ceiling((room.vertices[1] - room.vertices[0]).magnitude);
                int rows = (int)Math.Ceiling((room.vertices[2] - room.vertices[1]).magnitude);

                var startPoint = room.vertices[0];
                for(int i = 0; i < cols; i++)
                {
                    for(int j = 0; j < rows; j++)
                    {
                        GameObject ceil = Instantiate(ceiling, transform);
                        ceil.transform.position = Vector2At(startPoint + new Vector2(i, j), wallHeight);
                    }
                }
            }
        }
        
        //old full mesh generation approach
        var hasMeshFilter = gameObject.TryGetComponent(out meshFilter);
        if(!hasMeshFilter)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }
        mesh = new Mesh();
        meshFilter.mesh = mesh;
        MeshRenderer meshRenderer;
        MeshCollider meshCollider;
        var hasRenderer = gameObject.TryGetComponent(out meshRenderer);
        if (!hasRenderer)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshCollider = gameObject.AddComponent<MeshCollider>();
        } 
        else
        {
            meshCollider = gameObject.GetComponent<MeshCollider>();
        }
        meshRenderer.material = material;
        //meshCollider.isTrigger = true;

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
                    vertices[7] = doorOnSegment[0].point1;
                    vertices[4] = doorOnSegment[0].point2;
                    vertices[5] = vertices[4] + Vector3.up * options.doorHeight;
                    vertices[6] = vertices[7] + Vector3.up * options.doorHeight;

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
            // invert vertices to also make backside visible
            List<Vector3> verticesInverted = verticesList[i].ToList();
            verticesInverted.Reverse();
            combinedTriangles.AddRange(trianglesList[i].Select(x => x + combinedVertices.Count));
            combinedVertices.AddRange(verticesInverted.ToArray());
        }
        mesh.vertices = combinedVertices.ToArray();
        mesh.triangles = combinedTriangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
        
        meshCollider.sharedMesh = mesh;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        /*if(!firstShown && gameObject.activeSelf)
        {
            firstShown = true;
            generateNextRooms();
        }*/
    }

    public List<Vector3> getVertices()
    {
        return segments.Select(s => LayoutCreator.Vector2At(s.startPoint,0)).ToList();
    }

    public List<Vector2> getVerticesV2()
    {
        return segments.Select(s => (s.startPoint)).ToList();
    }
}
