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

    public override Vector2 getRandomDoorLocation(RoomGeneratorOptions options)
    {
        System.Random random = new System.Random();
        Vector2 doorDirection = this.endPoint - this.startPoint;
        double doorStart = random.NextDouble() * (Math.Abs(doorDirection.magnitude) - options.doorWidth);
        doorDirection.Normalize();
        return startPoint + doorDirection * (float)doorStart;
    }

    private bool isOnPlayAreaEdge(GeneralLayoutRoom playArea)
    {
        return playArea.isOnEdge(startPoint) && playArea.isOnEdge(endPoint);
    }

    private bool isEnoughSpaceForAnotherRoom(GeneralLayoutRoom playArea, float lengthInRhythmDirectionWherePlayAreaCannotEnd)
    {
        Vector2 outDirection = getOutwardDirection();
        return playArea.isInside(startPoint + outDirection * lengthInRhythmDirectionWherePlayAreaCannotEnd) &&
            playArea.isInside(endPoint + outDirection * lengthInRhythmDirectionWherePlayAreaCannotEnd);
    }

    public override void drawHandles()
    {
        Handles.color = color;
        Handles.DrawLine(Vector2At(startPoint,0), Vector2At(endPoint,0));
    }
}
