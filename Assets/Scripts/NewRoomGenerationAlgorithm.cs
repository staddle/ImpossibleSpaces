using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    public class NewRoomGenerationAlgorithm : AbstractRoomGenerationAlgorithm
    {
        public override void init(RoomGeneratorOptions options, bool testRoom = false, List<Vector2> testRoomVertices = null)
        {
            throw new System.NotImplementedException();
        }

        public override void movedThroughDoor(Door door)
        {
            throw new System.NotImplementedException();
        }

        public override void redraw(LayoutCreator.RoomDebug roomDebug, RoomGeneratorOptions options)
        {
            throw new System.NotImplementedException();
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