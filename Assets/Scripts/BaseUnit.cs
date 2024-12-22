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
    public Vector2 currentPos;
    public Vector2 targetPos;
    public bool moving = false;
    private Vector2 direction;
    
    // ========================================================================================

    public PathFinder pathFinder;
    public List<Node> _path = new();
    
    // ========================================================================================
    
    // TODO: Make unit collision script with Bullets from other units
    
    // Start is called before the first frame update
    void Start() {
        // https://docs.unity3d.com/6000.0/Documentation/ScriptReference/GameObject.Find.html
        sceneController = GameObject.Find("SceneController").GetComponent<SceneController>();
        pathFinder = GetComponent<PathFinder>();
        spawnCoords = team.spawnCoords;
        
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
            if (_path.Count > 0)
            {
                if (moving)
                {
                    GetDistance2();
                    return;
                }

                Move2();
            }
        }
        catch (InvalidOperationException)
        {
            Debug.Log("Node path is now empty");
        }
    }

    private void Move2()
    {
        sceneController.Grid[(int)currentPos.x, (int)currentPos.y] = sceneController.Grid[(int)_path[0].nodePosition.x, (int)_path[0].nodePosition.y];
        sceneController.Grid[(int)_path[0].nodePosition.x, (int)_path[0].nodePosition.y] = gameObject;
        currentPos = _path[0].nodePosition;
        moving = true;
    }

    private void GetDistance2()
    {
        // if the current and next positions are within 0.1f of each other
        if (Vector3.Distance(transform.position, new Vector3(_path[0].nodePosition.y, 1f, _path[0].nodePosition.x)) <= 0.1f)
        {
            // set moving to false
            moving = false;
            // make the unit's actual position the new position
            transform.position = new Vector3(_path[0].nodePosition.y, 1f, _path[0].nodePosition.x);
            // remove the first node
            _path.RemoveAt(0);
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
        var t = Task.Run(() => pathFinder.CalculatePath(startPos, endPos, grid));

        while (!t.IsCompleted)
        {
            yield return null;
        }

        if (!t.IsCompletedSuccessfully)
        {
            Debug.Log($"Failed to get path: {t.Exception}");
            throw new Exception("Failed to get path");
        }

        if (t.IsCompleted)
        { 
            _path = t.Result;
            Debug.Log(_path);
            Debug.Log("Task finished");
            yield return null;
        }
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