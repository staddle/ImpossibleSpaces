using Assets.Scripts;
using UnityEngine;

public class RoomSegment
{
    public Vector2 startPoint, endPoint;
    public Color color = Color.black;
    public float width = 0f;

    public RoomSegment(Vector2 startPoint, Vector2 endPoint)
    {
        this.startPoint = startPoint;
        this.endPoint = endPoint;
    }

    public virtual void drawHandles()
    {
        Debug.LogError("not implemented");
    }

    public virtual bool canContainDoor(float doorWidth, GeneralLayoutRoom playArea)
    {
        return false;
    }

    protected Vector3 Vector2At(Vector2 v2, float y)
    {
        return LayoutCreator.Vector2At(v2, y);
    }
}
