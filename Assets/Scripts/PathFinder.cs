using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PathFinder : MonoBehaviour
{
    // Game Manager
    //public SceneController sceneController => GameObject.Find("SceneController").GetComponent<SceneController>();

    // Node sets
    public List<Node2> openSet = new();
    public List<Node2> closedSet = new();
    public List<Node2> path = new();
    
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

    public bool PathComplete() => !path.Any();

    public Vector2 GetNextPathNode()
    {
        if (path.Any())
        {
            Vector2 position = path[0].nodePosition;
            path.RemoveAt(0);
            return position;
        }
        // (else)
        return new Vector2(-1, -1);
    }

    public List<Node2> CalculatePath(Vector2 start, Vector2 target, GameObject[,] grid)
    {
        Clear();

        startPosition = start;
        endPosition = target;
        int iterations = 0;

        if (CheckValidSpace(target, grid))
        {
            Node2 currentNode = new(start, target, null);
            closedSet.Add(currentNode);
            openSet.AddRange(GetNeighbours(currentNode, grid));
            // Create starting node, add to closedSet
            // add currentNode's neighbours to openSet

            
            while (openSet.Count > 0)
            //for (int i = 1; i <= 1000 && openSet.Count > 0; i++)
            {
                iterations ++;
                foreach (Node2 node in openSet)
                {
                    if (closedSet.Contains(currentNode) || node.fCost < currentNode.fCost) currentNode = node;
                }

                openSet.Remove(currentNode);
                closedSet.Add(currentNode);

                if (currentNode.nodePosition == endPosition)
                {
                    // Path found
                    Node2 pathNode = currentNode;
                    path = Retrace(pathNode);
                }
                else
                {
                    openSet.AddRange(GetNeighbours(currentNode, grid));
                }
            }
            
        }
        
        //Debug.Log($"Iterations: {iterations}");

        return path;
    }

    private List<Node2> Retrace(Node2 finalNode)
    {
        
        List<Node2> retracedPath = new();
        Node2 pathNode = finalNode;
        while (pathNode.Parent != null)
        {
            //Debug.Log("Making openSet nodes");
            closedSet.Remove(pathNode);
            retracedPath.Add(pathNode);
            pathNode = pathNode.Parent;
        }
        retracedPath.Reverse();

        // foreach (Node2 node in retracedPath)
        // {
        //     nodeRef = Instantiate(nodePrefab, new Vector3(node.nodePosition.y, 0.1f, node.nodePosition.x), Quaternion.identity);
        //     nodeRef.GetComponent<NodeVisualiser>().Init(Color.green);
        // }
        //
        // foreach (Node2 node in openSet)
        // {
        //     nodeRef = Instantiate(nodePrefab, new Vector3(node.nodePosition.y, 0.1f, node.nodePosition.x), Quaternion.identity);
        //     nodeRef.GetComponent<NodeVisualiser>().Init(Color.blue);
        // }
        //
        // foreach (Node2 node in closedSet)
        // {
        //     nodeRef = Instantiate(nodePrefab, new Vector3(node.nodePosition.y, 0.1f, node.nodePosition.x), Quaternion.identity);
        //     nodeRef.GetComponent<NodeVisualiser>().Init(Color.red);
        // }
        
        openSet.Clear();
        
        return retracedPath;
    }

    private List<Node2> GetNeighbours(Node2 origin, GameObject[,] grid)
    {
        List<Node2> neighbours = new();
        List<Node2> all = new();
        
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
                    Node2 existingNode = null;
                    existingNode = all.Find(node => node.nodePosition == newPos);
                    if (existingNode == null)
                    {
                        Node2 newNode = new(newPos, endPosition, origin);
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

}
