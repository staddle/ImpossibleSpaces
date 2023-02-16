using Assets.Scripts;
using System.Collections.Generic;
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

    public bool isInsideDoorArea(Vector3 point, float doorArea)
    {
        Vector2 p1 = getPoint1();
        Vector2 p2 = getPoint2();
        Vector2 p1top2 = p2 - p1;
        Vector2 outwards = new Vector2(p1top2.y, -p1top2.x).normalized;
        GeneralLayoutRoom generalLayoutRoom = new GeneralLayoutRoom(new List<Vector2>() { p1, p2, p2 + outwards * doorArea, p1 + outwards * doorArea });
        return generalLayoutRoom.isInside(new(point.x, point.z));
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
