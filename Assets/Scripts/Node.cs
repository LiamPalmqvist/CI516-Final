using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Node
{
    public readonly Vector2 NodePosition;
    public readonly Vector2 EndPos;
    public readonly Node Parent;
    public readonly int gCost; // The cost from the start node to the current node
    public readonly int hCost; // The hypothetical cost from the current node to the end node
    public readonly int hxCost;
    public readonly int hyCost;
    public readonly int fCost; // The gCost and the hCost added together
    public readonly bool walkable;
    
    public Node(Vector2 position, Vector2 endPosition, Node parent)
    {
        NodePosition = position;
        EndPos = endPosition;
        if (parent is null)
        {
            fCost = 0;
        }
        else
        {
            Parent = parent;
            fCost = CalculateGCost();
        }
        hCost = CalculateHCost();
        hxCost = (int)Math.Abs(NodePosition.x - EndPos.x) * 10;
        hyCost = (int)Math.Abs(NodePosition.y - EndPos.y) * 10;
        gCost = fCost + hCost;
    }

    private int CalculateHCost()
    {
        return (int)(Vector2.Distance(NodePosition, EndPos) * 10);
    }

    private int CalculateGCost()
    {
        return Parent.fCost + (int)(Vector2.Distance(NodePosition, Parent.NodePosition) * 10);
    }

    public List<Node> FindNeighbors()
    {
        List<Node> neighbors = new List<Node>();
        neighbors.Add(new Node(NodePosition + Vector2.up, EndPos, this));
        neighbors.Add(new Node(NodePosition + Vector2.down, EndPos, this));
        neighbors.Add(new Node(NodePosition + Vector2.left, EndPos, this));
        neighbors.Add(new Node(NodePosition + Vector2.right, EndPos, this));
        
        return neighbors;
    }
}
