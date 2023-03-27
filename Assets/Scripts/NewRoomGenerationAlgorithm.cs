using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static LayoutCreator;

namespace Assets.Scripts
{
    public class NewRoomGenerationAlgorithm : AbstractRoomGenerationAlgorithm
    {
        private List<Node> currentlyShownRooms = new List<Node>();
        private List<Tuple<Vector3, Vector3>> raycasts = new List<Tuple<Vector3, Vector3>>();
        private Transform playerTransform;
        private RoomGeneratorOptions options;

        // Update is called once per frame
        void Update()
        {
            raycasts.Clear();

            for (int i = 0; i < currentlyShownRooms.Count; i++)
            {
                Node node = currentlyShownRooms[i];
                bool isVisible = false;
                foreach(Door door in node.doors)
                {
                    bool doorVisible = isDoorVisible(door);
                    if (doorVisible)
                    {
                        if(door.nextNode == null)
                            createRoomForDoor(door);
                        if(!currentlyShownRooms.Contains(door.nextNode))
                        {
                            setActiveRoom(door.nextNode, true);
                        }
                    } 
                    isVisible = isVisible || doorVisible;
                }
                if (!isVisible)
                    setActiveRoom(node, false);
            }
        }

        public override void init(RoomGeneratorOptions options, bool testRoom = false, List<Vector2> testRoomVertices = null)
        {
            this.options = options;
            currentlyShownRooms = new List<Node>();
            playerTransform = options.playerTransform;
            currentRoom = createRandomRoom(new Vector2(0, 0), Vector2.up, null, null, currentlyShownRooms, options, testRoom ? testRoomVertices : null);
            setActiveRoom(currentRoom, true);
            createNextRooms(currentRoom);
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

        private void createRoomForDoor(Door door)
        {
            Vector2 p2ToP1 = door.getPoint2() - door.getPoint1();
            door.nextNode = createRandomRoom(new Vector2(door.position.x, door.position.z), new Vector2(p2ToP1.y, -p2ToP1.x).normalized, currentRoom, door, currentlyShownRooms, options);
            if(door.nextNode == null)
            {
                Node node = door.previousNode;
                Debug.LogError("Couldn't create room for door, removing door "+door.name);
                door.previousNode.doors.Remove(door);
                Destroy(door);
                node.createMesh(options.playAreaWallHeight, options.roomMaterial);
            }
        }

        private void setActiveRoom(Node room, bool active)
        {
            room.gameObject.SetActive(active);
            if(active) 
                currentlyShownRooms.Add(room);
            else 
                currentlyShownRooms.Remove(room);
        }

        private bool isDoorVisible(Door door)
        {
            Vector3 position = playerTransform.position;
            List<Vector3> directions = new List<Vector3> { door.point1 - position, door.point2 - position, door.position - position };
            float maxDistance = (float) Math.Sqrt(Math.Pow(options.playArea.x, 2) + Math.Pow(options.playArea.y,2));
            foreach (Vector3 direction in directions)
            {
                for(int i = currentRoom.LayerNumber; i<currentRoom.LayerNumber+options.depthForward; i++)
                {
                    DoorHit doorHit = hittingDoorAtDepth(door, i, position, direction, maxDistance);
                    if (doorHit == DoorHit.CORRECT_DOOR)
                    {
                        return true;
                    } 
                    else if(doorHit == DoorHit.WALLS)
                    {
                        break;
                    }
                }
            }
            return false;
        }

        private enum DoorHit
        {
            WALLS,
            OTHER_DOOR,
            CORRECT_DOOR
        }

        private DoorHit hittingDoorAtDepth(Door door, int depth, Vector3 position, Vector3 direction, float maxDistance)
        {
            int depthMask = 1 << depth;
            if (Physics.Raycast(position, direction, out RaycastHit hit, maxDistance, depthMask))
            {
                raycasts.Add(new Tuple<Vector3, Vector3>(position, hit.point));
                Door respectiveDoor = null;
                if (door.nextNode != null)
                    respectiveDoor = door.nextNode.doors.Find(x => x.position == door.position);
                //somehow ignore repsective doors at other room (why does this door not have nextNode?)
                if (hit.transform == door.transform || (respectiveDoor != null && hit.transform == respectiveDoor.transform))
                {
                    //Debug.Log("Door is visible");
                    return DoorHit.CORRECT_DOOR;
                } 
                else if(hit.transform.gameObject.GetComponent<Door>() != null)
                {
                    return DoorHit.OTHER_DOOR;
                }
            }
            return DoorHit.WALLS;
        }

        public override void movedThroughDoor(Door door)
        {
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
                Gizmos.color = Color.red;
                foreach(var raycast in raycasts)
                {
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