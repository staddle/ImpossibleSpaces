using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomGeneratorOptions : MonoBehaviour
{
    [Header("General")]
    [Range(0,10)]
    public int minimumRoomSize = 1;
    [Range(0,10)]
    public int maximumRoomSize = 2;
    [Range(0,1f)]
    public float probabilityOfNextRoom = 0.5f;
    [Range(0,1f)]
    public float probabilityMultiplierPerNextRoom = 0.5f;
    [Range(0,5f)]
    public float minimumGeneralLayoutWidth = 1;
    [Range(0,10f)]
    public float maximumGeneralLayoutRoomSize = 6;
    [Range(0,10)]
    public float lengthInRhythmDirectionWherePlayAreaCannotEnd = 1;
    [Range(0,10)]
    public int maxNumberOfSamplePointsPerEdge = 2;
    [Range(0,10f)]
    public float maxMagnitudeOfSamplePointsGoingAway = 1.5f;

    [Header("Layout")]
    public LayoutType type = LayoutType.arcs;
    public bool useOnlyVertices = false;
    public bool invertAfterEachPoint = true;
    [Range(1,100)]
    public int bezierSubdivisions = 10;
    [Range(0,90)]
    public int bezierMaxTangentAngle = 45;

    [Header("Display")]
    public bool showGeneralLayoutRooms = false;
    public bool showBigRoom = true;
    public bool showSamplePoints = true;
    public bool showVertexNumbers = false;
    public bool showSamplePointNumbers = false;
    public bool showFinishedRoom = true;
    public bool showBezierTangents = false;

    public Material roomMaterial;
}

public enum LayoutType {
    arcs,
    straights,
    beziers
}
