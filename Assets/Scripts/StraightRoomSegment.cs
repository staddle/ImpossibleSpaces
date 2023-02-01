using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class StraightRoomSegment : RoomSegment
{
    public StraightRoomSegment(Vector2 startPoint, Vector2 endPoint) : base(startPoint, endPoint)
    {

    }

    public override void drawHandles()
    {
        Handles.color = color;
        Handles.DrawLine(Vector2At(startPoint,0), Vector2At(endPoint,0));
    }
}
