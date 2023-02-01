using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BezierRoomSegment : RoomSegment
{
    public Vector2 startTangent, endTangent;

    public BezierRoomSegment(Vector2 startPoint, Vector2 endPoint, Vector2 startTangent, Vector2 endTangent) : base(startPoint, endPoint)
    {
        this.startTangent = startTangent;
        this.endTangent = endTangent;
    }

    public override void drawHandles()
    {
        Handles.DrawBezier(Vector2At(startPoint, 0), Vector2At(endPoint, 0), Vector2At(startTangent, 0), Vector2At(endTangent, 0), color, null, width);
    }

    public void drawGizmos(int subdivisions = 10, bool drawTangents = false)
    {
        Gizmos.color = Color.black;
        drawBeziers(Vector2At(startPoint, 0), Vector2At(endPoint, 0), Vector2At(startTangent, 0), Vector2At(endTangent, 0), subdivisions);
        Gizmos.color = Color.blue;
        if(drawTangents)
        {
            Gizmos.DrawLine(Vector2At(startPoint, 0), Vector2At(startTangent, 0));
            Gizmos.DrawLine(Vector2At(endPoint, 0), Vector2At(endTangent, 0));
        }
    }

    //https://answers.unity.com/questions/167362/trouble-with-handlesdrawbezier.html
    private void drawBeziers(Vector3 startPosition, Vector3 endPosition, Vector3 startTangent, Vector3 endTangent, int subdivisions = 10)
    {
        subdivisions = Mathf.Max(1, subdivisions);
        Vector3[] array = new Vector3[subdivisions + 1];

        // B(t) = (1-t)^3 * startPosition + 3 * (1-t)^2 * t * startTangent + 3 * (1-t) * t^2 * endTangent + t^3 * endPosition, t=[0,1]
        for (int i = 0; i <= subdivisions; i++)
        {
            float t = i / (float)subdivisions;
            float omt = 1.0f - t; // One minus t = omt
            array[i] = startPosition * (omt * omt * omt) +
                startTangent * (3 * omt * omt * t) +
                endTangent * (3 * omt * t * t) +
                endPosition * (t * t * t);
            if (i > 0)
            {
                Gizmos.DrawLine(array[i - 1], array[i]);
            }
        }
    }
}
