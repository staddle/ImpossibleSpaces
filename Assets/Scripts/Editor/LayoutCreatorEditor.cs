using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

[CustomEditor(typeof(LayoutCreator))]
public class LayoutCreatorEditor : Editor
{
    LayoutCreator layoutCreator;

    public static GUIStyle buttonStyle()
    {
        GUIStyle g = new GUIStyle(GUI.skin.button);
        //g.stretchWidth = false;
        return g;
    }

    private void OnEnable()
    {
        layoutCreator = (LayoutCreator)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using(new GUILayout.VerticalScope())
        {
            if (GUILayout.Button("Regenerate Layout", buttonStyle()))
            {
                layoutCreator.regenerateLayout();
            }
            if (GUILayout.Button("Redraw Connections", buttonStyle()))
            {
                layoutCreator.redraw();
            }
        }
        serializedObject.ApplyModifiedProperties();
    }

    public void OnSceneGUI()
    {
        Handles.DrawBezier(Vector3.zero, new Vector3(10, 0, 0), Vector3.zero, Vector3.zero, Color.black, null, 0);

        if(layoutCreator.roomGeneratorOptions != null && layoutCreator.roomGeneratorOptions.showFinishedRoom && layoutCreator.roomSegments != null && layoutCreator.roomSegments.Count > 0)
        {
            foreach(RoomSegment segment in layoutCreator.roomSegments)
            {
                if(segment.GetType() != typeof(BezierRoomSegment))
                {
                    Handles.color = segment.color;
                    segment.drawHandles();
                }
            }
        }
    }
}
