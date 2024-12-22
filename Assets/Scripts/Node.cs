using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
/// <summary>
/// Class <c>Node</c> models a node in a grid
/// </summary>
/// <param name="position">The position of the node relative to the grid in (Y, X) coords</param>
/// <param name="endPosition">The position of the node the A* algorithm is trying to pathfind to</param>
/// <param name="parent">The parent node of this node</param>
public class Node
{
    public Node Parent;
    public Vector2 nodePosition { get; }
    public Vector2 parentPosition { get; }
    public int gCost { get; }
    public int hCost { get; }
    public int fCost { get; }

    public Node(Vector2 NodePos, Vector2 targetPos, Node parent)
    {
        gCost = 0; // Cost from start node to current node
        hCost = 0; // hypothetical cost to end node from current node
        fCost = 0; // gCost + hCost
        Parent = parent;
        nodePosition = new Vector2((int)NodePos.x, (int)NodePos.y);

        hCost = CalculateMagnitude(NodePos, targetPos);
        
        if (parent is not null)
        {
            parentPosition = parent.nodePosition;
            gCost = parent.gCost + CalculateMagnitude(nodePosition, parentPosition);
        }
        
        fCost = gCost + hCost;
    }

    // private int CalculateMagnitude(Vector2 start, Vector2 end) => (int)(Vector2.Distance(start, end) * 10f);

    private int CalculateMagnitude(Vector2 start, Vector2 end) => (int)start.x == (int)end.x || (int)start.y == (int)end.y ? 10 : 14;
    private int ChebyshevDistance(Vector2 start, Vector2 end)
    {
        float difX = Math.Abs(start.x - end.x);
        float difY = Math.Abs(start.y - end.y);
        return (int)(Math.Max(difX, difY) * 10);
    }
}
