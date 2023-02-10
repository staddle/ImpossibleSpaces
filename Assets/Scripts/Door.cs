using UnityEngine;

public class Door : MonoBehaviour
{
    public Vector3 position;
    public Vector3 point1, point2;
    public RoomSegment roomSegment;
    public Node previousNode;
    public Node nextNode;

    public void setupDoor(RoomSegment segment, Node prev, Vector3 position)
    {
        roomSegment = segment;
        previousNode = prev;
        this.position = position;
    }

    public Vector2 getPosition()
    {
        return new(position.x, position.z);
    }

    public Vector2 getPoint1()
    {
        return new(point1.x, point1.z);
    }

    public Vector2 getPoint2()
    {
        return new(point2.x, point2.z);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
