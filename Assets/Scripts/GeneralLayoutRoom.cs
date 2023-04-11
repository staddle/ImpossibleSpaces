using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    public class GeneralLayoutRoom
    {
        public List<Vector2> vertices = new List<Vector2>();
        public int numberOfEdges { get { return vertices.Count; } }
        public Vector2 connectionToNext, connectionToPrev;

        public GeneralLayoutRoom(List<Vector2> _vertices)
        {
            vertices = _vertices;
        }

        public override string ToString()
        {
            return string.Join(",", vertices.Select(x => "(" + x.x + "," + x.y + ")").ToArray());
        }

        public bool isInside(Vector2 point)
        {
            return isInsideInt(point).Count % 2 != 0;
        }

        public bool isInside(Vector3 point)
        {
            return isInside(new Vector2(point.x, point.z));
        }

        public List<int> isInsideInt(Vector2 point)
        {
            List<int> intersections = new List<int>();
            for (int i = 0, j = numberOfEdges - 1; i < numberOfEdges; j = i++)
            {
                if (((vertices[i].y > point.y) != (vertices[j].y > point.y)) &&
                    (point.x < (vertices[j].x - vertices[i].x) * (point.y - vertices[i].y) / (vertices[j].y - vertices[i].y) + vertices[i].x))
                {
                    intersections.Add(i);
                }
            }
            return intersections;
        }

        public bool isOnEdge(Vector2 point)
        {
            for(int i=0; i<vertices.Count; i++)
            {
                if(isOnSpecificEdge(point, i))
                    return true;
            }
            return false;
        }

        public bool isOnSpecificEdge(Vector2 C, int edgeIndex)
        {
            return isPointOnLine(C, vertices[edgeIndex], vertices[(edgeIndex + 1) % vertices.Count]);
        }

        // as per https://stackoverflow.com/a/11912171
        public static bool isPointOnLine(Vector2 point, Vector2 line1, Vector2 line2, float THRESHOLD = 0.001f)
        {
            float distAC = Math.Abs(Vector2.Distance(line1, point));
            float distAB = Math.Abs(Vector2.Distance(line1, line2));
            float distBC = Math.Abs(Vector2.Distance(line2, point));

            return distAC + distBC - distAB < THRESHOLD;
        }
    }
}