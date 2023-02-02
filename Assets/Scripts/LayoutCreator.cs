using Assets.Scripts;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

public class LayoutCreator : MonoBehaviour
{
    Constants CONSTANTS;
    System.Random random = new System.Random();
    LinkedList<GeneralLayoutRoom> generalLayoutRooms = new LinkedList<GeneralLayoutRoom>();
    GeneralLayoutRoom playArea, bigRoom;
    bool createBigRoomStart = false;
    LinkedList<Vector2> sampledPoints = new LinkedList<Vector2>();
    public RoomGeneratorOptions roomGeneratorOptions;
    public LinkedList<RoomSegment> roomSegments = new LinkedList<RoomSegment>();
    public bool testRoom = false;
    public List<Vector2> testRoomVertices = new List<Vector2>() { new(), new(), new(), new() };

    // Start is called before the first frame update
    void Start()
    {
        Constants constantsComponent = gameObject.GetComponent<Constants>();
        if (constantsComponent != null)
            CONSTANTS = constantsComponent;
        else
            Debug.LogError("LayoutCreator.Start: Could not find Constants component. Make sure both components are attached to the same GameObject.");

        playArea = new GeneralLayoutRoom(new List<Vector2>() { Vector2.zero, new(0, CONSTANTS.playArea.y), new(CONSTANTS.playArea.x, CONSTANTS.playArea.y), new(CONSTANTS.playArea.x, 0) });

        runTests();

        buildPlayArea(CONSTANTS.playArea, CONSTANTS.playAreaWallHeight);
        setUpPlayerPosition(CONSTANTS.playerStartingPoint);
        roomGeneratorOptions = gameObject.GetComponent<RoomGeneratorOptions>();
        createRandomRoom(new Vector2(0, 0), Vector2.up, roomGeneratorOptions);
        //createTestRoom();
    }

    // Update is called once per frame
    void Update()
    {
        /*if(createBigRoomStart)
        {
            bigRoom = createBigRoom();
            createBigRoomStart = false;
        }*/
    }

    public void regenerateLayout()
    {
        generalLayoutRooms.Clear();
        bigRoom = null;
        createBigRoomStart = false;
        sampledPoints.Clear();
        roomSegments.Clear();
        createRandomRoom(new Vector2(0, 0), Vector2.up, gameObject.GetComponent<RoomGeneratorOptions>());
    }

    public void redraw()
    {
        roomSegments.Clear();
        connectPoints(sampledPoints);
    }

    private void runTests()
    {
        GeneralLayoutRoom testArea = new GeneralLayoutRoom(new List<Vector2>() { Vector2.zero, new(0, 10), new(10, 10), new(10, 0) });
        Vector2 inside = new(5, 5);
        Vector2 outside = new(5, 15);
        Assert.IsTrue(testArea.isInside(inside));
        Assert.IsFalse(testArea.isInside(outside));
    }

    private void createTestRoom()
    {
        generalLayoutRooms.Clear();
        /*generalLayoutRooms.AddLast(new GeneralLayoutRoom(new List<Vector2>() { new(-1, 0), new(3, 0), new(3, 1), new(-1, 1) }));
        generalLayoutRooms.AddLast(new GeneralLayoutRoom(new List<Vector2>() { new(2, 1), new(11, 1), new(11, 5), new(2, 5) }));
        generalLayoutRooms.AddLast(new GeneralLayoutRoom(new List<Vector2>() { new(8, 5), new(12, 5), new(12, 9), new(8, 9) }));
        generalLayoutRooms.AddLast(new GeneralLayoutRoom(new List<Vector2>() { new(3, 9), new(10.5f, 9), new(10.5f, 13), new(3, 13) }));*/
        Vector2 point1 = new(1.5f, 1.5f), point2 = new(2.5f, 1.5f);
        roomSegments.AddLast(new BezierRoomSegment(point1, point2, point1+Vector2.up, point2+Vector2.up));
    }

    private void OnDrawGizmos()
    {
        if (roomGeneratorOptions == null)
            return;
        if (roomGeneratorOptions.showGeneralLayoutRooms)
        {
            Gizmos.color = Color.blue;
            foreach (GeneralLayoutRoom room in generalLayoutRooms)
            {
                drawGeneralLayoutRoom(room);
            }
        }
        if(roomGeneratorOptions.showBigRoom && bigRoom != null)
        {
            Gizmos.color = Color.red;
            drawGeneralLayoutRoom(bigRoom, roomGeneratorOptions.showVertexNumbers);
        }
        if (roomGeneratorOptions.showSamplePoints && sampledPoints.Count > 0) 
        { 
            for(int i=0; i<sampledPoints.Count; i++)
            {
                Vector3 point = Vector2At(sampledPoints.ElementAt(i), 0);
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(point, 0.05f);
                if(roomGeneratorOptions.showSamplePointNumbers)
                {
                    DrawString(i.ToString(), point);
                }
            }
        }
        if(roomGeneratorOptions.showFinishedRoom)
        {
            Gizmos.color = Color.black;
            List<BezierRoomSegment> filteredSegments = roomSegments.ToList().Where(x => x.GetType() == typeof(BezierRoomSegment)).Select(x => (BezierRoomSegment)x).ToList();
            foreach(BezierRoomSegment segment in filteredSegments)
            {
                segment.drawGizmos(roomGeneratorOptions.bezierSubdivisions, roomGeneratorOptions.showBezierTangents);
            }
        }
    }

    private void buildPlayArea(Vector2 playArea, float wallHeight)
    {
        //build play area a bit bigger to stop z-fighting of room and playarea

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
            wall.transform.localScale = new Vector3(1, 1, wallHeight / 10);
        }

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
        Camera.main.transform.Translate(startPosition);
    }

    private void createRandomRoom(Vector2 startingPoint, Vector2 firstRhythmDirection, RoomGeneratorOptions options)
    {
        /* Steps:
            0. Have Starting Point
            1. Map out general room layout with Primitives
            2. Sample points along general layout with random offsets
            3. Connect points via different connector patterns
            4. Draw mesh    */


        // first room from starting point
        if(!testRoom)
        {
            GeneralLayoutRoom prevRoom = createRandomGeneralLayoutRoom(startingPoint, firstRhythmDirection, options);
            generalLayoutRooms.AddLast(prevRoom);
            //Debug.Log(prevRoom.ToString());

            // create next room(s) with starting point on previous room's edge but inside playarea
            for (int i = 0; i < options.maximumRoomSize; i++)
            {
                if (i < options.minimumRoomSize || random.NextDouble() < Math.Pow(options.probabilityOfNextRoom * options.probabilityMultiplierPerNextRoom, i - options.minimumRoomSize))
                {
                    if (createNextRandomGeneralyLayoutRoom(prevRoom, options, out prevRoom))
                    {
                        generalLayoutRooms.AddLast(prevRoom);
                        //Debug.Log(prevRoom.ToString());
                    } else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }
        else
        {
            //generalLayoutRooms.AddLast(new GeneralLayoutRoom(new List<Vector2>() { new(-2, 0), new(4, 0), new(-2, 12), new(4, 12) }));
            generalLayoutRooms.AddLast(createRandomGeneralLayoutRoom(startingPoint, firstRhythmDirection, options));
        }


        // create one big room from generalLayoutRooms
        bigRoom = createBigRoom();

        // sample points
        sampledPoints = samplePoints(bigRoom);

        // connect points
        connectPoints(sampledPoints);

        // generate mesh
        generateMesh(roomSegments);
    }

    private void generateMesh(LinkedList<RoomSegment> segments)
    {
        GameObject gameObject = GameObject.Find("Room");
        Destroy(gameObject);
        Node node;
        gameObject = new GameObject("Room");
        gameObject.transform.parent = transform;
        node = gameObject.AddComponent<Node>();
        node.createNode(segments, CONSTANTS.playAreaWallHeight, roomGeneratorOptions.roomMaterial);
    }

    private void connectPoints(LinkedList<Vector2> points)
    {
        if (roomGeneratorOptions.type == LayoutType.arcs)
            connectPointsByAllArcs(points);
        else if (roomGeneratorOptions.type == LayoutType.straights)
            connectPointsByStraights(points);
        else if (roomGeneratorOptions.type == LayoutType.beziers)
            connectPointsByBeziers(points);
    }

    private void connectPointsByAllArcs(LinkedList<Vector2> points)
    {
        for (int i = 0; i < points.Count; i++)
        {
            roomSegments.AddLast(new ArcRoomSegment(points.ElementAt(i), points.ElementAt((i + 1) % points.Count), roomGeneratorOptions.invertAfterEachPoint ? i % 2 == 0 : true));
        }
    }

    private void connectPointsByStraights(LinkedList<Vector2> points)
    {
        for (int i = 0; i < points.Count; i++)
        {
            roomSegments.AddLast(new StraightRoomSegment(points.ElementAt(i), points.ElementAt((i + 1) % points.Count)));
        }
    }

    private void connectPointsByBeziers(LinkedList<Vector2> points)
    {
        for (int i = 0; i < points.Count; i++)
        {
            Vector2 startPoint = points.ElementAt(i);
            Vector2 endPoint = points.ElementAt((i + 1) % points.Count);
            Vector2 start2End = (endPoint - startPoint).normalized;
            Vector2 prevEnd2Start;
            if (i == 0)
                prevEnd2Start = points.ElementAt(points.Count - 1) - startPoint;
            else
                prevEnd2Start = points.ElementAt((i-1) % points.Count) - startPoint;
            Vector2 startTangent = rotateVector(start2End, invertVector(start2End), random.Next(roomGeneratorOptions.bezierMaxTangentAngle * 2) - roomGeneratorOptions.bezierMaxTangentAngle);
            Vector2 end2Start = (startPoint - endPoint).normalized;
            Vector2 nextStart2End = points.ElementAt((i + 2) % points.Count) - endPoint;
            Vector2 endTangent = rotateVector(end2Start, invertVector(end2Start), random.Next(roomGeneratorOptions.bezierMaxTangentAngle * 2) - roomGeneratorOptions.bezierMaxTangentAngle);
            BezierRoomSegment roomSegment = new BezierRoomSegment(startPoint, endPoint, startPoint+startTangent, endPoint+endTangent);
            roomSegments.AddLast(roomSegment);
        }
    }

    private Vector2 invertVector(Vector2 vec)
    {
        return new(vec.y, -vec.x);
    }

    private Vector2 rotateVector(Vector2 start, Vector2 towards, float angle)
    {
        start.Normalize();

        Vector3 axis = Vector3.Cross(Vector2At(start, 0), Vector2At(towards, 0));

        if (axis == Vector3.zero) axis = Vector3.back;

        Vector3 rotated = Quaternion.AngleAxis(angle, axis) * start;
        return new(rotated.x, rotated.z);
        /*double x = start.x * Math.Cos(angle) - start.y * Math.Sin(angle), y = start.x * Math.Sin(angle) + start.y * Math.Cos(angle);
        return new((float)x, (float)y);*/
    }

    private LinkedList<Vector2> samplePoints(GeneralLayoutRoom room)
    {
        LinkedList<Vector2> sampledPoints = new LinkedList<Vector2>();
        if (roomGeneratorOptions.useOnlyVertices)
        {
            room.vertices.ForEach(x => sampledPoints.AddLast(x));
        }
        else
        {
            for(int i=0; i < room.numberOfEdges; i++)
            {
                int numberOfSamplePoints = random.Next(1, roomGeneratorOptions.maxNumberOfSamplePointsPerEdge);
                Vector2 direction = room.vertices[(i + 1) % room.numberOfEdges] - room.vertices[i];
                float maxDistance = direction.magnitude;
                direction.Normalize();
                Vector2 lastPoint = room.vertices[i];
                for(int j=0; j < numberOfSamplePoints; j++)
                {
                    Vector2 availableDistance = room.vertices[(i + 1) % room.numberOfEdges] - lastPoint;
                    if (availableDistance.magnitude < 0)
                        break;

                    Vector2 newPoint = lastPoint + direction * randomFloat(0, availableDistance.magnitude > maxDistance ? maxDistance : availableDistance.magnitude);
                    Vector2 orthogonalDirection = new(direction.y, -direction.x);
                    sampledPoints.AddLast(newPoint + orthogonalDirection * randomFloat(-roomGeneratorOptions.maxMagnitudeOfSamplePointsGoingAway, roomGeneratorOptions.maxMagnitudeOfSamplePointsGoingAway));
                    lastPoint = newPoint;
                }
            }
        }
        return sampledPoints;
    }

    public GeneralLayoutRoom createBigRoom()
    {
        return new GeneralLayoutRoom(createBigRoomRec(0, new List<Vector2>()));
    }

    private List<Vector2> createBigRoomRec(int roomIndex, List<Vector2> bigRoomVertices, int startWith=0)
    {
        for (int i = 0; i < generalLayoutRooms.ElementAt(roomIndex).numberOfEdges; i++)
        {
            GeneralLayoutRoom room = generalLayoutRooms.ElementAt(roomIndex);
            int iAdjusted = (i + startWith) % room.numberOfEdges;
            bigRoomVertices.Add(room.vertices[iAdjusted]);
            if (roomIndex + 1 < generalLayoutRooms.Count)
            {
                GeneralLayoutRoom nextRoom = generalLayoutRooms.ElementAt(roomIndex + 1);

                if (room.isOnSpecificEdge(nextRoom.vertices[0], iAdjusted) || room.isOnSpecificEdge(nextRoom.vertices[1], iAdjusted))
                {
                    //if ((nextRoom.vertices[1] - room.vertices[iAdjusted+1]).magnitude >  )


                    //int nextStartWith = room.isOnEdge(nextRoom.vertices[0]) & !room.isOnEdge(nextRoom.vertices[1]) ? 1 : 0;
                    int nextStartWith = 1;
                    // if point 0 or 4 of next room is between last added point and next-to-add point, add next room first
                    bigRoomVertices = createBigRoomRec(roomIndex + 1, bigRoomVertices, nextStartWith);
                }
            }
        }
        return bigRoomVertices;
    }

    private bool createNextRandomGeneralyLayoutRoom(GeneralLayoutRoom previousRoom, RoomGeneratorOptions options, out GeneralLayoutRoom nextRoom)
    {
        (Vector2 nextStartingPoint, Vector2 nextRhythmDirection) = getPointOnRoomEdge(previousRoom);
        nextRoom = null;

        bool maxIterationsExceeded = true;
        for (int i = 0; i < 10; i++)
        {
            if (!playArea.isInside(nextStartingPoint) || !playArea.isInside(nextRhythmDirection.normalized * options.lengthInRhythmDirectionWherePlayAreaCannotEnd))
            {
                (nextStartingPoint, nextRhythmDirection) = getPointOnRoomEdge(previousRoom);
            }
            else
            {
                maxIterationsExceeded = false;
                break;
            }
        }
        if (maxIterationsExceeded)
        {
            Debug.LogError("Iterations exceeded for finding point on previous room where next room can be joined.");
            return false;
        }

        nextRoom = createRandomGeneralLayoutRoom(nextStartingPoint, nextRhythmDirection, options);
        return true;
    }

    /// <summary>
    /// Returns a random point on a random edge of the given room and a vector perpendicular to the edge
    /// </summary>
    /// <param name="room"></param>
    /// <returns></returns>
    private (Vector2, Vector2) getPointOnRoomEdge(GeneralLayoutRoom room)
    {
        int index = random.Next(0, room.numberOfEdges-1);
        Vector2 direction = room.vertices[(index + 1) % room.numberOfEdges] - room.vertices[index];
        Vector2 pointOnEdge = room.vertices[index] + (float)random.NextDouble() * direction;
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

    private GeneralLayoutRoom createRandomGeneralLayoutRoom(Vector2 startingPoint, Vector2 rhythmDirection, RoomGeneratorOptions options)
    {
        Vector2 point1, point2, point3, point4, rhythmDirectionPerpendicular;
        float depth;
        if(!testRoom)
        {
            float sign = (random.Next(0, 1) - 0.5f) * 2;
            float width = randomFloat(options.minimumGeneralLayoutWidth, options.maximumGeneralLayoutRoomSize);
            depth = randomFloat(options.minimumGeneralLayoutWidth, options.maximumGeneralLayoutRoomSize);
            float distribution = randomFloat(0, 1); //how much of width lies on left side of entrance compared to right side of entrance
            rhythmDirection.Normalize();

            rhythmDirectionPerpendicular = new(sign * rhythmDirection.y, -sign * rhythmDirection.x);

            // Start from startingPoint and extrude in any direction perpendicular to rhythmDirection
            point1 = startingPoint + rhythmDirectionPerpendicular * width * distribution;

            // Second point is extruded in other direction of first point (so door lays somewhere in between those two points and then some distance back to give room area
            point2 = startingPoint + new Vector2(-rhythmDirectionPerpendicular.x, -rhythmDirectionPerpendicular.y) * (width - width * distribution);
        } 
        else
        {
            point1 = testRoomVertices[0];
            point2 = testRoomVertices[1];
            depth = Math.Abs(Vector2.Distance(point2, testRoomVertices[2]));
            rhythmDirectionPerpendicular = new(rhythmDirection.y, -rhythmDirection.x);
        }

        // handle what happens when room goes outside playarea
        point1 = handlePointOutside(point1, rhythmDirectionPerpendicular);
        point2 = handlePointOutside(point2, rhythmDirectionPerpendicular);

        // Third and fourth points are just first two points but the depth of the room added (rectangular room)
        point3 = point2 + rhythmDirection * depth;
        point4 = point1 + rhythmDirection * depth;

        // check when extruding in depth if point3 / point4 lay outside of playarea
        Vector2 newPoint3 = handlePointOutside(point3, rhythmDirection);
        Vector2 newPoint4 = handlePointOutside(point4, rhythmDirection);
        if(point3 != newPoint3 && newPoint4 == point4)
        {
            float newDepth = Math.Abs(Vector2.Distance(point2, newPoint3));
            newPoint4 = point1 + rhythmDirection * newDepth;
            Vector2 newNewPoint4 = handlePointOutside(newPoint4, rhythmDirection);
            point4 = newPoint4;
            newPoint4 = newNewPoint4;
        } 
        if(point4 != newPoint4 && point3 == newPoint3)
        {
            float newDepth = Math.Abs(Vector2.Distance(point1, newPoint4));
            newPoint3 = point2 + rhythmDirection * newDepth;
            Vector2 newNewPoint3 = handlePointOutside(newPoint3, rhythmDirection);
            if(newPoint3 != newNewPoint3)
            {
                newDepth = Math.Abs(Vector2.Distance(point2, newNewPoint3));
                newPoint4 = point1 + rhythmDirection * newDepth;
                Vector2 newNewPoint4 = handlePointOutside(newPoint4, rhythmDirection);
                point4 = newNewPoint4;
            }
            point3 = newNewPoint3;
        }
        if(point3 != newPoint3 && point4 != newPoint4)
        {
            float distanceP3 = Math.Abs(Vector2.Distance(point2, newPoint3));
            float distanceP4 = Math.Abs(Vector2.Distance(point1, newPoint4));
            point3 = newPoint3;
            point4 = newPoint4;
            if (distanceP3 < distanceP4)
            {
                point4 = point1 + rhythmDirection * distanceP3;
            }
            else if(distanceP3 > distanceP4)
            {
                point3 = point2 + rhythmDirection * distanceP4;
            }
        }

        // what if not go "back" with points 3 and 4 but change width of room (move point 1 and 4 instead of move point 3 and 4)

        return new GeneralLayoutRoom(new List<Vector2>(){ point1, point2, point3, point4 });
    }

    private Vector2 handlePointOutside(Vector2 point, Vector2 direction)
    {
        Vector2 pA = CONSTANTS.playArea;
        if (point.x < 0)
        {
            // calculate crossing point of playarea and room area
            if(LineLineIntersection(out Vector3 intersection, Vector2At(point, 0), Vector2At(direction, 0), Vector3.zero, new Vector3(0, 0, pA.y)))
                point = new(intersection.x, intersection.z);
        }
        if(point.y < 0)
        {
            if(LineLineIntersection(out Vector3 intersection, Vector2At(point, 0), Vector2At(direction, 0), Vector3.zero, new Vector3(pA.x, 0, 0)))
                point = new(intersection.x, intersection.z);
        }
        if(point.x > pA.x)
        {
            if (LineLineIntersection(out Vector3 intersection, Vector2At(point, 0), Vector2At(direction, 0), new Vector3(pA.x, 0, pA.y), new Vector3(0, 0, -pA.y)))
                point = new(intersection.x, intersection.z);
        }
        if(point.y > pA.y)
        {
            if (LineLineIntersection(out Vector3 intersection, Vector2At(point, 0), Vector2At(direction, 0), new Vector3(pA.x, 0, pA.y), new Vector3(-pA.x, 0, 0)))
                point = new(intersection.x, intersection.z);
        }

        return point;
    }

    // from https://stackoverflow.com/questions/59449628/check-when-two-vector3-lines-intersect-unity3d
    private static bool LineLineIntersection(out Vector3 intersection, Vector3 linePoint1,
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

    private float randomFloat(float min, float max)
    {
        return (float)random.NextDouble() * (max - min) + min;
    }

    private Vector2 projectPointToPlayArea(Vector2 point)
    {
        if (point.x < 0)
            point.x = 0;
        if (point.x > CONSTANTS.playArea.x)
            point.x = CONSTANTS.playArea.x;
        if (point.y < 0)
            point.y = 0;
        if (point.y > CONSTANTS.playArea.y)
            point.y = CONSTANTS.playArea.y;
        return point;
    }

    public static void DrawString(string text, Vector3 worldPos, Color? textColor = null, Color? backColor = null)
    {
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
    }

}
