using System;
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

        // Update is called once per frame
        void Update()
        {
            raycasts.Clear();

            for (int i = 0; i < currentlyShownRooms.Count; i++)
            {
                // door.nextNode von neuem Raum ist Startraum? Wann wird das gesetzt?
                Node node = currentlyShownRooms[i];
                bool isVisible = false;
                var doorsCopy = new List<Door>(node.doors);
                foreach(Door door in doorsCopy)
                {
                    bool doorVisible = isDoorVisible(door);
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
            Vector2 p2ToP1 = door.getPoint2() - door.getPoint1();
            door.nextNode = createRandomRoom(new Vector2(door.position.x, door.position.z), new Vector2(p2ToP1.y, -p2ToP1.x).normalized, currentRoom, door, currentlyShownRooms, options, 
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
            Vector3 position = playerTransform.position;
            Vector3 toMiddle = (door.point2 - door.point1).normalized;
            List<Vector3> directions = new List<Vector3> { door.position - position, door.point1 - position + toMiddle * 0.01f, door.point2 - position - toMiddle * 0.01f };
            directions.ForEach(x => x.Normalize());
            float maxDistance = (float) Math.Sqrt(Math.Pow(options.playArea.x, 2) + Math.Pow(options.playArea.y,2));
            foreach (Vector3 direction in directions)
            {
                /*if (Physics.Raycast(position, direction, out RaycastHit hit, maxDistance))
                {
                    //hit with wall?
                    if(hit.transform.parent != null && hit.transform.parent.gameObject.name == "PlayArea")
                    {
                        raycasts.Add(new Tuple<Vector3, Vector3, bool>(position, hit.point, true));
                        return true;
                    }
                }*/
                for (int i = currentRoom.LayerNumber; i<currentRoom.LayerNumber+options.depthForward; i++)
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
                else if(hit.transform.gameObject.GetComponent<Door>() != null)
                {
                    raycasts.Add(new Tuple<Vector3, Vector3, bool>(position, hit.point, false));
                    return DoorHit.OTHER_DOOR;
                }
                raycasts.Add(new Tuple<Vector3, Vector3, bool>(position, hit.point, false));
            }
            return DoorHit.WALLS;
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