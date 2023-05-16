using Assets.Scripts;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public partial class LayoutCreator : MonoBehaviour
{
    public static GeneralLayoutRoom playArea;
    public RoomGeneratorOptions roomGeneratorOptions;
    public bool testRoom = false;
    //public List<Vector2> testRoomVertices = new List<Vector2>() { new(), new(), new(), new() };
    public List<Vector2[]> testRoomsVertices = new List<Vector2[]>
    {
        new Vector2[] { new(0,0), new(0,5), new(5,5), new(5,3), new(7,3), new(7,0) },
        new Vector2[] { new(5,0), new(5,7), new(10,7), new(10,0)}
    };
    public AutoMoveThroughRooms autoMove;
    public AbstractRoomGenerationAlgorithm generationAlgorithm;
    [HideInInspector]
    public bool switchedRoom = false;

    private OldRoomGenerationAlgorithm oldRoomGenerationAlgorithm;
    private NewRoomGenerationAlgorithm newRoomGenerationAlgorithm;

    static Dictionary<Node, RoomDebug> roomDebugs = new Dictionary<Node, RoomDebug>();
    static LayoutCreator instance;

    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        var layersGenerator = new Layers();
        for (int i = 1; i <= 10; i++)
        {
            layersGenerator.AddNewLayer("RoomsLayer " + i);
        }

        roomGeneratorOptions = gameObject.GetComponent<RoomGeneratorOptions>();
        autoMove = roomGeneratorOptions.playerTransform.gameObject.GetComponent<AutoMoveThroughRooms>();
        playArea = new GeneralLayoutRoom(new List<Vector2>() { Vector2.zero, new(0, roomGeneratorOptions.playArea.y),
            new(roomGeneratorOptions.playArea.x, roomGeneratorOptions.playArea.y), new(roomGeneratorOptions.playArea.x, 0) }); ;

        buildPlayArea(roomGeneratorOptions.playArea, roomGeneratorOptions.playAreaWallHeight);
        setUpPlayerPosition(roomGeneratorOptions.playerStartingPoint);

        OldRoomGenerationAlgorithm oldRoomGenerationAlgorithm = GetComponent<OldRoomGenerationAlgorithm>();
        if (oldRoomGenerationAlgorithm == null)
            oldRoomGenerationAlgorithm = gameObject.AddComponent<OldRoomGenerationAlgorithm>();
        NewRoomGenerationAlgorithm newRoomGenerationAlgorithm = GetComponent<NewRoomGenerationAlgorithm>();
        if (newRoomGenerationAlgorithm == null)
            newRoomGenerationAlgorithm = gameObject.AddComponent<NewRoomGenerationAlgorithm>();
        if (roomGeneratorOptions.oldRoomGenerator)
            generationAlgorithm = oldRoomGenerationAlgorithm;
        else
            generationAlgorithm = newRoomGenerationAlgorithm;

        //test();

        generationAlgorithm.init(roomGeneratorOptions, testRoom, testRoomsVertices);
    }

    // Update is called once per frame
    void Update()
    {
        /*foreach (Door door in currentRoom.doors)
        {
            if (door.isInsideDoorArea(roomGeneratorOptions.playerTransform.position, roomGeneratorOptions.doorArea))
            {
                goNextRoom(door);
                break;
            }
        }*/
    }

    public static LayoutCreator get()
    {
        return instance;
    }

    public static Dictionary<Node, RoomDebug> RoomDebugs => roomDebugs;
    public Node CurrentRoom => generationAlgorithm.currentRoom;

    private void test()
    {
        //createRandomGeneralLayoutRoom

        var node = new Node();
        var roomDebug = new RoomDebug(null, new GeneralLayoutRoom(new List<Vector2> { new(2f, 2f), new(5, 2), new(5, 5), new(2, 5) }), null);
        var segments = fromVertices(roomDebug.bigRoom.vertices);
        node.roomDebug = roomDebug;
        var glr = AbstractRoomGenerationAlgorithm.createRandomGeneralLayoutRoom(new Vector2(5.5f, 3.5f), Vector2.up, 8f, false, new List<Node> { node }, roomGeneratorOptions);
        Debug.Log(glr.vertices.ToCommaSeparatedString());
    }

    private LinkedList<RoomSegment> fromVertices(List<Vector2> vertices)
    {
        var segments = new LinkedList<RoomSegment>();
        for(int i=0; i<vertices.Count; i++)
        {
            segments.AddLast(new RoomSegment(vertices[i], vertices[(i + 1) % vertices.Count]));
        }
        return segments;
    }

    public void regenerateLayout()
    {
        if (playArea != null)
        {
            GameObject roomsParent = GameObject.Find("Rooms");
            for (int i = 0; i < roomsParent.transform.childCount; i++)
            {
                Destroy(roomsParent.transform.GetChild(i).gameObject);
            }
            setUpPlayerPosition(roomGeneratorOptions.playerStartingPoint);
            generationAlgorithm.init(roomGeneratorOptions, testRoom, testRoomsVertices);
        }
    }

    public void goNextRoom(Door door)
    {
        generationAlgorithm.movedThroughDoor(door);
        if(get().autoMove != null)
        {
            get().autoMove.triggeredDoor(door);
        }
    }

    public static void CollidedWithDoor(Collider collider, Door door)
    {
        LayoutCreator layoutCreator = get();
        RoomGeneratorOptions roomGeneratorOptions1 = layoutCreator.roomGeneratorOptions;
        Transform playerTransform = roomGeneratorOptions1.playerTransform;
        GameObject playerObject = playerTransform.gameObject;
        CapsuleCollider player = playerObject.GetComponent<CapsuleCollider>();
        if (player != null && collider == player)
        {
            if (get().switchedRoom) get().switchedRoom = false;
            else
            {
                get().goNextRoom(door);
            }
        }
    }

    public void redraw()
    {
        //roomSegments.Clear();
        //connectPoints(sampledPoints);
        if (roomDebugs.TryGetValue(CurrentRoom, out RoomDebug roomDebug))
        {
            generationAlgorithm.redraw(roomDebug, roomGeneratorOptions);
        }
    }

    private void OnDrawGizmos()
    {
        if (roomGeneratorOptions == null)
            return;
        if (roomDebugs.TryGetValue(CurrentRoom, out RoomDebug roomDebug))
        {
            if (roomGeneratorOptions.showGeneralLayoutRooms)
            {
                Gizmos.color = Color.blue;
                foreach (GeneralLayoutRoom room in roomDebug.generalLayoutRooms)
                {
                    drawGeneralLayoutRoom(room);
                }
            }
            if(roomGeneratorOptions.showBigRoom && roomDebug.bigRoom != null)
            {
                Gizmos.color = Color.red;
                drawGeneralLayoutRoom(roomDebug.bigRoom, roomGeneratorOptions.showVertexNumbers);
            }
            if (roomGeneratorOptions.showSamplePoints && roomDebug.sampledPoints.Count > 0) 
            { 
                for(int i=0; i< roomDebug.sampledPoints.Count; i++)
                {
                    Vector3 point = Vector2At(roomDebug.sampledPoints.ElementAt(i), 0);
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(point, 0.05f);
                    DrawString(point.ToString(), point);
                    if(roomGeneratorOptions.showSamplePointNumbers)
                    {
                        DrawString(i.ToString(), point);
                    }
                }
            }
            if(roomGeneratorOptions.showFinishedRoom)
            {
                Gizmos.color = Color.black;
                List<BezierRoomSegment> filteredSegments = CurrentRoom.segments.ToList().Where(x => x.GetType() == typeof(BezierRoomSegment)).Select(x => (BezierRoomSegment)x).ToList();
                foreach(BezierRoomSegment segment in filteredSegments)
                {
                    segment.drawGizmos(roomGeneratorOptions.bezierSubdivisions, roomGeneratorOptions.showBezierTangents);
                }
            }
        }
    }

    private void buildPlayArea(Vector2 playArea, float wallHeight)
    {
        GameObject parentObject = new GameObject("PlayArea");
        parentObject.transform.position = new Vector3(0, 0, 0);
        GameObject wall1 = GameObject.CreatePrimitive(PrimitiveType.Plane);
        GameObject wall2 = GameObject.CreatePrimitive(PrimitiveType.Plane);
        GameObject wall3 = GameObject.CreatePrimitive(PrimitiveType.Plane);
        GameObject wall4 = GameObject.CreatePrimitive(PrimitiveType.Plane);
        List<GameObject> walls = new List<GameObject> { wall1, wall2, wall3, wall4 };

        foreach (var wall in walls)
        {
            wall.transform.parent = parentObject.transform;
            wall.transform.localScale = new Vector3(1.005f, 1, wallHeight / 10);
            wall.GetComponent<MeshCollider>().enabled = true;
        }

        //build play area a bit bigger to stop z-fighting of room and play area
        wall1.transform.position = new Vector3(playArea.x / 2, wallHeight / 2, 0 - 0.01f);
        wall1.transform.Rotate(Vector3.right, 90);

        wall2.transform.position = new Vector3(playArea.x / 2, wallHeight / 2, playArea.y + 0.01f);
        wall2.transform.Rotate(Vector3.left, 90);

        wall3.transform.position = new Vector3(0 - 0.01f, wallHeight / 2, playArea.y / 2);
        wall3.transform.Rotate(Vector3.left, 90);
        wall3.transform.Rotate(Vector3.back, 90);

        wall4.transform.position = new Vector3(playArea.x + 0.01f, wallHeight / 2, playArea.y / 2);
        wall4.transform.Rotate(Vector3.left, 90);
        wall4.transform.Rotate(Vector3.forward, 90);
    }

    private void setUpPlayerPosition(Vector3 startPosition)
    {
        roomGeneratorOptions.playerTransform.position = startPosition;
    }

    #region Room Creation
    /// <summary>
    /// Returns a random point on a random edge of the given room and a vector perpendicular to the edge
    /// </summary>
    /// <param name="room"></param>
    /// <returns></returns>
    public static (Vector2, Vector2) getPointOnRoomEdge(GeneralLayoutRoom room, Vector2? except = null)
    {
        if(room == null)
        {
            Debug.LogError("room was null!");
            return (Vector2.zero, Vector2.zero);
        }
        System.Random random = new System.Random();
        int index = random.Next(0, room.numberOfEdges - 1);
        Vector2 point1 = room.vertices[index], point2 = room.vertices[(index + 1) % room.numberOfEdges];
        Vector2 pointOnEdge, direction;
        if (except != null && GeneralLayoutRoom.isPointOnLine(except ?? Vector2.zero, point1, point2))
        {
            index = (index + random.Next(1, room.numberOfEdges - 1)) % room.numberOfEdges;
            point1 = room.vertices[index];
            point2 = room.vertices[(index + 1) % room.numberOfEdges];
        }
        direction = point2 - point1;
        pointOnEdge = point1 + (float)random.NextDouble() * direction;
        return (pointOnEdge, new(direction.y, -direction.x));
    }

    private void drawGeneralLayoutRoom(GeneralLayoutRoom room, bool drawNumbers = false)
    {
        for (int i = 0; i < room.numberOfEdges; i++)
        {
            Gizmos.DrawLine(Vector2At(room.vertices[i], 0), Vector2At(room.vertices[(i+1)%room.numberOfEdges], 0));
            if(drawNumbers)
                DrawString(i.ToString(), Vector2At(room.vertices[i], 0));
        }
    }

    public static Vector3 Vector2At(Vector2 v2, float y)
    {
        return new Vector3(v2.x, y, v2.y);
    }

    // from https://stackoverflow.com/questions/59449628/check-when-two-vector3-lines-intersect-unity3d
    public static bool LineLineIntersection(out Vector3 intersection, Vector3 linePoint1,
        Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
    {

        Vector3 lineVec3 = linePoint2 - linePoint1;
        Vector3 crossVec1and2 = Vector3.Cross(lineVec1, lineVec2);
        Vector3 crossVec3and2 = Vector3.Cross(lineVec3, lineVec2);

        float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);

        //is coplanar, and not parallel
        if (Mathf.Abs(planarFactor) < 0.0001f
                && crossVec1and2.sqrMagnitude > 0.0001f)
        {
            float s = Vector3.Dot(crossVec3and2, crossVec1and2)
                    / crossVec1and2.sqrMagnitude;
            intersection = linePoint1 + (lineVec1 * s);
            return true;
        }
        else
        {
            intersection = Vector3.zero;
            return false;
        }
    }

    public static bool LineLineIntersection(out Vector2 intersectionV2, Vector2 a1V2,
        Vector2 a2V2, Vector2 b1V2, Vector2 b2V2)
    {
        Vector3 a1 = Vector2At(a1V2, 0), 
                a2 = Vector2At(a2V2, 0),
                b1 = Vector2At(b1V2, 0),
                b2 = Vector2At(b2V2, 0),
                aDiff = Vector2At(a2V2 - a1V2, 0), 
                bDiff = Vector2At(b2V2 - b1V2, 0);
        if (LineLineIntersection(out Vector3 intersection, a1, aDiff, b1, bDiff))
        {
            float aSqrMagnitude = aDiff.sqrMagnitude;
            float bSqrMagnitude = bDiff.sqrMagnitude;

            if ((intersection - a1).sqrMagnitude <= aSqrMagnitude
                 && (intersection - a2).sqrMagnitude <= aSqrMagnitude
                 && (intersection - b1).sqrMagnitude <= bSqrMagnitude
                 && (intersection - b2).sqrMagnitude <= bSqrMagnitude)
            {
                // there is an intersection between the two segments and 
                //   it is at intersection
                intersectionV2 = new(intersection.x, intersection.z);
                return true;
            }
        }
        intersectionV2 = Vector2.zero;
        return false;
    }

        public static float randomFloat(float min, float max)
    {
        System.Random random = new System.Random();
        return (float)random.NextDouble() * (max - min) + min;
    }
    #endregion

    public static void DrawString(string text, Vector3 worldPos, Color? textColor = null, Color? backColor = null)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.BeginGUI();
        var restoreTextColor = GUI.color;
        var restoreBackColor = GUI.backgroundColor;

        GUI.color = textColor ?? Color.white;
        GUI.backgroundColor = backColor ?? Color.black;

        var view = UnityEditor.SceneView.currentDrawingSceneView;
        if (view != null && view.camera != null)
        {
            Vector3 screenPos = view.camera.WorldToScreenPoint(worldPos);
            if (screenPos.y < 0 || screenPos.y > Screen.height || screenPos.x < 0 || screenPos.x > Screen.width || screenPos.z < 0)
            {
                GUI.color = restoreTextColor;
                UnityEditor.Handles.EndGUI();
                return;
            }
            Vector2 size = GUI.skin.label.CalcSize(new GUIContent(text));
            var r = new Rect(screenPos.x - (size.x / 2), -screenPos.y + view.position.height + 4, size.x, size.y);
            GUI.Box(r, text, EditorStyles.numberField);
            GUI.Label(r, text);
            GUI.color = restoreTextColor;
            GUI.backgroundColor = restoreBackColor;
        }
        UnityEditor.Handles.EndGUI();
#endif
    }
}
