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
using Random = System.Random;

public class BaseUnit : MonoBehaviour
{
    public enum UnitStates
    {
        Idle, 
        Flee,
        Pathfinding,
        Roam,
        Attack
    }
    
    public int maxHealth = 100;
    public int maxAmmo = 100;
    public float moveSpeed = 10f;
    public int teamNumber;
    public UnitStates state = UnitStates.Idle;
    
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
    public bool fleeing = false;
    
    // ========================================================================================

    public PathFinder pathFinder;
    public List<Node> Path = new();
    public bool findingPath = false;
    
    // ========================================================================================
    
    // TODO: Make unit collision script with Bullets from other units
    
    // Start is called before the first frame update
    void Start() {
        // https://docs.unity3d.com/6000.0/Documentation/ScriptReference/GameObject.Find.html
        sceneController = GameObject.Find("SceneController").GetComponent<SceneController>();
        pathFinder = GetComponent<PathFinder>();
        spawnCoords = team.spawnCoords;
        spawnRadius = team.spawnRadius;
        teamNumber = team.teamNumber;
        
        // Round the positions to place them on the map properly
        // https://www.techiedelight.com/round-float-to-2-decimal-points-csharp/
        // Decimal newX = Decimal.Round((decimal)transform.position.x, 0);
        // Decimal newZ = Decimal.Round((decimal)transform.position.z, 0);
    }

    // Update is called once per frame
    void Update()
    {
        ControlStates();
        
        switch (state)
        {
            case UnitStates.Idle:
                break;
            case UnitStates.Pathfinding:
                PathFind();
                break;
            case UnitStates.Roam:
                Roam();
                break;
            case UnitStates.Flee:
                Flee();
                break;
            case UnitStates.Attack:
                //Attack();
                break;
        }
    }


    private void ControlStates()
    {
        // if the health is equal to or below zero then kill the unit
        if (currentHealth <= 0)
        {
            team.DisableUnit(gameObject);
            return;
        }

        if (Vector2.Distance(currentPos, new Vector2(spawnCoords.y, spawnCoords.x)) <= team.spawnRadius)
        {
            if (currentHealth < maxHealth)
            {
                Heal(1);
                return;
            }
        
            fleeing = false;
        }
        
        state = UnitStates.Roam;

        if (findingPath)
        {
            state = UnitStates.Idle;
        }
        
        // if the current health is below 30% of the max health, flee
        // can't have Path.Count == 0 because fleeing should occur regardless
        if (currentHealth < 3 * (maxHealth / 10) && !findingPath && !fleeing) 
        {
            state = UnitStates.Flee;
            fleeing = true;
        }
        
        // If the path count is greater than zero, set state to pathfind
        try
        {
            if (Path.Count <= 0) return; 
            // Debug.Log(Path.Count);
            state = UnitStates.Pathfinding;
        }
        catch (InvalidOperationException)
        {
            Debug.Log("Node path is now empty");
        }
    }

    private void PathFind()
    {
        if (!moving)
        {
            sceneController.Grid[(int)currentPos.x, (int)currentPos.y] = sceneController.Grid[(int)Path[0].nodePosition.x, (int)Path[0].nodePosition.y];
            sceneController.Grid[(int)Path[0].nodePosition.x, (int)Path[0].nodePosition.y] = gameObject;
            currentPos = Path[0].nodePosition;
            moving = true;
        }
        else
        {
            GetDistance();
        }
    }

    // The function that runs when the unit's state is set to "Flee"
    private void Flee()
    {
        Vector2 fleeCoords = PathFinder.CheckSpawnCoordinates(this, spawnCoords, spawnRadius, sceneController.Grid);
        // if the 
        if (fleeCoords == Vector2.zero)
        {
            // pick a random direction and go in that direction until it hits something
            Roam();
            return;
        }
        
        // Otherwise
        StartCoroutine(StartGetPath(currentPos, fleeCoords, sceneController.Grid));
        // Debug.Log($"Fleeing to {fleeCoords}");
    }

    /*
    // Runs in a random direction until it hits something
    private void Roam()
    {
        Vector2 startPos = currentPos;
        Random random = new Random();
        GameObject[,] grid = sceneController.Grid;
        int direction = random.Next(0, 4);
        
        switch (direction)
        {
            case 0:
                startPos.x -= 1;
                break;
            case 1:
                startPos.y -= 1;
                break;
            case 2:
                startPos.x += 1;
                break;
            case 3:
                startPos.y += 1;
                break;
        }
        
        while (!grid[(int)startPos.y, (int)startPos.x])
        {
            switch (direction)
            {
                case 0:
                    startPos.x -= 1;
                    break;
                case 1:
                    startPos.y -= 1;
                    break;
                case 2:
                    startPos.x += 1;
                    break;
                case 3:
                    startPos.y += 1;
                    break;
            }
        }
        Debug.Log($"final position for unit in team {team}: {startPos}");
        StartCoroutine(StartGetPath(currentPos, startPos, grid));
        state = UnitStates.Pathfinding;
    }
    */

    private void Roam()
    {
        Random random = new Random();
        int direction = random.Next(0, 4);
        Node node;
        switch (direction)
        {
            case 0:
                node = new Node(new Vector2(currentPos.x + 1, currentPos.y), new Vector2(currentPos.x + 1, currentPos.y), null);
                if (sceneController.Grid[(int)currentPos.x + 1, (int)currentPos.y]) break;
                Path.Add(node);
                // Debug.Log($"Team {team} roaming to node {node.nodePosition} from position {currentPos}");
                break;
            case 1:
                node = new Node(new Vector2(currentPos.x, currentPos.y + 1), new Vector2(currentPos.x, currentPos.y + 1), null);
                if (sceneController.Grid[(int)currentPos.x, (int)currentPos.y + 1]) break;
                Path.Add(node);
                // Debug.Log($"Team {team} roaming to node {node.nodePosition} from position {currentPos}");
                break;
            case 2:
                node = new Node(new Vector2(currentPos.x - 1, currentPos.y), new Vector2(currentPos.x - 1, currentPos.y), null);
                if (sceneController.Grid[(int)currentPos.x - 1, (int)currentPos.y]) break;
                Path.Add(node);
                // Debug.Log($"Team {team} roaming to node {node.nodePosition} from position {currentPos}");
                break;
            case 3:
                node = new Node(new Vector2(currentPos.x, currentPos.y - 1), new Vector2(currentPos.x, currentPos.y - 1), null);
                if (sceneController.Grid[(int)currentPos.x, (int)currentPos.y - 1]) break;
                Path.Add(node);
                // Debug.Log($"Team {team} roaming to node {node.nodePosition} from position {currentPos}");
                break;
        }
    }

    private void GetDistance()
    {
        // if the current and next positions are within 0.1f of each other
        if (Vector3.Distance(transform.position, new Vector3(Path[0].nodePosition.y, 1f, Path[0].nodePosition.x)) <= 0.1f)
        {
            // set moving to false
            moving = false;
            // make the unit's actual position the new position
            transform.position = new Vector3(Path[0].nodePosition.y, 1f, Path[0].nodePosition.x);
            // remove the first node
            Path.RemoveAt(0);
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

    public IEnumerator StartGetPath(Vector2 startPos, Vector2 endPos, GameObject[,] grid)
    {   
        var t = new Task<List<Node>>(() => pathFinder.CalculatePath(startPos, endPos, grid));

        try
        {
            t.Start();
        }
        catch (Exception e)
        { 
            Debug.Log(e);
        }
        
        findingPath = true;
        targetPos = endPos;

        while (!t.IsCompleted)
        {
            yield return null;
        }

        if (!t.IsCompletedSuccessfully)
        {
            // Debug.Log($"Failed to get path: {t.Exception}");
            throw new Exception("Failed to get path");
        }

        if (!t.IsCompleted)
        {
            yield break;
        }
        
        Path = t.Result;
        findingPath = false;
        // Debug.Log($"Node position: {t.Result.Count}");
        // Debug.Log("Task finished");
        yield return null;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
    }

    public void Heal(int healAmount)
    {
        currentHealth += healAmount;
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