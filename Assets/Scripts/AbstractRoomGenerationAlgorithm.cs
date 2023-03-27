﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static LayoutCreator;

namespace Assets.Scripts
{
    public abstract class AbstractRoomGenerationAlgorithm : MonoBehaviour
    {
        public Node currentRoom;

        public abstract void init(RoomGeneratorOptions options, bool testRoom = false, List<Vector2> testRoomVertices = null);
        public abstract void movedThroughDoor(Door door);
        public abstract void redraw(RoomDebug roomDebug, RoomGeneratorOptions options);

        protected static Node createRandomRoom(Vector2 startingPoint, Vector2 firstRhythmDirection, Node previousRoom, Door previousDoor, List<Node> noOverlapRooms,
            RoomGeneratorOptions options, List<Vector2> testVertices = null)
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
            if (testVertices == null)
            {
                float minimumWidth = options.doorWidth; //first room wall (with door back) should at least contain the door -> be broad enough to fit th edoor
                GeneralLayoutRoom prevRoom = createRandomGeneralLayoutRoom(startingPoint, firstRhythmDirection, minimumWidth, noOverlapRooms, options, testVertices);
                if(prevRoom == null)
                {
                    return null;
                }
                generalLayoutRooms.AddLast(prevRoom);

                // create next room(s) with starting point on previous room's edge but inside playarea
                for (int i = 0; i < options.maximumRoomSize; i++)
                {
                    if (i < options.minimumRoomSize || random.NextDouble() < Math.Pow(options.probabilityOfNextRoom * options.probabilityMultiplierPerNextRoom, i - options.minimumRoomSize))
                    {
                        if (createNextRandomGeneralLayoutRoom(prevRoom, previousDoor, noOverlapRooms, options, out prevRoom, testVertices))
                        {
                            generalLayoutRooms.AddLast(prevRoom);
                        }
                        else
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
                generalLayoutRooms.AddLast(createRandomGeneralLayoutRoom(startingPoint, firstRhythmDirection, 0, noOverlapRooms, options, testVertices));
            }

            return createRandomRoomInternal(generalLayoutRooms, previousRoom, previousDoor, options);
        }

        // to debug creation of bigRoom
        protected static Node createRandomRoomInternal(LinkedList<GeneralLayoutRoom> generalLayoutRooms, Node previousRoom, Door previousDoor, RoomGeneratorOptions options)
        {
            // create one big room from generalLayoutRooms
            GeneralLayoutRoom bigRoom = createBigRoom(generalLayoutRooms);

            // sample points
            LinkedList<Vector2> sampledPoints = samplePoints(bigRoom, options);

            // connect points
            LinkedList<RoomSegment> roomSegments = connectPoints(sampledPoints, options);

            GameObject roomGameObject = new GameObject("Room" + Time.fixedTime);
            var roomsGO = GameObject.Find("Rooms");
            if(roomsGO == null)
            {
                roomsGO = new GameObject("Rooms");
            }
            roomGameObject.transform.parent = roomsGO.transform;
            roomGameObject.SetActive(false);

            Node room = roomGameObject.AddComponent<Node>();
            RoomDebug debug = new RoomDebug(generalLayoutRooms, bigRoom, sampledPoints);
            room.setupNode(roomSegments, previousRoom, previousDoor, debug, options, CollidedWithDoor);
            RoomDebugs.Add(room, debug);

            // generate doors
            room.generateDoors();

            // draw mesh
            room.createMesh(options.playAreaWallHeight, options.roomMaterial);

            return room;
        }
        protected static LinkedList<RoomSegment> connectPoints(LinkedList<Vector2> points, RoomGeneratorOptions roomGeneratorOptions)
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
                    prevEnd2Start = points.ElementAt((i - 1) % points.Count) - startPoint;
                Vector2 startTangent = rotateVector(start2End, invertVector(start2End), random.Next(roomGeneratorOptions.bezierMaxTangentAngle * 2) - roomGeneratorOptions.bezierMaxTangentAngle);
                Vector2 end2Start = (startPoint - endPoint).normalized;
                Vector2 nextStart2End = points.ElementAt((i + 2) % points.Count) - endPoint;
                Vector2 endTangent = rotateVector(end2Start, invertVector(end2Start), random.Next(roomGeneratorOptions.bezierMaxTangentAngle * 2) - roomGeneratorOptions.bezierMaxTangentAngle);
                BezierRoomSegment roomSegment = new BezierRoomSegment(startPoint, endPoint, startPoint + startTangent, endPoint + endTangent);
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
                for (int i = 0; i < room.numberOfEdges; i++)
                {
                    int numberOfSamplePoints = random.Next(1, options.maxNumberOfSamplePointsPerEdge);
                    Vector2 direction = room.vertices[(i + 1) % room.numberOfEdges] - room.vertices[i];
                    float maxDistance = direction.magnitude;
                    direction.Normalize();
                    Vector2 lastPoint = room.vertices[i];
                    for (int j = 0; j < numberOfSamplePoints; j++)
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

        private static GeneralLayoutRoom createBigRoom(LinkedList<GeneralLayoutRoom> generalLayoutRooms)
        {
            return new GeneralLayoutRoom(createBigRoomRec(0, new List<Vector2>(), generalLayoutRooms));
        }

        private static List<Vector2> createBigRoomRec(int roomIndex, List<Vector2> bigRoomVertices, LinkedList<GeneralLayoutRoom> generalLayoutRooms, int startWith = 0)
        {
            for (int i = 0; i < generalLayoutRooms.ElementAt(roomIndex).numberOfEdges; i++)
            {
                GeneralLayoutRoom room = generalLayoutRooms.ElementAt(roomIndex);
                int iAdjusted = (i + startWith) % room.numberOfEdges;
                Vector2 newVertex = room.vertices[iAdjusted];
                bool skip = false;
                foreach (Vector2 v in bigRoomVertices)
                {
                    skip = skip || v == newVertex;
                }
                if (skip)
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
                        room.connectionToNext = room.vertices[iAdjusted] + (bigRoomVertices.Last() - room.vertices[iAdjusted]) / 2;
                        nextRoom.connectionToPrev = room.connectionToNext;
                    }
                }
            }
            return bigRoomVertices;
        }

        private static bool createNextRandomGeneralLayoutRoom(GeneralLayoutRoom previousRoom, Door previousDoor, List<Node> noOverlapRooms, RoomGeneratorOptions options,
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

            nextRoom = createRandomGeneralLayoutRoom(nextStartingPoint, nextRhythmDirection, 0, noOverlapRooms, options, testRoomVertices);
            return true;
        }

        private static GeneralLayoutRoom createRandomGeneralLayoutRoom(Vector2 startingPoint, Vector2 rhythmDirection, float minimumWidth, List<Node> noOverlapRooms, RoomGeneratorOptions options, List<Vector2> testRoomVertices = null)
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

            bool sPNotOverlaps = handleOverlapRooms(out newPoint1, newPoint1, startingPoint, noOverlapRooms);
            sPNotOverlaps = sPNotOverlaps && handleOverlapRooms(out newPoint2, newPoint2, startingPoint, noOverlapRooms);
            if(!sPNotOverlaps)
            {
                return null; //starting point is already inside some other room -> cant generate room here
            }

            // if room is too small after moving points inside playArea, we can move the other point to make room bigger again
            float dist = Math.Abs(Vector2.Distance(newPoint1, newPoint2));
            if (dist < options.minimumGeneralLayoutWidth)
            {
                if (newPoint1 != point1)
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

            // check when extruding in depth if point3 / point4 lie outside of playarea
            Vector2 newPoint3 = handlePointOutside(point3, rhythmDirection);
            Vector2 newPoint4 = handlePointOutside(point4, rhythmDirection);

            bool a = handleOverlapRooms(out Vector2 newPoint31, newPoint3, startingPoint + rhythmDirection * depth, noOverlapRooms);
            bool b = handleOverlapRooms(out Vector2 newPoint41, newPoint4, startingPoint + rhythmDirection * depth, noOverlapRooms);
            newPoint3 = newPoint31;
            newPoint4 = newPoint41;

            a = a && b;

            if (!a) //startingPoint + depth is inside another room -> reduce depth
            {
                // get new depth by getting intersection point of starting point and room's edge
                handleOverlapRooms(out Vector2 newDepthPoint, startingPoint + rhythmDirection * depth, startingPoint, noOverlapRooms);
                float newDepth = (newDepthPoint - startingPoint).magnitude;
                newPoint3 = point2 + rhythmDirection * newDepth;
                newPoint4 = point1 + rhythmDirection * newDepth;
                
                //check again if new points now maybe are inside other rooms again
                handleOverlapRooms(out newPoint31, newPoint3, newDepthPoint, noOverlapRooms);
                handleOverlapRooms(out newPoint41, newPoint4, newDepthPoint, noOverlapRooms);
                newPoint3 = newPoint31;
                newPoint4 = newPoint41;
            }

            // case: point3 was outside but point4 not
            if (point3 != newPoint3 && newPoint4 == point4)
            {
                float newDepth = Math.Abs(Vector2.Distance(point2, newPoint3));
                newPoint4 = point1 + rhythmDirection * newDepth;
                Vector2 newNewPoint4 = handlePointOutside(newPoint4, rhythmDirection);
                point4 = newPoint4;
                newPoint4 = newNewPoint4;
            }
            // case: point4 was outside but point3 not
            if (point4 != newPoint4 && point3 == newPoint3)
            {
                float newDepth = Math.Abs(Vector2.Distance(point1, newPoint4));
                newPoint3 = point2 + rhythmDirection * newDepth;
                Vector2 newNewPoint3 = handlePointOutside(newPoint3, rhythmDirection);
                if (newPoint3 != newNewPoint3)
                {
                    newDepth = Math.Abs(Vector2.Distance(point2, newNewPoint3));
                    newPoint4 = point1 + rhythmDirection * newDepth;
                    Vector2 newNewPoint4 = handlePointOutside(newPoint4, rhythmDirection);
                    point4 = newNewPoint4;
                }
                point3 = newNewPoint3;
            }
            // case: both points outside
            if (point3 != newPoint3 && point4 != newPoint4)
            {
                float distanceP3 = Math.Abs(Vector2.Distance(point2, newPoint3));
                float distanceP4 = Math.Abs(Vector2.Distance(point1, newPoint4));
                point3 = newPoint3;
                point4 = newPoint4;
                if (distanceP3 < distanceP4)
                {
                    point4 = point1 + rhythmDirection * distanceP3;
                }
                else if (distanceP3 > distanceP4)
                {
                    point3 = point2 + rhythmDirection * distanceP4;
                }
            }

            // what if not go "back" with points 3 and 4 but change width of room (move point 1 and 4 instead of move point 3 and 4)

            return new GeneralLayoutRoom(new List<Vector2>() { point1, point2, point3, point4 });
        }
    }
}