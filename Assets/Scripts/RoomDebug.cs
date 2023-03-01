using Assets.Scripts;
using System.Collections.Generic;
using UnityEngine;

public partial class LayoutCreator
{
    public struct RoomDebug
    {
        public LinkedList<GeneralLayoutRoom> currentGeneralLayoutRooms;
        public GeneralLayoutRoom currentBigRoom;
        public LinkedList<Vector2> currentSampledPoints;

        public RoomDebug(LinkedList<GeneralLayoutRoom> currentGeneralLayoutRooms, GeneralLayoutRoom currentBigRoom, LinkedList<Vector2> currentSampledPoints)
        {
            this.currentGeneralLayoutRooms = currentGeneralLayoutRooms;
            this.currentBigRoom = currentBigRoom;
            this.currentSampledPoints = currentSampledPoints;
        }
    }
}
