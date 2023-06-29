using UnityEditor;
using UnityEngine;

public class ArcRoomSegment : RoomSegment
{
    bool normalUp;

    public ArcRoomSegment(Vector2 startPoint, Vector2 endPoint, bool normalUp = true) : base(startPoint, endPoint)
    {
        this.normalUp = normalUp;
    }

//#if UNITY_EDITOR
    public override void drawHandles()
    {
        Vector3 center, normal, from, to;
        float angle, radius;

        center = Vector2At(startPoint + (endPoint - startPoint) / 2, 0);
        normal = normalUp ? Vector3.up : Vector3.down;
        from = Vector2At(startPoint, 0) - center;
        to = Vector2At(endPoint, 0) - center;
        angle = Vector3.Angle(from, to);
        radius = from.magnitude;

        Handles.DrawWireArc(center, normal, from, angle, radius, width);
    }
//#endif
}
