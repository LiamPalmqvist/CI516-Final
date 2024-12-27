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
        Clear();
        
        Debug.Log("Hello!");

        Debug.Log($"{start}, {target}");
        
        startPosition = start;
        endPosition = target;
        int iterations = 0;

        if (CheckValidSpace(target, grid))
        {
            Node currentNode = new(start, target, null);
            closedSet.Add(currentNode);
            openSet.AddRange(GetNeighbours(currentNode, grid));
            // Create starting node, add to closedSet
            // add currentNode's neighbours to openSet

            Debug.Log($"Current NODE: {currentNode}");
            
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
                    Debug.Log($"PATH FOUND AT {endPosition}");
                    // Path found
                    Node pathNode = currentNode;
                    path = Retrace(pathNode);
                }
                else
                {
                    openSet.AddRange(GetNeighbours(currentNode, grid));
                }
            }
            
        }
        
        //Debug.Log($"Iterations: {iterations}");
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

    private bool CheckValidSpace(Vector2 position, GameObject[,] grid)
    {
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
    public static Vector2 CheckSpawnCoordinates(BaseUnit unit, Vector2 spawnPosition, int radius, GameObject[,] grid)
    {
        Vector2 newSpawnPosition = new Vector2(0, 0);
        float bestDistanceToSpawn = Mathf.Infinity;
        float closestDistanceToUnit = Mathf.Infinity;
        
        // loop through vertically
        for (int y = (int)spawnPosition.x-radius; y < (int)spawnPosition.x+radius; y++)
        {
            // loop through horizontally
            for (int x = (int)spawnPosition.y-radius; x < (int)spawnPosition.y+radius; x++)
            {
                Vector2 gridPosition = new Vector2(y, x);
                Debug.Log(gridPosition);
                
                // if the grid position IS FILLED, continue to next loop
                try
                {
                    if (grid[x, y])
                    {
                        Debug.Log($"Grid position {gridPosition.x}, {gridPosition.y} filled");
                        continue;
                    }
                }
                // if the index is outside the range of the grid, i.e. (-1, 25)
                // continue to the next loop
                catch (IndexOutOfRangeException e)
                {
                    Debug.Log(e);
                    continue;
                }
                
                // if the new position IS the spawn position, continue to next loop
                if (gridPosition == spawnPosition) continue;

                Debug.Log($"{grid[x, y]} is free");
                
                // if the distance from the current grid position to the spawn coords
                // is LESS than the current best distance
                if (Vector2.Distance(spawnPosition, gridPosition) < bestDistanceToSpawn)
                {
                    // then the new best distance is the distance from the spawn position to the current
                    // grid position
                    bestDistanceToSpawn = Vector2.Distance(spawnPosition, gridPosition);
                    newSpawnPosition = new Vector2(x, y);
                    Debug.Log($"{newSpawnPosition} is the new best position with distance of {bestDistanceToSpawn}");
                
                } 
                // Otherwise if the distance is APPROXIMATELY THE SAME
                else if (Mathf.Approximately(Vector2.Distance(spawnPosition, gridPosition), bestDistanceToSpawn))
                {
                    // IF the distance from the current grid position is LESS than the current 
                    // closest distance to the unit
                    if (Vector2.Distance(gridPosition, unit.currentPos) <= closestDistanceToUnit)
                    {
                        // Make that grid position the new best
                        closestDistanceToUnit = Vector2.Distance(gridPosition, unit.currentPos);
                        bestDistanceToSpawn = Vector2.Distance(spawnPosition, gridPosition);
                        newSpawnPosition = new Vector2(x, y);
                        Debug.Log($"{newSpawnPosition} is now the new best position");
                    }
                }
            }
        }
        
        // Finally, return the new position to travel to
        return newSpawnPosition;
    }

    public static List<BaseUnit> GetUnitsInArea(Vector2 startPosition, Vector2 endPosition, GameObject[,] grid)
    {
        List<BaseUnit> units = new();
        if (startPosition.x <= endPosition.x)
            for (int x = (int)startPosition.x; x < (int)endPosition.x; x++)
            {
                if (startPosition.y <= endPosition.y)
                    for (int y = (int)startPosition.y; y < (int)endPosition.y; y++)
                    {
                        try
                        {
                            if (grid[x, y].name == "EntityPrefab(Clone)")
                                units.Add(grid[y, x].GetComponent<BaseUnit>());
                        }
                        catch (Exception e)
                        {
                            Debug.Log(e);
                        }
                    }
                else
                    for (int y = (int)endPosition.y; y < (int)startPosition.y; y++)
                    {
                        try
                        {
                            if (grid[x, y].name == "EntityPrefab(Clone)")
                                units.Add(grid[y, x].GetComponent<BaseUnit>());
                        }
                        catch (Exception e)
                        {
                            Debug.Log(e);
                        }
                    }
            }
        else
            for (int x = (int)endPosition.x; x < (int)startPosition.x; x++)
            {
                if (startPosition.y <= endPosition.y)
                    for (int y = (int)startPosition.y; y < (int)endPosition.y; y++)
                    {
                        try
                        {
                            if (grid[x, y].name == "EntityPrefab(Clone)")
                                units.Add(grid[y, x].GetComponent<BaseUnit>());
                        }
                        catch (Exception e)
                        {
                            Debug.Log(e);
                        }
                    }
                else
                    for (int y = (int)endPosition.y; y < (int)startPosition.y; y++)
                    {
                        try
                        {
                            if (grid[x, y].name == "EntityPrefab(Clone)")
                                units.Add(grid[y, x].GetComponent<BaseUnit>());
                        }
                        catch (Exception e)
                        {
                            Debug.Log(e);
                        }
                    }
            }

        return units;
    }

    private void printPath(List<Node> path)
    {
        Debug.Log("Path retracing");
        for (int i = 0; i < path.Count - 1; i++)
        {
            Debug.Log($"Retraced node position {path[i].nodePosition}");
        }
    }

}
