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
        using (new GUILayout.VerticalScope())
        {
            if (GUILayout.Button("Regenerate Layout", buttonStyle()))
            {
                layoutCreator.regenerateLayout();
            }
            if (GUILayout.Button("Redraw Connections", buttonStyle()))
            {
                layoutCreator.redraw();
            }
            if(layoutCreator.currentRoom != null)
                using (new GUILayout.HorizontalScope())
                {
                    for(int i=1; i < layoutCreator.currentRoom.doors.Count; i++)
                    {
                        if(GUILayout.Button("Door "+i))
                        {
                            layoutCreator.goNextRoom(i);
                        }
                    }
                }
            bool testRoom = EditorGUILayout.Toggle("Test Room", layoutCreator.testRoom);
            layoutCreator.testRoom = testRoom;
            if(layoutCreator.testRoomVertices.Count != 4) 
                layoutCreator.testRoomVertices = new List<Vector2>() { new(), new(), new(), new() };
            if (testRoom)
            {
                using(new GUILayout.VerticalScope())
                {
                    for (int i = 0; i < 4; i++)
                    {
                        using(new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("Vertex " + i);
                            float x = FloatField("X", layoutCreator.testRoomVertices[i].x);
                            float y = FloatField("Y", layoutCreator.testRoomVertices[i].y);
                            layoutCreator.testRoomVertices[i] = new(x, y);
                        }
                    }
                }
            }
        }
        serializedObject.ApplyModifiedProperties();
    }

    //from https://forum.unity.com/threads/remove-empty-space-between-the-editorguilayout-textfields-label-and-inputbox.181115/
    public static float FloatField(string label, float number)
    {
        var dimensions = GUI.skin.label.CalcSize(new GUIContent(label));
        EditorGUIUtility.labelWidth = dimensions.x;
        return EditorGUILayout.FloatField(label, number);
    }

    public void OnSceneGUI()
    {
       /* Handles.DrawBezier(Vector3.zero, new Vector3(10, 0, 0), Vector3.zero, Vector3.zero, Color.black, null, 0);

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
        }*/
    }
}
