using System.Collections.Generic;
using UnityEngine;
using static LayoutCreator;

namespace Assets.Scripts
{
    public class OldRoomGenerationAlgorithm : AbstractRoomGenerationAlgorithm
    {

        public override void init(RoomGeneratorOptions options, bool testRoom = false, List<Vector2[]> testRoomVertices = null)
        {
            currentRoom = createRandomRoom(new Vector2(0, 0), Vector2.up, null, null, new List<Node>(), options, testRoom ? testRoomVertices[0] : null);
            currentRoom.gameObject.SetActive(true);
            setFollowingRooms(true);
        }

        public override void redraw(RoomDebug roomDebug, RoomGeneratorOptions options)
        {
            createRandomRoomInternal(roomDebug.generalLayoutRooms, null, null, options, null);
        }

        private void setFollowingRooms(bool active)
        {
            foreach (Door door in currentRoom.doors)
            {
                if (door.nextNode == null)
                {
                    Vector2 p2ToP1 = door.getPoint2() - door.getPoint1();
                    door.nextNode = createRandomRoom(new Vector2(door.position.x, door.position.z), new Vector2(p2ToP1.y, -p2ToP1.x).normalized, currentRoom, door, new List<Node>(), get().roomGeneratorOptions); ;
                }
                if (!get().roomGeneratorOptions.renderNextRoomsAlready)
                    continue;
                door.nextNode.gameObject.SetActive(active);
                if (active)
                {
                    door.nextNode.sendToShader(currentRoom.getVertices());
                    door.nextNode.setAllDoorsActive(false);
                }
            }
        }

        public override void movedThroughDoor(Door door)
        {
            currentRoom.gameObject.SetActive(false);
            setFollowingRooms(false);
            currentRoom = door.nextNode;
            currentRoom.gameObject.SetActive(true);
            currentRoom.sendToShader(new List<Vector3>());
            currentRoom.setAllDoorsActive(true);
            setFollowingRooms(true);
            get().switchedRoom = door;
        }
    }
}