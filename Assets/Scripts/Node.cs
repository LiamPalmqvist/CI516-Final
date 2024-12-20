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
    // Node Costs
    public readonly int GCost; // The cost from the start node to the current node
    private readonly int _hCost; // The hypothetical cost from the current node to the end node
    public readonly int FCost; // The gCost and the hCost added together
    
    // Node Positions
    public readonly Vector2 NodePosition;
    private readonly Vector2 _endPos;
    
    // Node Parent
    public readonly Node Parent; // can be NULL
    private readonly int[,] _grid;
    public readonly int nameValue;
    public Node(Vector2 position, Vector2 endPosition, Node parent, int[,] grid)
    {
        NodePosition = position;
        _endPos = endPosition;
        _grid = grid;
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

        nameValue = GetName();
    }

    private int CalculateHCost()
    {
        return (int)(Vector2.Distance(NodePosition, _endPos) * 10);
    }

    private int CalculateGCost()
    {
        return Parent.FCost + (int)(Vector2.Distance(NodePosition, Parent.NodePosition) * 10);
    }

    private int GetName()
    {
        return _grid[(int)NodePosition.x, (int)NodePosition.y];
    }

    public List<Node> FindNeighbors()
    {
        List<Node> neighbors = new List<Node>();
        neighbors.Add(new Node(NodePosition + Vector2.up, _endPos, this, _grid));
        neighbors.Add(new Node(NodePosition + Vector2.down, _endPos, this, _grid));
        neighbors.Add(new Node(NodePosition + Vector2.left, _endPos, this, _grid));
        neighbors.Add(new Node(NodePosition + Vector2.right, _endPos, this, _grid));
        
        return neighbors;
    }
    
    // Addition of new function CalculateMagnitude
    public int CalculateMagnitude(Vector2 start, Vector2 end) => (int)Vector2.Distance(start, end) * 10;
}

public class Node2
{
    public Node2 Parent;
    public Vector2 nodePosition { get; set; }
    public Vector2 parentPosition { get; set; }
    public int gCost { get; set; }
    public int hCost { get; set; }
    public int fCost { get; set; }

    public Node2(Vector2 NodePos, Vector2 targetPos, Node2 parent)
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
