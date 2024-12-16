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

    public GameObject pathProjection;
    
    // TOOD: Make unit collision script with Bullets from other units
    
    // Start is called before the first frame update
    void Start() {
        // https://docs.unity3d.com/6000.0/Documentation/ScriptReference/GameObject.Find.html
        sceneController = GameObject.Find("SceneController").GetComponent<SceneController>();
        
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
            
            if (nodes.Count > 0)
            {
                if (moving)
                {
                    GetDistance();
                    Debug.Log($"Getting Distance to {nodes[0].NodePosition}");
                }
                else
                {
                    Move();
                    Debug.Log($"Starting move to {nodes[0].NodePosition}");
                }
            }
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
    
    private static List<Node> RetraceAStar2(Node lastNode)
    {
        List<Node> path = new List<Node>();

        Node currentNode = lastNode;
        
        while (currentNode.Parent != null)
        {
            path.Add(currentNode);
            currentNode = currentNode.Parent;
        }

        path.Reverse();
        return path;
    }

    
    public void AStar(Vector2 startPos, Vector2 endPos, GameObject[,] grid)
    {
        // SETUP:
        // WE WANT THREE queues. An open list, a closed list and a path
        // WE ALSO WANT 2 starting nodes
        List<Node> path = new();
        //PriorityQueue<Node, int> openSet = new(new MaxHeapCompare());
        //PriorityQueue<Node, int> closedSet = new(new MaxHeapCompare());
        PriorityQueue<Node, int> openSet = new();
        List<Vector2> openSetCoords = new List<Vector2>();
        List<Vector2> closedSet = new();
        
        Node startNode = new(startPos, endPos, null);
        Node endNode = new(endPos, endPos, null);
        
        openSet.Enqueue(startNode, startNode.gCost);
        openSetCoords.Add(startNode.NodePosition);
        int count = 0;


        while (openSet.Count > 0)
        {
            if (count >= 10000)
            {
                Debug.Log(openSet.Count);
                throw new Exception("Too many open nodes.");
            }

            count++;
            openSet.TryDequeue(out Node currentNode, out int priority);
            Debug.Log($"CURRENT NODE {currentNode.NodePosition}, {priority}");

            // THERE SHOULDN'T BE AN ERROR HERE BECAUSE THERE WILL ALWAYS BE AT LEAST 1 NODE
            // IN THE OPEN SET
            if (currentNode!.NodePosition == endNode.NodePosition)
            {
                Debug.Log($"FINAL POSITION FOUND AFTER SEARCHING {count} TILES");
                path = RetraceAStar2(currentNode);
                Debug.Log(path.Count);
                break;
            }

            List<Node> neighbors = currentNode.FindNeighbors();
            foreach (var neighbor in neighbors)
            {
                if (currentNode.fCost + 10 <= neighbor.fCost &&
                    grid[(int)neighbor.NodePosition.x, (int)neighbor.NodePosition.y].name ==
                    "EmptyNode" &&
                    !openSetCoords.Contains(neighbor.NodePosition)
                   )
                {
                    openSet.Enqueue(neighbor, neighbor.gCost);
                    openSetCoords.Add(neighbor.NodePosition);
                    Debug.Log($"Adding {neighbor.NodePosition} with cost {neighbor.gCost}");
                    Debug.Log($"Open set count: {openSet.Count}");
                }
                else if (!closedSet.Contains(neighbor.NodePosition))
                {
                    closedSet.Add(neighbor.NodePosition);
                }

                Debug.Log(
                    $"Current {currentNode.NodePosition}, {currentNode.fCost} vs Neighbour {neighbor.NodePosition}, {neighbor.fCost}");
            }

            closedSet.Add(currentNode.NodePosition);
        }

        nodes = path;
    }

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