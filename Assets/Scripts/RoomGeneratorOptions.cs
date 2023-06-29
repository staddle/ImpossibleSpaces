using UnityEngine;

public class RoomGeneratorOptions : MonoBehaviour
{
    [Header("Constants")]
    public Vector2 playArea = new Vector2(10, 10);
    public float playAreaWallHeight = 3f;
    public Vector3 bottomLeftMostPoint = new Vector3(0, 0, 0);
    public Vector3 playerStartingPoint = new Vector3(0.5f, 0, 0.5f);

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
    public float maximumGeneralLayoutWidth = 6;
    [Range(0,10)]
    public float lengthInRhythmDirectionWherePlayAreaCannotEnd = 1;
    [Range(0,10)]
    public int maxNumberOfSamplePointsPerEdge = 2;
    [Range(0,10f)]
    public float maxMagnitudeOfSamplePointsGoingAway = 1.5f;
    [Range(0, 10)]
    public int maxNumberOfDoorsPerRoom = 4;
    [Range(0, 10)]
    public int minNumberOfDoorsPerRoom = 1;
    [Range(0, 10f)]
    public float doorHeight = 2f;
    [Range(0, 10f)]
    public float doorWidth = 1f;
    [Tooltip("When going back through the door you came from, should the room be the previous one or a newly generated one?")]
    public bool backDoorToPreviousRoom = true;
    public bool oldRoomGenerator = true;
    public bool debugGenerator = false;

    [Header("Layout")]
    public LayoutType type = LayoutType.straights;
    public bool useOnlyVertices = true;
    public bool invertAfterEachPoint = true;
    [Range(1,100)]
    public int bezierSubdivisions = 10;
    [Range(0,90)]
    public int bezierMaxTangentAngle = 45;

    [Header("Display")]
    public bool showGeneralLayoutRooms = false;
    public bool showBigRoom = false;
    public bool showSamplePoints = false;
    public bool showVertexNumbers = false;
    public bool showSamplePointNumbers = false;
    public bool showFinishedRoom = true;
    public bool showBezierTangents = false;
    public bool useSegments = false;
    public bool useCeilings = false;

    [Header("Gameplay")]
    public Transform playerTransform;
    [Range(0,5f)]
    public float doorArea = 0.5f;
    //old room generator
    public bool renderNextRoomsAlready = false;
    [Tooltip("How many rooms forward should be tested whether a room is visible or not (how many doors in a row can be visible)")]
    public int depthForward = 3;


    public Material roomMaterial;
    public GameObject wallSegmentPrefab;
    public GameObject ceilingPrefab;
    public GameObject doorPrefab;
}

public enum LayoutType {
    arcs,
    straights,
    beziers
}
