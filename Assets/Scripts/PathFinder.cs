using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

public class PathFinder : MonoBehaviour
{
    // Game Manager
    //public SceneController sceneController => GameObject.Find("SceneController").GetComponent<SceneController>();

    // Node sets
    public List<Node> openSet = new();
    public List<Node> closedSet = new();
    public List<Node> path = new();
    
    // public HashSet<Node> closedSet = new();
    // public PriorityQueue<Node> openSet = new();
    
    // Positions
    public Vector2 startPosition;
    public Vector2 endPosition;
    
    // Visualisers
    public GameObject nodePrefab;
    public GameObject nodeRef;
    public Transform nodeParent;
    
    private void Clear()
    {
        path.Clear();
        openSet.Clear();
        closedSet.Clear();
        startPosition = Vector2.zero;
        endPosition = Vector2.zero;
    }

    // This assumes that the coordinates ARE flipped from (X, Y) to (Y, X)
    public List<Node> CalculatePath(Vector2 start, Vector2 target, GameObject[,] grid)
    {
        if (start == target)
            return new List<Node>();
        
        Clear();
        
        // Debug.Log("Hello!");

        //Debug.Log($"{start} to {target}");
        //Debug.Log($"{target} is {CheckValidSpace(target, grid)}");
        startPosition = start;
        endPosition = target;
        int iterations = 0;

        Node currentNode = new(start, target, null);
        
        if (CheckValidSpace(target, grid))
        {
            closedSet.Add(currentNode);
            openSet.AddRange(GetNeighbours(currentNode, grid));
            // Create starting node, add to closedSet
            // add currentNode's neighbours to openSet

            // Debug.Log($"Current NODE: {currentNode}");
            
            while (openSet.Count > 0)
            //for (int i = 1; i <= 1000 && openSet.Count > 0; i++)
            {
                iterations ++;
                foreach (Node node in openSet)
                {
                    if (closedSet.Contains(currentNode) || node.fCost < currentNode.fCost) currentNode = node;
                }

                openSet.Remove(currentNode);
                closedSet.Add(currentNode);

                if (currentNode.nodePosition == endPosition)
                {
                    // Debug.Log($"PATH FOUND AT {endPosition}");
                    // Path found
                    Node pathNode = currentNode;
                    path = Retrace(pathNode);
                }
                else
                {
                    openSet.AddRange(GetNeighbours(currentNode, grid));
                }
                
                Debug.Log($"Stopped at node position: {currentNode.nodePosition}");
            }
            
        }
        else
        {
            // Debug.Log("Not a valid space");
        }
        
        //Debug.Log($"Iterations: {iterations}");
        if (path.Count > 0)
        {
            Debug.Log($"Found path from {startPosition} to {endPosition}");
        }
        else
        {
            Debug.Log($"No path from {startPosition} to {endPosition}. Stopped at node position: {currentNode.nodePosition} after {iterations} iteration(s)\n The position was {(CheckValidSpace(currentNode.nodePosition, grid) ? "open" : "closed")} because of {grid[(int)currentNode.nodePosition.y, (int)currentNode.nodePosition.x]}");
        }
        printPath(path);
        return path;
    }

    private List<Node> Retrace(Node finalNode)
    {
        
        List<Node> retracedPath = new();
        Node pathNode = finalNode;
        while (pathNode.Parent != null)
        {
            //Debug.Log("Making openSet nodes");
            closedSet.Remove(pathNode);
            retracedPath.Add(pathNode);
            pathNode = pathNode.Parent;
        }

        retracedPath.Reverse();
        
        openSet.Clear();
        
        return retracedPath;
    }

    private List<Node> GetNeighbours(Node origin, GameObject[,] grid)
    {
        List<Node> neighbours = new();
        List<Node> all = new();
        
        all.AddRange(closedSet);
        all.AddRange(openSet);

        // Vector2 displacement = Vector2.zero;
        Vector2 newPos = Vector2.zero;

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                // if the new positions are diagonal or 0,0
                if (i ==0 && j == 0/*i == j|| i == -j || -i == j*/) continue;
                newPos = origin.nodePosition + new Vector2(j, i);
                
                // Check if the new position is valid
                if (CheckValidSpace(newPos, grid))
                {
                    Node existingNode = null;
                    existingNode = all.Find(node => node.nodePosition == newPos);
                    if (existingNode == null)
                    {
                        Node newNode = new(newPos, endPosition, origin);
                        neighbours.Add(newNode);
                        
                        // Debug.Log($"{newPos} HCost: {newNode.hCost}");
                        // Debug.Log($"{newPos} GCost: {newNode.gCost}");
                        // Debug.Log($"{newPos} FCost: {newNode.fCost}");
                    }
                }
            }
        }
        
        return neighbours;
    }

    public static bool CheckValidSpace(Vector2 position, GameObject[,] grid = null)
    {
        grid ??= GameObject.Find("SceneController").GetComponent<SceneController>().Grid;
        
        int xPos = (int)position.x;
        int zPos = (int)position.y;
        
        // Check if:
        // 1. xPos is greater than or equal to 0
        // 2. if the xPos is less than the length of the playArea's x length
        // 3. if the zPos is greater than or equal to 0
        // 4. if the zPos is less than the length of the playArea's y length
        // 5. if the playArea's[z, x] is equal to null
        return (
            xPos >= 0 &&
            xPos < grid.GetLength(0) &&
            zPos >= 0 &&
            zPos < grid.GetLength(1) &&
            !grid[xPos, zPos]
        );
    }

    // This assumes that the spawn coordinates are already flipped
    // from (X, Y) to (Y, X)
    public static Vector2 FindOpenPosition(Vector2 startPos, Vector2 endPos, int radius, bool hardLimit = false, GameObject[,] grid = null, int iteration = 0, List<Vector2> closedPositions = null)
    {
        // set if default values present - makes it unnecessary to pass as arguments initially
        closedPositions ??= new List<Vector2>();
        grid ??= GameObject.Find("SceneController").GetComponent<SceneController>().Grid;
        
        // if the algorithm has hit the hard limit of iterations return zero.
        // This will save the program from getting stuck
        if (iteration > 5) return Vector2.zero;
        
        float closestDistanceToTarget = Mathf.Infinity;
        Vector2 closestNodeToTarget = Vector2.positiveInfinity;
        List<Vector2> openSet = new();

        for (int x = (int)endPos.x - radius; x < (int)endPos.x + radius; x++)
        {
            for (int y = (int)endPos.y - radius; y < (int)endPos.y + radius; y++)
            {
                if (x == (int)endPos.x && y == (int)endPos.y) continue;
                // This is reversed because we are passing in coords in [x, z]
                // and the grid is in [z, x]
                if (closedPositions.Contains(new Vector2(x, y))) continue;
                
                if (CheckValidSpace(new Vector2(x, y), grid))
                {
                    openSet.Add(new Vector2(x, y));
                }
                else
                {
                    closedPositions.Add(new Vector2(x, y));
                }
            }
        }
        
        // if there are no available spots return Vector2.zero 
        if (openSet.Count == 0 && !hardLimit) return FindOpenPosition(startPos, endPos, radius, false, grid, iteration+1, closedPositions);
        
        foreach (Vector2 pos in openSet)
        {
            if (!(Vector2.Distance(endPos, pos) < closestDistanceToTarget)) continue;
            
            closestDistanceToTarget = Vector2.Distance(endPos, pos);
            closestNodeToTarget = pos;
        }

        Debug.Log($"Closest node was {closestDistanceToTarget} at position {closestNodeToTarget}");
        
        return closestNodeToTarget;
    }
    
    public static List<GameObject> GetUnitsInArea(Vector2Int startPosition, Vector2Int endPosition, List<GameObject> teamUnits)
    {
        // Get all the BaseUnit Components in the parsed units list
        List<BaseUnit> units = new();
        foreach (GameObject unit in teamUnits)
        {
            units.Add(unit.GetComponent<BaseUnit>());
        }
        
        // Check if the unit's
        // 1. X position is less than the start/end position's X value
        // 2. Y position is less than the start/end position's Y value
        // 3. X position is greater than the end/start position's X 
        // 4. Y position is greater than the end/start position's Y
        List<GameObject> selectedUnits = new();
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i].currentPos.x < (startPosition.x < endPosition.x ? endPosition.x : startPosition.x) &&
                units[i].currentPos.x > (startPosition.x < endPosition.x ? startPosition.x : endPosition.x) &&
                units[i].currentPos.y < (startPosition.y < endPosition.y ? endPosition.y : startPosition.y) &&
                units[i].currentPos.y > (startPosition.y < endPosition.y ? startPosition.y : endPosition.y)
               )
            {
                selectedUnits.Add(teamUnits[i]);
            }
        }
        
        return selectedUnits;
    }

    private void printPath(List<Node> path)
    {
        // Debug.Log("Path retracing");
        for (int i = 0; i < path.Count - 1; i++)
        {
            Debug.Log($"Retraced node position {path[i].nodePosition}");
        }
    }

}
