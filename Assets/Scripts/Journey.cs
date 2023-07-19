using UnityEngine;

[RequireComponent(typeof(RoomGeneratorOptions))]
public class Journey : MonoBehaviour
{
    RoomGeneratorOptions options;
    LayoutCreator layoutCreator;
    private int depth = 0;

    public JourneyType journeyType = JourneyType.BigToSmall;

    public int endDepth = 10;

    public enum JourneyType
    {
        BigToSmall
    }

    // Start is called before the first frame update
    void Start()
    {
        layoutCreator = LayoutCreator.get();
        optionsForDepth(0);
    }

    /// <summary>
    /// Called when player enters other room
    /// </summary>
    /// <param name="newDepth"></param>
    void switchedRoom(int newDepth)
    {
        depth = newDepth;
        optionsForDepth(depth);
    }

    public RoomGeneratorOptions optionsForDepth(int d)
    {
        if (options == null)
            options = GetComponent<RoomGeneratorOptions>();
        switch (journeyType)
        {
            case JourneyType.BigToSmall:
                bigToSmall(d);
                break;
        }
        return options;
    }

    private void bigToSmall(int d)
    {
        options.minimumRoomSize = 1;
        options.minNumberOfDoorsPerRoom = 1;
        options.maxNumberOfDoorsPerRoom = 2;
        options.maximumRoomSize = endDepth - d;
        if (options.maximumRoomSize < 0)
        {
            options.maximumRoomSize = 0;
            options.minNumberOfDoorsPerRoom = 0;
            options.maxNumberOfDoorsPerRoom = 0;
            return;
        }
        options.probabilityOfNextRoom = 1 - d / endDepth;
    }
}
