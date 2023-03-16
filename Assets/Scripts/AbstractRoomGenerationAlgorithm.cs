using System.Collections.Generic;
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