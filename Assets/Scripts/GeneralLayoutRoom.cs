using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    public class GeneralLayoutRoom
    {
        public List<Vector2> vertices = new List<Vector2>();
        public int numberOfEdges { get { return vertices.Count; } }

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
            bool isInside = false;
            for (int i = 0, j = numberOfEdges - 1; i < numberOfEdges; j = i++)
            {
                if (((vertices[i].y > point.y) != (vertices[j].y > point.y)) &&
                    (point.x < (vertices[j].x - vertices[i].x) * (point.y - vertices[i].y) / (vertices[j].y - vertices[i].y) + vertices[i].x))
                {
                    isInside = !isInside;
                }
            }
            return isInside;
        }

        public bool isOnEdge(Vector2 point)
        {
            for(int i=0; i<4; i++)
            {
                if(isOnSpecificEdge(point, i))
                    return true;
            }
            return false;
        }

        // as per https://stackoverflow.com/a/11912171
        public bool isOnSpecificEdge(Vector2 C, int edgeIndex)
        {
            float THRESHOLD = 0.001f;
            Vector2 A = vertices[edgeIndex], B = vertices[(edgeIndex + 1) % 4];
            float distAC = Math.Abs(Vector2.Distance(A, C));
            float distAB = Math.Abs(Vector2.Distance(A, B));
            float distBC = Math.Abs(Vector2.Distance(B, C));

            return distAC + distBC - distAB < THRESHOLD;

            /*if(Vector2.Angle(Vector2.right, point2 - point1) == Vector2.Angle(Vector2.right, point - point1))
            {
                // same angle but is point between line points?
                return Math.Abs(Vector2.Distance(point1, point)) < Math.Abs(Vector2.Distance(point1, point2)) 
                    && Math.Abs(Vector2.Distance(point2, point)) < Math.Abs(Vector2.Distance(point2, point1));
            }

            return false;*/

        }
    }
}