using Assets.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class LayoutCreator : MonoBehaviour
{
    public static GeneralLayoutRoom playArea;
    public RoomGeneratorOptions roomGeneratorOptions;
    public bool testRoom = false;
    public List<Vector2> testRoomVertices = new List<Vector2>() { new(), new(), new(), new() };
    public Node currentRoom;

    static Dictionary<Node, RoomDebug> roomDebugs = new Dictionary<Node, RoomDebug>();
    static LayoutCreator instance;
    bool switchedRoom = false;

    // Start is called before the first frame update
    void Start()
    {
        instance = this;
        roomGeneratorOptions = gameObject.GetComponent<RoomGeneratorOptions>();
        playArea = new GeneralLayoutRoom(new List<Vector2>() { Vector2.zero, new(0, roomGeneratorOptions.playArea.y),
            new(roomGeneratorOptions.playArea.x, roomGeneratorOptions.playArea.y), new(roomGeneratorOptions.playArea.x, 0) }); ;

        buildPlayArea(roomGeneratorOptions.playArea, roomGeneratorOptions.playAreaWallHeight);
        setUpPlayerPosition(roomGeneratorOptions.playerStartingPoint);
        currentRoom = createRandomRoom(new Vector2(0, 0), Vector2.up, null, null, roomGeneratorOptions, testRoom ? testRoomVertices : null);
        currentRoom.gameObject.SetActive(true);
        setFollowingRooms(currentRoom, true);
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

    public void regenerateLayout()
    {
        if (playArea != null)
        {
            GameObject roomsParent = GameObject.Find("Rooms");
            for (int i = 0; i < roomsParent.transform.childCount; i++)
            {
                Destroy(roomsParent.transform.GetChild(i).gameObject);
            }
            currentRoom = createRandomRoom(new Vector2(0, 0), Vector2.up, null, null, gameObject.GetComponent<RoomGeneratorOptions>(), testRoom ? testRoomVertices : null);
            currentRoom.gameObject.SetActive(true);
            setFollowingRooms(currentRoom, true);
        }
    }

    public void goNextRoom(int doorNumber)
    {
        goNextRoom(currentRoom.doors[doorNumber]);
    }


    public void goNextRoom(Door door)
    {
        currentRoom.gameObject.SetActive(false);
        setFollowingRooms(currentRoom, false);
        currentRoom = door.nextNode;
        currentRoom.gameObject.SetActive(true);
        setFollowingRooms(currentRoom, true);
        switchedRoom = door;
    }

    private void setFollowingRooms(Node room, bool active)
    {
        if (!roomGeneratorOptions.renderNextRoomsAlready)
            return;
        foreach (Door otherDoor in room.doors)
        {
            if(otherDoor.nextNode != null)
                otherDoor.nextNode.gameObject.SetActive(active);
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
            else get().goNextRoom(door);
        }
    }

    public void redraw()
    {
        //roomSegments.Clear();
        //connectPoints(sampledPoints);
        if (roomDebugs.TryGetValue(currentRoom, out RoomDebug roomDebug))
        { 
            createRandomRoomInternal(roomDebug.currentGeneralLayoutRooms, null, null, roomGeneratorOptions);
        }
    }

    private void OnDrawGizmos()
    {
        if (roomGeneratorOptions == null)
            return;
        if (roomDebugs.TryGetValue(currentRoom, out RoomDebug roomDebug))
        {
            if (roomGeneratorOptions.showGeneralLayoutRooms)
            {
                Gizmos.color = Color.blue;
                foreach (GeneralLayoutRoom room in roomDebug.currentGeneralLayoutRooms)
                {
                    drawGeneralLayoutRoom(room);
                }
            }
            if(roomGeneratorOptions.showBigRoom && roomDebug.currentBigRoom != null)
            {
                Gizmos.color = Color.red;
                drawGeneralLayoutRoom(roomDebug.currentBigRoom, roomGeneratorOptions.showVertexNumbers);
            }
            if (roomGeneratorOptions.showSamplePoints && roomDebug.currentSampledPoints.Count > 0) 
            { 
                for(int i=0; i< roomDebug.currentSampledPoints.Count; i++)
                {
                    Vector3 point = Vector2At(roomDebug.currentSampledPoints.ElementAt(i), 0);
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
                List<BezierRoomSegment> filteredSegments = currentRoom.segments.ToList().Where(x => x.GetType() == typeof(BezierRoomSegment)).Select(x => (BezierRoomSegment)x).ToList();
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
        roomGeneratorOptions.playerTransform.Translate(startPosition);
    }

    public static Node createRandomRoom(Vector2 startingPoint, Vector2 firstRhythmDirection, Node previousRoom, Door previousDoor, RoomGeneratorOptions options, List<Vector2> testVertices = null)
    {
        /* Steps:
            0. Have Starting Point
            1. Map out general room layout with Primitives
            2. Sample points along general layout with random offsets
            3. Connect points via different connector patterns
            4. Draw mesh    */
        System.Random random = new();

        // first room from starting point
        LinkedList<GeneralLayoutRoom> generalLayoutRooms = new LinkedList<GeneralLayoutRoom>();
        if(testVertices == null)
        {
            float minimumWidth = options.doorWidth; //first room wall (with door back) should at least contain the door -> be broad enough to fit th edoor
            GeneralLayoutRoom prevRoom = createRandomGeneralLayoutRoom(startingPoint, firstRhythmDirection, minimumWidth, options, testVertices);
            generalLayoutRooms.AddLast(prevRoom);

            // create next room(s) with starting point on previous room's edge but inside playarea
            for (int i = 0; i < options.maximumRoomSize; i++)
            {
                if (i < options.minimumRoomSize || random.NextDouble() < Math.Pow(options.probabilityOfNextRoom * options.probabilityMultiplierPerNextRoom, i - options.minimumRoomSize))
                {
                    if (createNextRandomGeneralLayoutRoom(prevRoom, previousDoor, options, out prevRoom, testVertices))
                    {
                        generalLayoutRooms.AddLast(prevRoom);
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
            generalLayoutRooms.AddLast(createRandomGeneralLayoutRoom(startingPoint, firstRhythmDirection, 0, options, testVertices));
        }

        return createRandomRoomInternal(generalLayoutRooms, previousRoom, previousDoor, options);
    }

    // to debug creation of bigRoom
    private static Node createRandomRoomInternal(LinkedList<GeneralLayoutRoom> generalLayoutRooms, Node previousRoom, Door previousDoor, RoomGeneratorOptions options)
    {
        // create one big room from generalLayoutRooms
        GeneralLayoutRoom bigRoom = createBigRoom(generalLayoutRooms);

        // sample points
        LinkedList<Vector2> sampledPoints = samplePoints(bigRoom, options);

        // connect points
        LinkedList<RoomSegment> roomSegments = connectPoints(sampledPoints, options);

        GameObject roomGameObject = new GameObject("Room" + Time.fixedTime);
        roomGameObject.transform.parent = GameObject.Find("Rooms").transform;
        roomGameObject.SetActive(false);

        Node room = roomGameObject.AddComponent<Node>();
        room.setupNode(roomSegments, previousRoom, previousDoor, options, CollidedWithDoor);
        roomDebugs.Add(room, new RoomDebug(generalLayoutRooms, bigRoom, sampledPoints));

        // generate doors
        room.generateDoors();

        // draw mesh
        room.createMesh(options.playAreaWallHeight, options.roomMaterial);

        return room;
    }

    private static LinkedList<RoomSegment> connectPoints(LinkedList<Vector2> points, RoomGeneratorOptions roomGeneratorOptions)
    {
        if (roomGeneratorOptions.type == LayoutType.arcs)
            return connectPointsByAllArcs(points, roomGeneratorOptions);
        else if (roomGeneratorOptions.type == LayoutType.straights)
            return connectPointsByStraights(points);
        else if (roomGeneratorOptions.type == LayoutType.beziers)
            return connectPointsByBeziers(points, roomGeneratorOptions);
        return new LinkedList<RoomSegment>();
    }

    private static LinkedList<RoomSegment> connectPointsByAllArcs(LinkedList<Vector2> points, RoomGeneratorOptions roomGeneratorOptions)
    {
        LinkedList<RoomSegment> result = new LinkedList<RoomSegment>();
        for (int i = 0; i < points.Count; i++)
        {
            result.AddLast(new ArcRoomSegment(points.ElementAt(i), points.ElementAt((i + 1) % points.Count), roomGeneratorOptions.invertAfterEachPoint ? i % 2 == 0 : true));
        }
        return result;
    }

    private static LinkedList<RoomSegment> connectPointsByStraights(LinkedList<Vector2> points)
    {
        LinkedList<RoomSegment> result = new LinkedList<RoomSegment>();
        for (int i = 0; i < points.Count; i++)
        {
            result.AddLast(new StraightRoomSegment(points.ElementAt(i), points.ElementAt((i + 1) % points.Count)));
        }
        return result;
    }

    private static LinkedList<RoomSegment> connectPointsByBeziers(LinkedList<Vector2> points, RoomGeneratorOptions roomGeneratorOptions)
    {
        LinkedList<RoomSegment> result = new LinkedList<RoomSegment>();
        System.Random random = new System.Random();
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
            result.AddLast(roomSegment);
        }
        return result;
    }

    private static Vector2 invertVector(Vector2 vec)
    {
        return new(vec.y, -vec.x);
    }

    private static Vector2 rotateVector(Vector2 start, Vector2 towards, float angle)
    {
        start.Normalize();

        Vector3 axis = Vector3.Cross(Vector2At(start, 0), Vector2At(towards, 0));

        if (axis == Vector3.zero) axis = Vector3.back;

        Vector3 rotated = Quaternion.AngleAxis(angle, axis) * start;
        return new(rotated.x, rotated.z);
        /*double x = start.x * Math.Cos(angle) - start.y * Math.Sin(angle), y = start.x * Math.Sin(angle) + start.y * Math.Cos(angle);
        return new((float)x, (float)y);*/
    }

    private static LinkedList<Vector2> samplePoints(GeneralLayoutRoom room, RoomGeneratorOptions options)
    {
        LinkedList<Vector2> sampledPoints = new LinkedList<Vector2>();
        System.Random random = new System.Random();
        if (options.useOnlyVertices)
        {
            room.vertices.ForEach(x => sampledPoints.AddLast(x));
        }
        else
        {
            for(int i=0; i < room.numberOfEdges; i++)
            {
                int numberOfSamplePoints = random.Next(1, options.maxNumberOfSamplePointsPerEdge);
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
                    sampledPoints.AddLast(newPoint + orthogonalDirection * randomFloat(-options.maxMagnitudeOfSamplePointsGoingAway, options.maxMagnitudeOfSamplePointsGoingAway));
                    lastPoint = newPoint;
                }
            }
        }
        return sampledPoints;
    }

    public static GeneralLayoutRoom createBigRoom(LinkedList<GeneralLayoutRoom> generalLayoutRooms)
    {
        return new GeneralLayoutRoom(createBigRoomRec(0, new List<Vector2>(), generalLayoutRooms));
    }

    private static List<Vector2> createBigRoomRec(int roomIndex, List<Vector2> bigRoomVertices, LinkedList<GeneralLayoutRoom> generalLayoutRooms, int startWith=0)
    {
        for (int i = 0; i < generalLayoutRooms.ElementAt(roomIndex).numberOfEdges; i++)
        {
            GeneralLayoutRoom room = generalLayoutRooms.ElementAt(roomIndex);
            int iAdjusted = (i + startWith) % room.numberOfEdges;
            Vector2 newVertex = room.vertices[iAdjusted];
            bool skip = false;
            foreach(Vector2 v in bigRoomVertices)
            {
                skip = skip || v == newVertex;
            }
            if(skip)
            {
                continue;
            }
            bigRoomVertices.Add(room.vertices[iAdjusted]);
            if (roomIndex + 1 < generalLayoutRooms.Count)
            {
                GeneralLayoutRoom nextRoom = generalLayoutRooms.ElementAt(roomIndex + 1);

                if (room.isOnSpecificEdge(nextRoom.vertices[0], iAdjusted) || room.isOnSpecificEdge(nextRoom.vertices[1], iAdjusted))
                {
                    // if point 0 or 4 of next room is between last added point and next-to-add point, add next room first
                    bigRoomVertices = createBigRoomRec(roomIndex + 1, bigRoomVertices, generalLayoutRooms, 1);
                }
            }
        }
        return bigRoomVertices;
    }

    private static bool createNextRandomGeneralLayoutRoom(GeneralLayoutRoom previousRoom, Door previousDoor, RoomGeneratorOptions options, 
        out GeneralLayoutRoom nextRoom, List<Vector2> testRoomVertices = null)
    {
        (Vector2 nextStartingPoint, Vector2 nextRhythmDirection) = getPointOnRoomEdge(previousRoom, previousDoor == null ? null : previousDoor.getPosition());
        nextRoom = null;

        bool maxIterationsExceeded = true;
        for (int i = 0; i < 10; i++)
        {
            if (!playArea.isInside(nextStartingPoint) || !playArea.isInside(nextRhythmDirection.normalized * options.lengthInRhythmDirectionWherePlayAreaCannotEnd))
            {
                (nextStartingPoint, nextRhythmDirection) = getPointOnRoomEdge(previousRoom, previousDoor == null ? null : previousDoor.getPosition());
            }
            else
            {
                maxIterationsExceeded = false;
                break;
            }
        }
        if (maxIterationsExceeded)
        {
            Debug.Log("Iterations exceeded for finding point on previous room where next room can be joined.");
            return false;
        }

        nextRoom = createRandomGeneralLayoutRoom(nextStartingPoint, nextRhythmDirection, 0, options, testRoomVertices);
        return true;
    }


    /// <summary>
    /// Returns a random point on a random edge of the given room and a vector perpendicular to the edge
    /// </summary>
    /// <param name="room"></param>
    /// <returns></returns>
    private static (Vector2, Vector2) getPointOnRoomEdge(GeneralLayoutRoom room, Vector2? except = null)
    {
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

    private static GeneralLayoutRoom createRandomGeneralLayoutRoom(Vector2 startingPoint, Vector2 rhythmDirection, float minimumWidth, RoomGeneratorOptions options, List<Vector2> testRoomVertices = null)
    {
        System.Random random = new System.Random();
        Vector2 point1, point2, point3, point4, rhythmDirectionPerpendicular;
        float depth;
        if (testRoomVertices == null)
        {
            float sign = (random.Next(0, 1) - 0.5f) * 2;
            // width in one direction at least minimumWidth / 2 (for door) but if minimumGeneralLayoutWidth (minus other side of the door) is bigger, then it should be at least that
            float minWidth1 = (options.minimumGeneralLayoutWidth - minimumWidth / 2) < minimumWidth / 2 ? (options.minimumGeneralLayoutWidth - minimumWidth / 2) : minimumWidth / 2; 
            float width1 = randomFloat(minWidth1, options.maximumGeneralLayoutRoomSize); //TODO: Can room be bigger than maximumGeneralLayoutRoomSize with this approach?

            float minWidth2 = minimumWidth / 2;
            if (width1 < (options.minimumGeneralLayoutWidth - minimumWidth / 2))
            {
                float atLeast = options.minimumGeneralLayoutWidth - width1;
                minWidth2 = atLeast < minWidth2 ? minWidth2 : atLeast;
            }
            float width2 = randomFloat(minWidth2, options.maximumGeneralLayoutRoomSize);
            depth = randomFloat(options.minimumGeneralLayoutWidth, options.maximumGeneralLayoutRoomSize);
            rhythmDirection.Normalize();

            rhythmDirectionPerpendicular = new(sign * rhythmDirection.y, -sign * rhythmDirection.x);

            // Start from startingPoint and extrude in any direction perpendicular to rhythmDirection
            point1 = startingPoint + rhythmDirectionPerpendicular.normalized * width1;

            // Second point is extruded in other direction of first point (so door lays somewhere in between those two points and then some distance back to give room area
            point2 = startingPoint + new Vector2(-rhythmDirectionPerpendicular.x, -rhythmDirectionPerpendicular.y).normalized * width2;
        }
        else
        {
            point1 = testRoomVertices[0];
            point2 = testRoomVertices[1];
            depth = Math.Abs(Vector2.Distance(point2, testRoomVertices[2]));
            rhythmDirectionPerpendicular = new(rhythmDirection.y, -rhythmDirection.x);
        }

        // handle what happens when room goes outside playarea
        Vector2 newPoint1 = handlePointOutside(point1, rhythmDirectionPerpendicular);
        Vector2 newPoint2 = handlePointOutside(point2, rhythmDirectionPerpendicular);

        // if room is too small after moving points inside playArea, we can move the other point to make room bigger again
        float dist = Math.Abs(Vector2.Distance(newPoint1, newPoint2));
        if (dist < options.minimumGeneralLayoutWidth)
        {
            if(newPoint1 != point1)
            {
                newPoint2 = newPoint2 + (newPoint2 - newPoint1).normalized * (options.minimumGeneralLayoutWidth - dist);
            } 
            else if (newPoint2 != point2)
            {
                newPoint1 = newPoint1 + (newPoint1 - newPoint2).normalized * (options.minimumGeneralLayoutWidth - dist);
            }
        }
        point1 = newPoint1;
        point2 = newPoint2;

        // Third and fourth points are just first two points but the depth of the room added (rectangular room)
        point3 = point2 + rhythmDirection * depth;
        point4 = point1 + rhythmDirection * depth;

        // check when extruding in depth if point3 / point4 lay outside of playarea
        Vector2 newPoint3 = handlePointOutside(point3, rhythmDirection);
        Vector2 newPoint4 = handlePointOutside(point4, rhythmDirection);
        // case: point3 was outside but point4 not
        if(point3 != newPoint3 && newPoint4 == point4)
        {
            float newDepth = Math.Abs(Vector2.Distance(point2, newPoint3));
            newPoint4 = point1 + rhythmDirection * newDepth;
            Vector2 newNewPoint4 = handlePointOutside(newPoint4, rhythmDirection);
            point4 = newPoint4;
            newPoint4 = newNewPoint4;
        } 
        // case: point4 was outside but point3 not
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
        // case: both points outside
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

    private static Vector2 handlePointOutside(Vector2 point, Vector2 direction)
    {
        Vector2 pA = playArea.vertices[2];
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

    private static float randomFloat(float min, float max)
    {
        System.Random random = new System.Random();
        return (float)random.NextDouble() * (max - min) + min;
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

    public struct RoomDebug
    {
        public LinkedList<GeneralLayoutRoom> currentGeneralLayoutRooms;
        public GeneralLayoutRoom currentBigRoom;
        public LinkedList<Vector2> currentSampledPoints;

        public RoomDebug(LinkedList<GeneralLayoutRoom> currentGeneralLayoutRooms, GeneralLayoutRoom currentBigRoom, LinkedList<Vector2> currentSampledPoints)
        {
            this.currentGeneralLayoutRooms = currentGeneralLayoutRooms;
            this.currentBigRoom = currentBigRoom;
            this.currentSampledPoints = currentSampledPoints;
        }
    }
}
