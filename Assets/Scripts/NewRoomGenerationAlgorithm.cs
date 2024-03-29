﻿using System;
using System.Collections.Generic;
using UnityEngine;
using static LayoutCreator;

namespace Assets.Scripts
{
    public class NewRoomGenerationAlgorithm : AbstractRoomGenerationAlgorithm
    {
        private List<Node> currentlyShownRooms = new List<Node>();
        private List<Tuple<Vector3, Vector3, bool>> raycasts = new List<Tuple<Vector3, Vector3, bool>>();
        private Transform playerTransform;
        private RoomGeneratorOptions options;
        private List<Vector2[]> testRooms;
        private Vector3 lastPlayerLocation;
        private float positionThreshold = 0.01f;

        // Update is called once per frame
        void Update()
        {
            if(lastPlayerLocation == null || Vector3.Distance(lastPlayerLocation, playerTransform.position) > positionThreshold)
            {
                lastPlayerLocation = playerTransform.position;
                
                raycasts.Clear();

                for (int i = 0; i < currentlyShownRooms.Count; i++)
                {
                    Node node = currentlyShownRooms[i];
                    bool isVisible = false;
                    var doorsCopy = new List<Door>(node.doors);
                    foreach(Door door in doorsCopy)
                    {
                        bool doorVisible = door.Equals(get().switchedRoom);
                        doorVisible |= isDoorVisible(door);
                        if (doorVisible)
                        {
                            var roomGenerated = true;
                            if(door.nextNode == null)
                                roomGenerated = createRoomForDoor(door);
                            if(roomGenerated && !currentlyShownRooms.Contains(door.nextNode))
                            {
                                setActiveRoom(door.nextNode, true);
                            }
                            isVisible = true;
                        } 
                    }
                    if (!isVisible && !node.Equals(currentRoom))
                        setActiveRoom(node, false);
                }
            }
        }

        public override void init(RoomGeneratorOptions options, bool testRoom = false, List<Vector2[]> testRoomVertices = null)
        {
            this.options = options;
            currentlyShownRooms = new List<Node>();
            playerTransform = options.playerTransform;
            currentRoom = createRandomRoom(new Vector2(0, 0), Vector2.up, null, null, currentlyShownRooms, options, testRoom ? testRoomVertices[0] : null);
            if (testRoom)
                testRooms = testRoomVertices;
            setActiveRoom(currentRoom, true);
            //createNextRooms(currentRoom);
        }

        private void createNextRooms(Node currentRoom)
        {
            foreach (Door door in currentRoom.doors)
            {
                if (isDoorVisible(door) && door.nextNode == null)
                {
                    createRoomForDoor(door);
                }
            }
        }

        private bool createRoomForDoor(Door door)
        {
            if (door == null)
            {
                Debug.LogError("door was null");
                return false;
            }
            Vector2 p2ToP1 = door.getPoint2() - door.getPoint1();
            door.nextNode = createRandomRoom(new Vector2(door.position.x, door.position.z), new Vector2(p2ToP1.y, -p2ToP1.x).normalized, door.previousNode ?? currentRoom, door, currentlyShownRooms, options, 
                testRooms != null && testRooms.Count < door.previousNode.depth + 1 ? testRooms[door.previousNode.depth+1] : null);
            if(door.nextNode == null)
            {
                Node node = door.previousNode;
                Debug.LogError("Couldn't create room for door, removing door "+door.name);
                door.previousNode.doors.Remove(door);
                Destroy(door);
                node.createMesh(options.playAreaWallHeight, options.roomMaterial);
                return false;
            }
            return true;
        }

        private void setActiveRoom(Node room, bool active)
        {
            if (room == null)
            {
                Debug.LogError("setActiveRoom: room was null??");
                return;
            }
            room.gameObject.SetActive(active);
            if(active)
                currentlyShownRooms.Add(room);
            else if(!room.Equals(currentRoom))
                currentlyShownRooms.Remove(room);
        }

        private bool isDoorVisible(Door door)
        {
            return raycastingVisibilityCheck(door);
        }

        private bool raycastingVisibilityCheck(Door door)
        {
            Vector3 position = playerTransform.position;
            Vector3 toMiddle = (door.point2 - door.point1).normalized;
            List<Vector3> directions = new List<Vector3> { door.position - position, door.point1 - position + toMiddle * 0.01f, door.point2 - position - toMiddle * 0.01f };
            directions.ForEach(x => x.Normalize());
            float maxDistance = (float)Math.Sqrt(Math.Pow(options.playArea.x, 2) + Math.Pow(options.playArea.y, 2));
            foreach (Vector3 direction in directions)
            {
                bool correctHitPreviousLayer = false;
                for (int i = currentRoom.LayerNumber - 1; i < currentRoom.LayerNumber + options.depthForward; i++)
                {
                    DoorHit doorHit = hittingDoorAtDepth(door, i, position, direction, maxDistance);
                    if (doorHit == DoorHit.CORRECT_DOOR)
                    {
                        if (i == currentRoom.LayerNumber - 1)
                        {
                            //door was hit on previous layer, but still walls could be hit on current layer, so we need to check current layer still
                            correctHitPreviousLayer = true;
                            continue;
                        }
                        return true;
                    }
                    else if (doorHit == DoorHit.WALLS)
                    {
                        if (i == currentRoom.LayerNumber - 1) continue; //go to current layer if walls of previous room are hit (through door of current room)
                        else break; //else this ray won't hit any other door
                    }
                    else if (correctHitPreviousLayer)
                    {
                        //if not walls were hit on current layer, then the hit on previous layer was correct 
                        return true;
                    }
                }
            }
            return false;
        }

        private enum DoorHit
        {
            NOTHING,
            WALLS,
            OTHER_DOOR,
            CORRECT_DOOR
        }

        private DoorHit hittingDoorAtDepth(Door door, int depth, Vector3 position, Vector3 direction, float maxDistance)
        {
            int depthMask = 1 << depth;
            try
            {
                if (Physics.Raycast(position, direction, out RaycastHit hit, maxDistance, depthMask))
                {
                    Door respectiveDoor = null;
                    if (door.nextNode != null)
                        respectiveDoor = door.nextNode.doors.Find(x => x.position == door.position);
                    //somehow ignore repsective doors at other room (why does this door not have nextNode?)
                    if (hit.transform == door.transform || (respectiveDoor != null && hit.transform == respectiveDoor.transform))
                    {
                        //Debug.Log("Door is visible");
                        raycasts.Add(new Tuple<Vector3, Vector3, bool>(position, hit.point, true));
                        return DoorHit.CORRECT_DOOR;
                    }
                    else if (hit.transform.gameObject.GetComponent<Door>() != null)
                    {
                        if (hittingDoorAtDepth(door, depth, hit.point, direction, maxDistance) == DoorHit.CORRECT_DOOR)
                        {
                            raycasts.Add(new Tuple<Vector3, Vector3, bool>(position, hit.point, true));
                            return DoorHit.CORRECT_DOOR;
                        }
                        raycasts.Add(new Tuple<Vector3, Vector3, bool>(position, hit.point, false));
                        return DoorHit.OTHER_DOOR;
                    }
                    raycasts.Add(new Tuple<Vector3, Vector3, bool>(position, hit.point, false));
                    return DoorHit.WALLS;
                }
            }
            catch(Exception e)
            {
                Debug.LogError("Exception while raycasting: " + e.Message + "\n" + depthMask + " " + maxDistance + "\n" + position + "\n" + direction);
            }
            return DoorHit.NOTHING;
        }

        public override void movedThroughDoor(Door door)
        {
            Debug.Log("Moved through door " + door.name + " to room " + door.nextNode.name);
            currentRoom.setAllDoorsActive(false);
            currentRoom = door.nextNode;
            currentRoom.setAllDoorsActive(true);
            get().switchedRoom = door;
        }

        public override void redraw(RoomDebug roomDebug, RoomGeneratorOptions options)
        {
            throw new System.NotImplementedException();
        }

        private void OnDrawGizmos()
        {
            if (currentRoom != null && options.debugGenerator)   
            {
                foreach(var raycast in raycasts)
                {
                    if(raycast.Item3)
                        Gizmos.color = Color.yellow;
                    else 
                        Gizmos.color = Color.red;
                    Gizmos.DrawLine(raycast.Item1, raycast.Item2);
                }
            }
        }

        // Use this for initialization
        void Start()
        {   

        }
    }
}