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
    public readonly Vector2 NodePosition;
    private readonly Vector2 _endPos;
    public readonly Node Parent;
    public readonly int GCost; // The cost from the start node to the current node
    private readonly int _hCost; // The hypothetical cost from the current node to the end node
    public readonly int FCost; // The gCost and the hCost added together
    public Node(Vector2 position, Vector2 endPosition, Node parent)
    {
        NodePosition = position;
        _endPos = endPosition;
        if (parent is null)
        {
            FCost = 0;
        }
        else
        {
            Parent = parent;
            FCost = CalculateGCost();
        }
        _hCost = CalculateHCost();
        GCost = FCost + _hCost;
    }

    private int CalculateHCost()
    {
        return (int)(Vector2.Distance(NodePosition, _endPos) * 10);
    }

    private int CalculateGCost()
    {
        return Parent.FCost + (int)(Vector2.Distance(NodePosition, Parent.NodePosition) * 10);
    }

    public List<Node> FindNeighbors()
    {
        List<Node> neighbors = new List<Node>();
        neighbors.Add(new Node(NodePosition + Vector2.up, _endPos, this));
        neighbors.Add(new Node(NodePosition + Vector2.down, _endPos, this));
        neighbors.Add(new Node(NodePosition + Vector2.left, _endPos, this));
        neighbors.Add(new Node(NodePosition + Vector2.right, _endPos, this));
        
        return neighbors;
    }
}
