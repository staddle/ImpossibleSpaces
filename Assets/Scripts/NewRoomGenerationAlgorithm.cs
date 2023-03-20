using System.Collections.Generic;
using UnityEngine;
using static LayoutCreator;

namespace Assets.Scripts
{
    public class NewRoomGenerationAlgorithm : AbstractRoomGenerationAlgorithm
    {
        private List<Node> currentlyShownRooms = new List<Node>();
        private Transform playerTransform;

        // Update is called once per frame
        void Update()
        {
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
            currentlyShownRooms = new List<Node>();
            playerTransform = get().roomGeneratorOptions.playerTransform;
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
            door.nextNode = createRandomRoom(new Vector2(door.position.x, door.position.z), new Vector2(p2ToP1.y, -p2ToP1.x).normalized, currentRoom, door, currentlyShownRooms, get().roomGeneratorOptions);
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
            List<Vector3> directions = new List<Vector3> { door.point1 - position, door.point2 - position };
            foreach(Vector3 direction in directions)
            {
                if(Physics.Raycast(position, direction, out RaycastHit hitInfo))
                {
                    Door respectiveDoor = null;
                    if (door.nextNode != null)
                        respectiveDoor = door.nextNode.doors.Find(x => x.position == door.position);
                    if(hitInfo.transform == door.transform || (respectiveDoor != null && hitInfo.transform == respectiveDoor.transform))
                    {
                        Debug.Log("Door is visible");
                        return true;
                    }
                }
            }
            return false;
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

        // Use this for initialization
        void Start()
        {

        }
    }
}