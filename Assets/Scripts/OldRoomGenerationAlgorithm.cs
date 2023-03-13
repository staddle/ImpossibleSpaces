using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    public class OldRoomGenerationAlgorithm : AbstractRoomGenerationAlgorithm
    {

        public override void init(RoomGeneratorOptions options)
        {
            currentRoom = createRandomRoom(new Vector2(0, 0), Vector2.up, null, null, options, testRoom ? testRoomVertices : null);
            currentRoom.gameObject.SetActive(true);
            setFollowingRooms(currentRoom, true);
        }

        private void setFollowingRooms(Node room, bool active)
        {
            currentRoom.generateNextRooms();
            if (!roomGeneratorOptions.renderNextRoomsAlready)
                return;
            foreach (Door otherDoor in room.doors)
            {
                if (otherDoor.nextNode != null)
                {
                    otherDoor.nextNode.gameObject.SetActive(active);
                    if (active)
                    {
                        otherDoor.nextNode.sendToShader(currentRoom.getVertices());
                        otherDoor.nextNode.setAllDoorsActive(false);
                    }
                }
            }
        }

        public override void movedThroughDoor(Door door)
        {
            currentRoom.gameObject.SetActive(false);
            setFollowingRooms(currentRoom, false);
            currentRoom = door.nextNode;
            currentRoom.gameObject.SetActive(true);
            currentRoom.sendToShader(new List<Vector3>());
            currentRoom.setAllDoorsActive(true);
            setFollowingRooms(currentRoom, true);
            switchedRoom = door;
        }

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}