using Assets.Scripts;
using System.Collections.Generic;
using UnityEngine;

public class RoomSegment
{
    public Vector2 startPoint, endPoint;
    public Color color = Color.black;
    public float width { get{ return (endPoint - startPoint).magnitude; } }

    public RoomSegment(Vector2 startPoint, Vector2 endPoint)
    {
        this.startPoint = startPoint;
        this.endPoint = endPoint;
    }
#if UNITY_EDITOR
    public virtual void drawHandles()
    {
        Debug.LogError("not implemented");
    }
#endif
    public virtual bool canContainDoor(float doorWidth, float lengthInRhythmDirectionWherePlayAreaCannotEnd, GeneralLayoutRoom playArea, List<Node> visibleRooms)
    {
        return false;
    }

    public virtual Vector2 getRandomDoorLocation(RoomGeneratorOptions options)
    {
        return new();
    }

    public Vector2 getOutwardDirection()
    {
        Vector2 start2End = endPoint - startPoint;
        return new Vector2(start2End.y, -start2End.x).normalized;
    }

    protected Vector3 Vector2At(Vector2 v2, float y)
    {
        return LayoutCreator.Vector2At(v2, y);
    }
}
