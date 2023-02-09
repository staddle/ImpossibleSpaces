using Assets.Scripts;
using System;
using UnityEditor;
using UnityEngine;

public class StraightRoomSegment : RoomSegment
{
    public StraightRoomSegment(Vector2 startPoint, Vector2 endPoint) : base(startPoint, endPoint)
    {

    }

    public override bool canContainDoor(float doorWidth, float lengthInRhythmDirectionWherePlayAreaCannotEnd, GeneralLayoutRoom playArea)
    {
        bool isEnoughWidth = Math.Abs(Vector2.Distance(endPoint, startPoint)) >= doorWidth;
        bool isEnoughSpace = isEnoughSpaceForAnotherRoom(playArea, lengthInRhythmDirectionWherePlayAreaCannotEnd);

        return isEnoughSpace && isEnoughWidth;
    }

    private bool isOnPlayAreaEdge(GeneralLayoutRoom playArea)
    {
        return playArea.isOnEdge(startPoint) && playArea.isOnEdge(endPoint);
    }

    private bool isEnoughSpaceForAnotherRoom(GeneralLayoutRoom playArea, float lengthInRhythmDirectionWherePlayAreaCannotEnd)
    {
        Vector2 start2End = endPoint - startPoint;
        Vector2 outDirection = new Vector2(start2End.y, -start2End.x).normalized;
        return playArea.isInside(startPoint + outDirection * lengthInRhythmDirectionWherePlayAreaCannotEnd) &&
            playArea.isInside(endPoint + outDirection * lengthInRhythmDirectionWherePlayAreaCannotEnd);
    }

    public override void drawHandles()
    {
        Handles.color = color;
        Handles.DrawLine(Vector2At(startPoint,0), Vector2At(endPoint,0));
    }
}
