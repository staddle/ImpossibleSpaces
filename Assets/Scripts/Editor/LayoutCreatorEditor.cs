using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LayoutCreator))]
public class LayoutCreatorEditor : Editor
{
    LayoutCreator layoutCreator;
    bool segmentsFoldOut = false, doorsFoldOut = false;

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
                    for(int i=0; i < layoutCreator.currentRoom.doors.Count; i++)
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
                            /*EditorGUILayout.LabelField("Vertex " + i);
                            float x = FloatField("X", layoutCreator.testRoomVertices[i].x);
                            float y = FloatField("Y", layoutCreator.testRoomVertices[i].y);
                            layoutCreator.testRoomVertices[i] = new(x, y);
                            */
                            layoutCreator.testRoomVertices[i] = EditorGUILayout.Vector2Field("Vertex " + i, layoutCreator.testRoomVertices[i]);
                        }
                    }
                }
            }
            if(layoutCreator.currentRoom != null)
            {
                segmentsFoldOut = EditorGUILayout.Foldout(segmentsFoldOut, "RoomSegments");
                if(segmentsFoldOut)
                {
                    LinkedList<RoomSegment> segments = layoutCreator.currentRoom.segments;
                    for (int i = 0; i < segments.Count; i++)
                    {
                        using (new GUILayout.VerticalScope())
                        {
                            EditorGUILayout.LabelField("Segment " + i);
                            using (new EditorGUI.IndentLevelScope())
                            {
                                EditorGUILayout.Vector2Field("Start: ", segments.ElementAt(i).startPoint);
                                EditorGUILayout.Vector2Field("End: ", segments.ElementAt(i).endPoint);
                            }
                        }
                    }
                }
                doorsFoldOut = EditorGUILayout.Foldout(doorsFoldOut, "Doors");
                if (doorsFoldOut)
                {
                    List<Door> doors = layoutCreator.currentRoom.doors;
                    for (int i = 0; i < doors.Count; i++)
                    {
                        using (new GUILayout.VerticalScope())
                        {
                            EditorGUILayout.LabelField("Door " + i);
                            using (new EditorGUI.IndentLevelScope())
                            {
                                EditorGUILayout.Vector2Field("Position: ", new(doors[i].position.x, doors[i].position.z));
                            }
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
        if (layoutCreator == null || layoutCreator.currentRoom == null)
            return;
        var doors = layoutCreator.currentRoom.doors;
        if (doors == null)
            return;
        for (int i = 0; i < doors.Count; i++)
        {
            Door door = doors[i];
            Vector3 position = door.point1 + (door.point2 - door.point1) / 2;
            GUIStyle style = new GUIStyle();
            style.normal = new GUIStyleState();
            style.normal.textColor = Color.black;
            style.normal.background = Texture2D.whiteTexture;
            Handles.Label(position, "Door " + i, style);
        }
    }
}
