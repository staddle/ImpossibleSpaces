using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class StraightRoomSegment : RoomSegment
{
    public StraightRoomSegment(Vector2 startPoint, Vector2 endPoint) : base(startPoint, endPoint)
    {

    }

    public override bool canContainDoor(float doorWidth, GeneralLayoutRoom playArea)
    {
        bool isEnoughWidth = Math.Abs(Vector2.Distance(endPoint, startPoint)) >= doorWidth;
        bool isEnoughSpace = !isOnPlayAreaEdge(playArea);

        return isEnoughSpace && isEnoughWidth;
    }

    private bool isOnPlayAreaEdge(GeneralLayoutRoom playArea)
    {
        return playArea.isOnEdge(startPoint) && playArea.isOnEdge(endPoint);
    }

    public override void drawHandles()
    {
        Handles.color = color;
        Handles.DrawLine(Vector2At(startPoint,0), Vector2At(endPoint,0));
    }
}
