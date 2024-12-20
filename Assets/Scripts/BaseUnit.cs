using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Utils;
using ArgumentNullException = System.ArgumentNullException;

public class BaseUnit : MonoBehaviour
{
    public enum UnitStates
    {
        Idle, 
        MoveTo,
        Flee,
    }
    
    public int maxHealth = 100;
    public int maxAmmo = 100;
    public float moveSpeed = 10f;
    public int teamNumber;
    
    [SerializeField] SceneController sceneController; // Controls which units are in what places
    [SerializeField] public TeamClass team;
    
    [SerializeField] int currentHealth;
    [SerializeField] int currentAmmo;

    [SerializeField] int currentTeamNumber;
    [SerializeField] private Vector2 spawnCoords;
    [SerializeField] private int spawnRadius;
    public Vector2 currentPos;
    public Vector2 targetPos;
    public bool moving = false;
    private Vector2 direction;
    public List<Node> nodes = new List<Node>();
    public List<GameObject> pathInstances = new List<GameObject>();

    // ========================================================================================
    // THIS IS FOR THE ITERATIVE VERSION OF THE A* ALGORITHM
    // private bool _findingPath = false;
    // public List<Node> _path = new();
    // private PriorityQueue<Node, int> _openSet = new ();
    // private HashSet<Vector2> _openSetCoords = new(); // Hash sets can ONLY contain unique items, this means they are optimised for sorting and searching
    // private List<Vector2> _closedSetCoords = new();
    // private GameObject[,] _grid;
    // private int[,] _gridInts;
    // // temp var
    // private int count;
    //     
    // // set up the start and end nodes
    // private Node _startNode = new(new Vector2(), new Vector2(), null, null);
    // private Node _endNode = new(new Vector2(), new Vector2(), null, null);
    // ========================================================================================
    
    
    // ========================================================================================

    public PathFinder pathFinder;
    public List<Node2> _path2 = new();
    
    // ========================================================================================
    
    // TODO: Make unit collision script with Bullets from other units
    
    // Start is called before the first frame update
    void Start() {
        // https://docs.unity3d.com/6000.0/Documentation/ScriptReference/GameObject.Find.html
        sceneController = GameObject.Find("SceneController").GetComponent<SceneController>();
        pathFinder = GetComponent<PathFinder>();
        spawnCoords = team.spawnCoords;
        spawnRadius = team.spawnRadius;
        
        // Round the positions to place them on the map properly
        // https://www.techiedelight.com/round-float-to-2-decimal-points-csharp/
        // Decimal newX = Decimal.Round((decimal)transform.position.x, 0);
        // Decimal newZ = Decimal.Round((decimal)transform.position.z, 0);
    }

    // Update is called once per frame
    void Update()
    {
        ControlStates();
    }
    
    private void ControlStates()
    {
        // if the health is equal to or below zero then kill the unit
        if (currentHealth <= 0)
        {
            team.DisableUnit(gameObject);
            return;
        }
        
        try
        {
            if (_path2.Count > 0)
            {
                if (moving)
                {
                    GetDistance2();
                    return;
                }

                Move2();
                return;
            }
            
            // if (_findingPath)
            // {
            //     Debug.Log("Finding path");
            //     IterateAStar();
            //     return;
            // }
            //
            // if (_path.Count > 0)
            // {
            //     if (moving)
            //     {
            //         Debug.Log($"Getting Distance to {_path[0].NodePosition}");
            //         NewGetDistance();
            //         return;
            //     }
            //     
            //     Debug.Log($"Starting move to {_path[0].NodePosition}");
            //     NewMove();
            //     return;
            // }
            //
            // if (nodes.Count > 0)
            // {
            //     if (moving)
            //     {
            //         GetDistance();
            //         Debug.Log($"Getting Distance to {nodes[0].NodePosition}");
            //     }
            //     else
            //     {
            //         Move();
            //         Debug.Log($"Starting move to {nodes[0].NodePosition}");
            //     }
            // }
        }
        catch (InvalidOperationException)
        {
            Debug.Log("Node path is now empty");
        }
    }
    
    private void Move()
    {
        sceneController.Grid[(int)currentPos.x, (int)currentPos.y] = sceneController.Grid[(int)nodes[0].NodePosition.x, (int)nodes[0].NodePosition.y];
        sceneController.Grid[(int)nodes[0].NodePosition.x, (int)nodes[0].NodePosition.y] = gameObject;
        currentPos = nodes[0].NodePosition;
        moving = true;
    }

    private void GetDistance()
    {
        // if the current and next positions are within 0.1f of eachother
        if (Vector3.Distance(transform.position, new Vector3(nodes[0].NodePosition.y, 1f, nodes[0].NodePosition.x)) <= 0.1f)
        {
            // set moving to false
            moving = false;
            // make the unit's actual position the new position
            transform.position = new Vector3(nodes[0].NodePosition.y, 1f, nodes[0].NodePosition.x);
            // remove the first node
            nodes.RemoveAt(0);
        }
        else
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                new Vector3(currentPos.y, 1f, currentPos.x), 
                moveSpeed * Time.deltaTime
            );
        }
    }
    
    // private void NewMove()
    // {
    //     sceneController.Grid[(int)currentPos.x, (int)currentPos.y] = sceneController.Grid[(int)_path[0].NodePosition.x, (int)_path[0].NodePosition.y];
    //     sceneController.Grid[(int)_path[0].NodePosition.x, (int)_path[0].NodePosition.y] = gameObject;
    //     currentPos = _path[0].NodePosition;
    //     moving = true;
    // }
    //
    // private void NewGetDistance()
    // {
    //     // if the current and next positions are within 0.1f of eachother
    //     if (Vector3.Distance(transform.position, new Vector3(_path[0].NodePosition.y, 1f, _path[0].NodePosition.x)) <= 0.1f)
    //     {
    //         // set moving to false
    //         moving = false;
    //         // make the unit's actual position the new position
    //         transform.position = new Vector3(_path[0].NodePosition.y, 1f, _path[0].NodePosition.x);
    //         // remove the first node
    //         _path.RemoveAt(0);
    //     }
    //     else
    //     {
    //         transform.position = Vector3.MoveTowards(
    //             transform.position,
    //             new Vector3(currentPos.y, 1f, currentPos.x), 
    //             moveSpeed * Time.deltaTime
    //         );
    //     }
    // }

    private void Move2()
    {
        sceneController.Grid[(int)currentPos.x, (int)currentPos.y] = sceneController.Grid[(int)_path2[0].nodePosition.x, (int)_path2[0].nodePosition.y];
        sceneController.Grid[(int)_path2[0].nodePosition.x, (int)_path2[0].nodePosition.y] = gameObject;
        currentPos = _path2[0].nodePosition;
        moving = true;
    }

    private void GetDistance2()
    {
        // if the current and next positions are within 0.1f of each other
        if (Vector3.Distance(transform.position, new Vector3(_path2[0].nodePosition.y, 1f, _path2[0].nodePosition.x)) <= 0.1f)
        {
            // set moving to false
            moving = false;
            // make the unit's actual position the new position
            transform.position = new Vector3(_path2[0].nodePosition.y, 1f, _path2[0].nodePosition.x);
            // remove the first node
            _path2.RemoveAt(0);
        }
        else
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                new Vector3(currentPos.y, 1f, currentPos.x), 
                moveSpeed * Time.deltaTime
            );
        }
    }
    
    // private static List<Node> RetraceAStar(Node lastNode)
    // {
    //     List<Node> path = new List<Node>();
    //
    //     Node currentNode = lastNode;
    //     
    //     while (currentNode.Parent != null)
    //     {
    //         path.Add(currentNode);
    //         currentNode = currentNode.Parent;
    //     }
    //
    //     path.Reverse();
    //     return path;
    // }
    //
    // public void StartAStar(Vector2 startPos, Vector2 endPos, GameObject[,] sceneGrid, int[,] gridInts)
    // {
    //     // This sets up the variables for the A* algorithm to work
    //     _path = new();
    //     _openSet = new();
    //     _openSetCoords = new();
    //     _closedSetCoords = new();
    //     
    //     // set up the start and end nodes
    //     _startNode = new Node(startPos, endPos, null, gridInts);
    //     _endNode = new Node(endPos, endPos, null, gridInts);
    //
    //     _openSet.Enqueue(_startNode, _startNode.GCost);
    //     _openSetCoords.Add(_startNode.NodePosition);
    //
    //     _grid = sceneGrid;
    //     _gridInts = gridInts;
    //     
    //     count = 0; // temp var
    //     
    //     _findingPath = true;
    //     
    //     Debug.Log("Setup finished");
    // }
    //
    // // This should be faster (in theory) than the other version
    // private void IterateAStar()
    // {
    //     count++;
    //     if (_openSet.Count >= 10000)
    //     {
    //         // Debug.Log($"{openSet.Count} nodes in openSet");
    //         throw new Exception("openSet is too large.");
    //     }
    //     if (_openSet.Count > 0)
    //     {
    //         _openSet.TryDequeue(out Node currentNode, out int _);
    //
    //         if (currentNode.NodePosition == _endNode.NodePosition)
    //         {
    //             Debug.Log(count);
    //             _path = RetraceAStar(currentNode);
    //             Debug.Log("Path found");
    //             _findingPath = false;
    //         }
    //         
    //         List<Node> neighbors = currentNode.FindNeighbors();
    //
    //         foreach (var neighbor in neighbors)
    //         {
    //             if (currentNode.FCost + 10 <= neighbor.FCost &&
    //                 _grid[(int)neighbor.NodePosition.x, (int)neighbor.NodePosition.y] ==
    //                 null &&
    //                 !_openSetCoords.Contains(neighbor.NodePosition) && 
    //                 !_closedSetCoords.Contains(neighbor.NodePosition)
    //                )
    //             {
    //                 _openSet.Enqueue(neighbor, neighbor.GCost);
    //                 _openSetCoords.Add(neighbor.NodePosition);
    //                 Debug.Log($"Adding {neighbor.NodePosition} with cost {neighbor.GCost}");
    //                 Debug.Log($"Open set count: {_openSetCoords.Count}");
    //             }
    //             else
    //                 _closedSetCoords.Add(neighbor.NodePosition);
    //
    //             Debug.Log(
    //                  $"Current {currentNode.NodePosition}, {currentNode.FCost} vs Neighbour {neighbor.NodePosition}, {neighbor.FCost}");
    //         }
    //
    //         _closedSetCoords.Add(currentNode.NodePosition);
    //     }
    // }

    // ================================================================================================
    // ================================================================================================
    // ================================================================================================

    // public IEnumerator StartGetPath(Vector2 startPos, Vector2 endPos, int[,] gridInts)
    // {
    //     count = 0;
    //     var t = Task.Run(() => GetPath(startPos, endPos, gridInts));
    //
    //     while (!t.IsCompleted)
    //     {
    //         count++;
    //         yield return null;
    //     }
    //
    //     if (!t.IsCompletedSuccessfully)
    //     {
    //         Debug.LogError($"Failed to get path: {t.Exception}");
    //         throw new Exception("Failed to get path");
    //     } 
    //     
    //     if (t.IsCompleted)
    //     {
    //         Debug.Log(count);
    //         _path = t.Result;
    //         Debug.Log(_path);
    //         Debug.Log("Task completed");
    //         yield return null;
    //     }
    // }
    //
    // /*
    // private async Task<List<Node>> GetPathTask(Vector2 startPos, Vector2 endPos, GameObject[,] sceneGrid)
    // {
    //     return await GetPath(startPos, endPos, sceneGrid);
    // }
    // */
    //
    // public List<Node> GetPath(Vector2 startPos, Vector2 endPos, int[,] gridInts)
    // {
    //     // This sets up the variables for the A* algorithm to work
    //     List<Node> path = new();
    //     PriorityQueue<Node, int> openSet = new();
    //     HashSet<Vector2> openSetCoords = new();
    //     List<Vector2> closedSetCoords = new();
    //     
    //     // set up the start and end nodes
    //     Node startNode = new Node(startPos, endPos, null, gridInts);
    //     Node endNode = new Node(endPos, endPos, null, gridInts);
    //
    //     openSet.Enqueue(startNode, startNode.GCost);
    //     openSetCoords.Add(startNode.NodePosition);
    //     
    //     int count = 0; // temp var
    //     
    //     bool findingPath = true;
    //     
    //     Debug.Log("Setup finished");
    //     count++;
    //     if (openSet.Count >= 10000)
    //     {
    //         // Debug.Log($"{openSet.Count} nodes in openSet");
    //         throw new Exception("openSet is too large.");
    //     }
    //
    //     while (openSet.Count > 0)
    //     {
    //         openSet.TryDequeue(out Node currentNode, out int _);
    //
    //         if (currentNode.NodePosition == endNode.NodePosition)
    //         {
    //             Debug.Log(count);
    //             path = RetraceAStar(currentNode);
    //             // path = RetraceAStar(currentNode);
    //             Debug.Log("Path found");
    //             // findingPath = false;
    //         }
    //         List<Node> neighbors = currentNode.FindNeighbors();
    //
    //         foreach (var neighbor in neighbors)
    //         {
    //             try
    //             {
    //                 Debug.Log(neighbor);
    //                 if (currentNode.FCost + 10 <= neighbor.FCost &&
    //                     neighbor.nameValue == 1 &&
    //                     !openSetCoords.Contains(neighbor.NodePosition) &&
    //                     !closedSetCoords.Contains(neighbor.NodePosition)
    //                    )
    //                 {
    //                     openSet.Enqueue(neighbor, neighbor.GCost);
    //                     openSetCoords.Add(neighbor.NodePosition);
    //                     Debug.Log($"Adding {neighbor.NodePosition} with cost {neighbor.GCost}");
    //                     Debug.Log($"Open set count: {openSetCoords.Count}");
    //                 }
    //                 else
    //                     closedSetCoords.Add(neighbor.NodePosition);
    //
    //                 Debug.Log(openSetCoords.Count);
    //
    //                 Debug.Log(
    //                     $"Current {currentNode.NodePosition}, {currentNode.FCost} vs Neighbour {neighbor.NodePosition}, {neighbor.FCost}");
    //             }
    //             catch (Exception e)
    //             {
    //                 Debug.LogError(e);
    //             }
    //         }
    //         Debug.Log("4");
    //         closedSetCoords.Add(currentNode.NodePosition);
    //     }
    //     Debug.Log("5");
    //     return path;
    // }
    
    // ================================================================================================
    // ================================================================================================
    // ================================================================================================
    
    // public void AStar(Vector2 startPos, Vector2 endPos, GameObject[,] grid)
    // {
    //     // SETUP:
    //     // WE WANT THREE queues. An open list, a closed list and a path
    //     // WE ALSO WANT 2 starting nodes
    //     List<Node> path = new();
    //     //PriorityQueue<Node, int> openSet = new(new MaxHeapCompare());
    //     //PriorityQueue<Node, int> closedSet = new(new MaxHeapCompare());
    //     PriorityQueue<Node, int> openSet = new();
    //     List<Vector2> openSetCoords = new List<Vector2>();
    //     List<Vector2> closedSet = new();
    //     
    //     Node startNode = new(startPos, endPos, null, _gridInts);
    //     Node endNode = new(endPos, endPos, null, _gridInts);
    //     
    //     openSet.Enqueue(startNode, startNode.GCost);
    //     openSetCoords.Add(startNode.NodePosition);
    //     int count = 0;
    //
    //
    //     while (openSet.Count > 0)
    //     {
    //         if (count >= 10000)
    //         {
    //             Debug.Log(openSet.Count);
    //             throw new Exception("Too many open nodes.");
    //         }
    //
    //         count++;
    //         openSet.TryDequeue(out Node currentNode, out int priority);
    //         Debug.Log($"CURRENT NODE {currentNode.NodePosition}, {priority}");
    //
    //         // THERE SHOULDN'T BE AN ERROR HERE BECAUSE THERE WILL ALWAYS BE AT LEAST 1 NODE
    //         // IN THE OPEN SET
    //         if (currentNode!.NodePosition == endNode.NodePosition)
    //         {
    //             Debug.Log($"FINAL POSITION FOUND AFTER SEARCHING {count} TILES");
    //             path = RetraceAStar(currentNode);
    //             Debug.Log(path.Count);
    //             break;
    //         }
    //
    //         List<Node> neighbors = currentNode.FindNeighbors();
    //         foreach (var neighbor in neighbors)
    //         {
    //             if (currentNode.FCost + 10 <= neighbor.FCost &&
    //                 grid[(int)neighbor.NodePosition.x, (int)neighbor.NodePosition.y] == null &&
    //                 !openSetCoords.Contains(neighbor.NodePosition)
    //                )
    //             {
    //                 openSet.Enqueue(neighbor, neighbor.GCost);
    //                 openSetCoords.Add(neighbor.NodePosition);
    //                 Debug.Log($"Adding {neighbor.NodePosition} with cost {neighbor.GCost}");
    //                 Debug.Log($"Open set count: {openSet.Count}");
    //             }
    //             else if (!closedSet.Contains(neighbor.NodePosition))
    //             {
    //                 closedSet.Add(neighbor.NodePosition);
    //             }
    //
    //             Debug.Log(
    //                 $"Current {currentNode.NodePosition}, {currentNode.FCost} vs Neighbour {neighbor.NodePosition}, {neighbor.FCost}");
    //         }
    //
    //         closedSet.Add(currentNode.NodePosition);
    //     }
    //
    //     nodes = path;
    // }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
    }

    public void Respawn()
    {
        currentHealth = maxHealth;
        currentAmmo = maxAmmo;
    }
}

// Custom sorter to make sure that the PriorityQueues are in Max Heap order where elements go from
// BIGGER -> smaller (meaning the last item in the queue is the smallest)
// as opposed to Min Heap order where elements go from smaller -> BIGGER
// (meaning the last item in the queue is the biggest)
// This isn't actually required since we want the elements in order of smallest -> BIGGEST
public class MaxHeapCompare : IComparer<int>
{
    public int Compare(int x, int y) => x.CompareTo(y);
}