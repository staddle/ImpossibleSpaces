using Assets.Scripts;
using System.Collections.Generic;
using UnityEngine;

public partial class LayoutCreator
{
    public struct RoomDebug
    {
        public LinkedList<GeneralLayoutRoom> generalLayoutRooms;
        public GeneralLayoutRoom bigRoom;
        public LinkedList<Vector2> sampledPoints;

        public RoomDebug(LinkedList<GeneralLayoutRoom> currentGeneralLayoutRooms, GeneralLayoutRoom currentBigRoom, LinkedList<Vector2> currentSampledPoints)
        {
            generalLayoutRooms = currentGeneralLayoutRooms;
            bigRoom = currentBigRoom;
            sampledPoints = currentSampledPoints;
        }

        public LinkedList<Vector2> getConnectionPoints()
        {
            LinkedList<Vector2> ret = new LinkedList<Vector2> ();
            foreach(GeneralLayoutRoom room in generalLayoutRooms)
            {
                ret.AddLast(room.connectionToNext);
            }
            return ret;
        }
    }
}
