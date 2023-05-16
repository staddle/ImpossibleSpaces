using Assets.Scripts;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    public Vector3 position;
    public Vector3 point1, point2;
    public Vector3 outwardsDirection;
    public RoomSegment roomSegment;
    public Node previousNode;
    public Node nextNode;
    public delegate void OnCollisionEnterDel(Collider collider, Door door);

    private BoxCollider doorCollider;
    private OnCollisionEnterDel onCollisionEnter;
    private float doorHeight, doorArea;
 

    public void setupDoor(RoomSegment segment, Node prev, Vector3 position, float doorHeight, float doorArea, float doorWidth, OnCollisionEnterDel callback, bool skipLayer = false)
    {
        roomSegment = segment;
        previousNode = prev;
        if(!skipLayer)
            gameObject.layer = previousNode.LayerNumber;
        this.position = position;
        onCollisionEnter = callback;
        this.doorArea = doorArea;
        this.doorHeight = doorHeight;
        var doorDirection = LayoutCreator.Vector2At(segment.endPoint - segment.startPoint, 0).normalized;
        point1 = position - doorDirection * doorWidth / 2;
        point2 = position + doorDirection * doorWidth / 2;
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

    public BoxCollider DoorCollider => doorCollider;

    private void OnTriggerEnter(Collider collider)
    {
        //Debug.Log("Collision detected on " + gameObject.name + " with " + collider.gameObject.name);
        onCollisionEnter(collider, this);
    }

    public bool isInsideDoorArea(Vector3 point, float doorArea)
    {
        Vector2 p1 = getPoint1();
        Vector2 p2 = getPoint2();
        Vector2 p1top2 = p2 - p1;
        Vector2 outwards = new Vector2(p1top2.y, -p1top2.x).normalized;
        GeneralLayoutRoom generalLayoutRoom = new GeneralLayoutRoom(new List<Vector2>() { p1, p2, p2 + outwards * doorArea, p1 + outwards * doorArea });
        return generalLayoutRoom.isInside(new Vector2(point.x, point.z));
    }

    // Start is called before the first frame update
    void Start()
    {
        // add collider when Door becomes active (when mesh is already calculated)
        doorCollider = gameObject.AddComponent<BoxCollider>();
        doorCollider.isTrigger = true;
        doorCollider.center += Vector3.up * doorHeight / 2;
        Vector3 size = new Vector3(0, doorHeight, 0);
        Vector3 p1Top2 = point2 - point1;
        outwardsDirection = Vector3.Cross(p1Top2, new(0, 1, 0)).normalized;
        size += p1Top2;
        size += outwardsDirection * doorArea / 4;
        for(int i=0; i<3; i++)
        {
            if (size[i] < 0f) size[i] *= -1;
        }
        doorCollider.size = size;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
