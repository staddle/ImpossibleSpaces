using Assets.Scripts;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AutoMoveThroughRooms : MonoBehaviour
{
    [SerializeField]
    private Transform camTransform;
    [SerializeField]
    private float moveSpeed = 1f;
    [SerializeField]
    private bool debugMode = false;
    [SerializeField]
    private float debugGizmosSize = 0.2f;
    [SerializeField]
    private bool log = false;

    private LayoutCreator creator;

    private Node currentRoom;
    private Door door;
    private List<Vector3> waypoints = new List<Vector3>();
    private int currentWaypoint = 0;

    private HashSet<Node> traversedNodes = new HashSet<Node>();
    private Node startRoom;
    private System.Random random = new System.Random();

    // Start is called before the first frame update
    void Start()
    {
        creator = LayoutCreator.get();
    }

    public void triggeredDoor(Door door)
    {
        if (door == this.door) return; // as expected
        else resetProgress(creator.CurrentRoom); //unexpectedly walked through other door
    }

    List<Vector3> calculateWaypoints()
    {
        var ret = new List<Vector3>();
        /*var vertices = currentRoom.getVerticesV2();
        GeneralLayoutRoom generalLayoutRoom = new GeneralLayoutRoom(vertices);
        var intersections = generalLayoutRoom.isInsideInt(new(door.position.x, door.position.z));
        if (intersections.Count > 0)
        {
            foreach(var intersection in intersections)
            {
                ret.Add(vertices[(intersection + 1) % vertices.Count]);
            }
        }*/
        if(LayoutCreator.RoomDebugs.ContainsKey(currentRoom))
        {
            LinkedList<GeneralLayoutRoom> glrs = LayoutCreator.RoomDebugs[currentRoom].generalLayoutRooms;
            int start = 0, end = 0;
            for(int i = 0; i<glrs.Count; i++)
            {
                var room = glrs.ElementAt(i);
                if(room.isInside(transform.position))
                {
                    start = i;
                }
                if(room.isInside(door.position))
                {
                    end = i;
                }
            }

            if(start < end)
            {
                for(int i = start; i < end; i++)
                {
                    Vector3 p = LayoutCreator.Vector2At(glrs.ElementAt(i).connectionToNext, 0);
                    if(new GeneralLayoutRoom(currentRoom.getVerticesV2()).isInside(p))
                        ret.Add(p);
                }
            } 
            else if(start > end)
            {
                for(int i = end; i > start; i--)
                {
                    Vector3 p = LayoutCreator.Vector2At(glrs.ElementAt(i).connectionToPrev, 0);
                    if (new GeneralLayoutRoom(currentRoom.getVerticesV2()).isInside(p))
                        ret.Add(p);
                }
            }
        }

        ret.Add(door.position + door.outwardsDirection.normalized / 2);
        ret.Add(door.position);
        ret.Add(door.position - door.outwardsDirection.normalized);
        return ret;
    }

    // Update is called once per frame
    void Update()
    {
        if(startRoom == null)
        {
            if(creator.CurrentRoom != null && creator.CurrentRoom.doors != null)
            {
                startRoom = creator.CurrentRoom;
                resetProgress(creator.CurrentRoom);
            }
        }
        if(door != null)
        {
            Vector3 direction = waypoints[currentWaypoint] - transform.position;
            if(direction.magnitude < Time.deltaTime * moveSpeed) // if waypoint is reached, increment currentWaypoint
            {
                currentWaypoint++;
                if(currentWaypoint == waypoints.Count)
                {
                    //moveToDoor();
                    resetProgress(door.nextNode); //backtracking => previousNode?
                    return; //door reached
                }
                direction = waypoints[currentWaypoint] - transform.position;
            }
            direction.Normalize();
            transform.Translate(direction * Time.deltaTime * moveSpeed); // move in direction of next waypoint
            camTransform.Rotate(new Vector3(0, Vector3.Angle(camTransform.forward, direction), 0));
        }
    }

    void resetEverything()
    {
        if(log) Debug.Log("Regenerating layout...");
        creator.regenerateLayout();
        startRoom = null;
        door = null;
    }

    void resetProgress(Node nextRoom)
    {
        if(nextRoom == null || nextRoom.doors.Count == 0) //something really went wrong -> reset everything
        {
            resetEverything();
            return;
        }
        if(currentRoom != null)
            traversedNodes.Add(currentRoom);
        if(nextRoom.doors.Count == 1 && nextRoom != startRoom)
        {
            if(door != null)
                traversedNodes.Add(door.nextNode);
            resetProgress(currentRoom);
            return;
        }
        currentRoom = nextRoom;
        currentWaypoint = 0;
        int doorIndex = random.Next(currentRoom == startRoom ? 0 : 1, currentRoom.doors.Count - 1);
        int savedIndex = doorIndex;
        bool firstIter = true;
        door = currentRoom.doors[doorIndex];
        while(traversedNodes.Contains(door.nextNode))
        {
            if(savedIndex == doorIndex)
            {
                if(!firstIter)
                {
                    if (nextRoom == startRoom)
                    {
                        resetEverything();
                        return;
                    }
                    else
                    {
                        door = nextRoom.doors[0]; // get back to previous room TODO: Walk in/out of collider?
                        if(log) Debug.Log("Backtracking");
                    }
                    break;
                } 
                else
                {
                    firstIter = false;
                }
            }
            doorIndex = ++doorIndex % currentRoom.doors.Count;
            door = currentRoom.doors[doorIndex];
        }
        waypoints = calculateWaypoints();
        if(log) Debug.Log(string.Join(", ", waypoints));
    }

    private void OnDrawGizmos()
    {
        if (!debugMode || waypoints.Count == 0)
            return;
        Gizmos.color = Color.yellow;
        for(int i=currentWaypoint; i<waypoints.Count; i++)
        {
            Gizmos.DrawCube(waypoints[i], new(debugGizmosSize, debugGizmosSize, debugGizmosSize));
        }
        Gizmos.DrawLine(transform.position, waypoints[currentWaypoint]);
    }
}
