using UnityEngine;

namespace Assets.Scripts
{
    public abstract class AbstractRoomGenerationAlgorithm : MonoBehaviour
    {
        public Node currentRoom;

        public abstract void init(RoomGeneratorOptions options);
        public abstract void movedThroughDoor(Door door);

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