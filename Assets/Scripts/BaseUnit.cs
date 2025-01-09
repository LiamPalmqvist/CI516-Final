using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Random = System.Random;

public class BaseUnit : MonoBehaviour
{
    public enum UnitStates
    {
        Idle, 
        Flee,
        Pathfinding,
        Roam,
        Attack,
        Gather,
        Deposit
    }

    public UnitInformation unitInformation;
    
    public int maxHealth = 100;
    public int maxAmmo = 100;
    public float moveSpeed = 10f;
    public UnitStates state = UnitStates.Idle;
    
    [SerializeField] SceneController sceneController; // Controls which units are in what places
    
    [SerializeField] int currentHealth;
    [SerializeField] int currentAmmo;

    
    // ========================================================================================

    public Vector2 currentPos;
    public Vector2 targetPos;
    public Vector3 actualTargetPos;
    public bool moving = false;
    public bool fleeing = false;
    public bool checkingCoords;
    public PathFinder pathFinder;
    private List<Node> Path = new();
    public bool findingPath = false;
    
    // ========================================================================================

    public int currentResources = 0;
    public int maxResources = 100;
    public Resource nearestResource = null;
    public bool gathering = false;
    
    // ========================================================================================
    
    // Entity enemies
    public List<GameObject> enemies = new();
    private bool ableToShoot = true;
    public GameObject projectilePrefab;

    // ========================================================================================

    // TODO: Make unit collision script with Bullets from other units

    // Awake is called when the script is initialised whether it is enabled or not
    // It is called even if the script is disabled
    void Awake()
    {
        // https://docs.unity3d.com/6000.0/Documentation/ScriptReference/GameObject.Find.html
        sceneController = GameObject.Find("SceneController").GetComponent<SceneController>();
        pathFinder = GetComponent<PathFinder>();
        unitInformation = GetComponent<UnitInformation>();
        // Round the positions to place them on the map properly
        // https://www.techiedelight.com/round-float-to-2-decimal-points-csharp/
        // Decimal newX = Decimal.Round((decimal)transform.position.x, 0);
        // Decimal newZ = Decimal.Round((decimal)transform.position.z, 0);
    }
    
    // Called when the unit script is enabled
    void OnEnable() {
        currentHealth = maxHealth;
        targetPos = currentPos;
    }

    // Called when the unit script is disabled
    void OnDisable()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // STATE MACHINE
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
                Attack();
                break;
            case UnitStates.Gather:
                Gather();
                break;
            case UnitStates.Deposit:
                Deposit();
                break;
        }
    }

    // DECISION TREE
    private void ControlStates()
    {
        // if the health is equal to or below zero then kill the unit
        if (currentHealth <= 0)
        {
            unitInformation.Team.DisableUnit(gameObject);
            return;
        }
        
        // if the distance to the spawnCoords is less than the spawn radius and the current health is
        if (Vector2.Distance(currentPos, new Vector2(unitInformation.SpawnCoords.y, unitInformation.SpawnCoords.x)) <= unitInformation.SpawnRadius && currentHealth < maxHealth)
        {
            Heal(1);
            return;
        }
        
        // Set initial state and nearest resource to null
        nearestResource = null;

        // set initial target node to current position if the target position is (0,0)
        if (targetPos == Vector2.zero) targetPos = currentPos;
        
        // We don't want the unit to do anything while it is path finding
        if (findingPath) return;
        
        // if there isn't anything to do, the unit will stay idle
        state = UnitStates.Roam;
        
        // Check if there is a resource nearby,
        // this also sets the gathering variable to true if within the gathering distance
        CheckNearbyResources();
        // Check if there are enemies nearby
        // checkNearbyEnemies();
        
        // Checks if there is a resource nearby
        if (nearestResource && state != UnitStates.Gather && targetPos == currentPos)
        {
            // Checks if the unit is within the resource's gathering range
            // if so: start gathering resources
            // if not: start path find to it
            // BREAK CONDITION: resources is at max capacity OR resources depleted
            state = UnitStates.Gather;
        }

        if (!nearestResource && currentResources > 0 && state != UnitStates.Deposit || currentResources >= maxResources && state != UnitStates.Deposit)
        {
            state = UnitStates.Deposit;
        }

        // Checks if there is an enemy nearby
        // if (nearestEnemy && state != UnitStates.Attack)
        // {
        //     // Checks if the enemy is within attack range
        //     // if so: start fighting the enemy
        //     // if not: path find to the enemy
        //     state = UnitStates.Attack;
        // }
        
        // Check if health < 30%
        if (currentHealth < 3 * (maxHealth / 10) && state != UnitStates.Flee && targetPos == currentPos)
        {
            // Checks if unit is near home.
            // if so: heal
            // if not: path find
            // BREAK CONDITION: Health => 100%
            state = UnitStates.Flee; 
        }

        // Checks if Path.Length > 0
        // if so: follow path
        // BREAK CONDITION: Path.Length =< 0
        if (Path.Count > 0 || targetPos != currentPos)
        {
            // Check if the target position is not the unit's current one
            state = UnitStates.Pathfinding;
        }
    }

    private void CheckNearbyResources()
    {
        float closestResourceDistance = float.MaxValue;
        foreach (Resource resource in sceneController.activeResources.Select(resourceControl => resourceControl.GetComponent<Resource>()))
        {
            // Get the distance to the current resource in the loop
            float resourceDistance = Vector2.Distance(currentPos, resource.currentPosition);
            
            // if it is within the radius that the unit can see
            // and is a lesser distance than the previous ones
            if (resourceDistance < closestResourceDistance && resourceDistance < unitInformation.SeeRadius)
            {
                // make it the closest one in range
                closestResourceDistance = resourceDistance;
                nearestResource = resource;
                
                // if the resource is within the gathering range
                if (resourceDistance < resource.gatherRange)
                {
                    // set gathering to true
                    gathering = true;
                }
            }
        }
        
        if (!nearestResource) gathering = false;
    }

    private void PathFind()
    {
        if (currentPos != targetPos) checkingCoords = false;
        // Debug.Log("Pathfinding started");
        if (targetPos == Vector2.zero) return;
        
        // This returns if the unit is still trying to find a path to targetPos
        if (findingPath) return;
        // Debug.Log("Finding path");
        // if the Path length is greater than 0 this means the unit is following a path
        if (Path.Count > 0)
        {
            // if the unit is not moving, move the unit
            if (!moving)
            {
                sceneController.Grid[(int)currentPos.x, (int)currentPos.y] = sceneController.Grid[(int)Path[0].nodePosition.x, (int)Path[0].nodePosition.y];
                sceneController.Grid[(int)Path[0].nodePosition.x, (int)Path[0].nodePosition.y] = gameObject;
                currentPos = Path[0].nodePosition;
                moving = true;
            }
            // Otherwise get the distance to the next tile
            else
            {
                GetDistance();
            }
        }
        // else, start finding the path
        else
        {
            // Debug.Log("Starting pathfinding");
            StartCoroutine(StartGetPath(currentPos, targetPos, sceneController.Grid));
            Debug.Log($"currentPos: {currentPos}, targetPos: {targetPos}, gridPos: {sceneController.Grid[(int)currentPos.y, (int)currentPos.x]}");
            findingPath = true;
        }
    }
    
    // Random position 1 away from unit in cardinal direcitons
    
    private void Roam()
    {
        List<Vector2> validPoints = new List<Vector2>();
        
        // List of points adjacent to the unit, not including unit's own position
        Vector2[] points =
        {
            new(currentPos.x + 1, currentPos.y),
            new(currentPos.x + 1, currentPos.y + 1),
            new(currentPos.x + 1, currentPos.y - 1),
            new(currentPos.x, currentPos.y + 1),
            new(currentPos.x, currentPos.y - 1),
            new(currentPos.x - 1, currentPos.y),
            new(currentPos.x - 1, currentPos.y),
            new(currentPos.x - 1, currentPos.y + 1),
        };

        foreach (Vector2 point in points)
        {
            if (PathFinder.CheckValidSpace(point, sceneController.Grid))
            {
                validPoints.Add(point);
            }
            /*
            else
            {
                Debug.Log($"{point} is invalid to traverse to for team {team.teamColour}, {gameObject}");
            }
            */
        }
        
        // Choose a random point from the valid points
        try
        {
            targetPos = validPoints[new Random().Next(0, validPoints.Count)];
        }
        catch (Exception)
        {
            
        }

        // Original way the units used to path find randomly. Was limited by 
        /*
        switch (new Random().Next(0, 4))
        {
            case 0:
                targetPos = new Vector2(currentPos.x + 1, currentPos.y);
                // node = new Node(new Vector2(currentPos.x + 1, currentPos.y), new Vector2(currentPos.x + 1, currentPos.y), null);
                // if (sceneController.Grid[(int)currentPos.x + 1, (int)currentPos.y]) break;
                // Path.Add(node);
                // Debug.Log($"Team {team} roaming to node {node.nodePosition} from position {currentPos}");
                break;
            case 1:
                targetPos = new Vector2(currentPos.x, currentPos.y + 1);
                // node = new Node(new Vector2(currentPos.x, currentPos.y + 1), new Vector2(currentPos.x, currentPos.y + 1), null);
                // if (sceneController.Grid[(int)currentPos.x, (int)currentPos.y + 1]) break;
                // Path.Add(node);
                // Debug.Log($"Team {team} roaming to node {node.nodePosition} from position {currentPos}");
                break;
            case 2:
                targetPos = new Vector2(currentPos.x - 1, currentPos.y);
                // node = new Node(new Vector2(currentPos.x - 1, currentPos.y), new Vector2(currentPos.x - 1, currentPos.y), null);
                // if (sceneController.Grid[(int)currentPos.x - 1, (int)currentPos.y]) break;
                // Path.Add(node);
                // Debug.Log($"Team {team} roaming to node {node.nodePosition} from position {currentPos}");
                break;
            case 3:
                targetPos = new Vector2(currentPos.x, currentPos.y - 1);
                // node = new Node(new Vector2(currentPos.x, currentPos.y - 1), new Vector2(currentPos.x, currentPos.y - 1), null);
                // if (sceneController.Grid[(int)currentPos.x, (int)currentPos.y - 1]) break;
                // Path.Add(node);
                // Debug.Log($"Team {team} roaming to node {node.nodePosition} from position {currentPos}");
                break;
        }

        if (SceneInformation.currentMap[(int)targetPos.y][(int)targetPos.x] == 1)
        {
            return;
        }
        Roam();

        // if (PathFinder.CheckValidSpace(targetPos, sceneController.Grid))
        // {
        //     return;
        // }
        // Roam();
        */
    }

    // The function that runs when the unit's state is set to "Flee"
    private void Flee()
    {
        // greater than or equal to the mex health OR the Path is not empty return
        if (Path.Count > 0) return;
        
        Vector2 fleeCoords = PathFinder.FindOpenPosition(currentPos, unitInformation.SpawnCoords, unitInformation.SpawnRadius, true);
        Debug.Log(PathFinder.CheckValidSpace(unitInformation.SpawnCoords + Vector2.down, sceneController.Grid));
        // if the flee coordinates aren't valid
        if (fleeCoords == Vector2.zero)
        {
            // pick a random direction and go in that direction until it hits something
            Roam();
        }
        else
        {
            targetPos = new Vector2(fleeCoords.y, fleeCoords.x);
        }
        
        // Otherwise
        // StartCoroutine(StartGetPath(currentPos, fleeCoords, sceneController.Grid));
        // Debug.Log($"Fleeing to {fleeCoords}");
    }
    
    private void Gather()
    {
        if (gathering)
        {
            if (currentResources < maxResources && nearestResource.resourcesHeld > 0)
            {
                DepleteResources();
                return;
            }

            if (currentResources >= maxResources)
            {
                gathering = false;
            }
        }
        else
        {
            Vector2 coordinate = PathFinder.FindOpenPosition(currentPos, nearestResource.currentPosition, nearestResource.gatherRange);
            if (coordinate == Vector2.zero) return;
            targetPos = coordinate;
        }
    }

    private void Deposit()
    {
        if (Vector2.Distance(currentPos, new Vector2(unitInformation.SpawnCoords.y, unitInformation.SpawnCoords.x)) <= unitInformation.SpawnRadius)
        {
            DepositAtBase();
            return;
        }

        if (targetPos != currentPos)
            checkingCoords = false;

        // if the bot is checking for coordinates
        if (checkingCoords) return;
        
        checkingCoords = true;
        Debug.Log("Not at base");
        Vector2 coordinate = PathFinder.FindOpenPosition(currentPos, unitInformation.SpawnCoords, unitInformation.SpawnRadius, true);
        if (coordinate == Vector2.zero) return;
        targetPos = new Vector2(coordinate.y, coordinate.x);;
    }

    private void DepositAtBase()
    {
        unitInformation.Team.resources++;
        currentResources--;
    }

    private void Attack()
    {
        // if health is low, flee
        if (currentHealth <= maxHealth / 10 * 3) return;
        
        // if unable to shoot, try dodging by moving in a random direction
        if (!ableToShoot) 
        { 
            Roam();
            return;
        }
        
        // Calculate the closest enemy with the lowest health
        var closestEnemy = enemies[0];
        var index = 0;
        for (; index < enemies.Count; index++)
        {
            var enemy = enemies[index];
            if (Vector3.Distance(transform.position, enemy.transform.position) > Vector3.Distance(transform.position, closestEnemy.transform.position) && 
                enemy.GetComponent<BaseUnit>().currentHealth < closestEnemy.GetComponent<BaseUnit>().currentHealth
            )
            {
                closestEnemy = enemy;
            }
        }
    
        ShootProjectile(closestEnemy.GetComponent<BaseUnit>());
        ableToShoot = false;
        StartCoroutine(CountDown(5));
    }

    private void ShootProjectile(BaseUnit enemy)
    {
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        projectile.GetComponent<Bullet>().parent = this;
        projectile.transform.LookAt(enemy.transform.position);
    }
    
    private IEnumerator CountDown(int amount)
    {
        yield return new WaitForSeconds(amount);
        
        ableToShoot = true;
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

    private void DepleteResources()
    {
        currentResources += nearestResource.depletionRate;
        nearestResource.resourcesHeld -= nearestResource.depletionRate;
        // Debug.Log(nearestResource.resourcesHeld);
    }
    
    private void GetDistance()
    {
        // if the current and next positions are within 0.1f of each other
        if (Vector3.Distance(transform.position, new Vector3(Path[0].nodePosition.y, 1f, Path[0].nodePosition.x)) <= 0.1f)
        {
            // Make a new variable
            // set moving to false
            moving = false;
            // make the unit's actual position the new position
            transform.position = new Vector3(Path[0].nodePosition.y, 1f, Path[0].nodePosition.x);
            // remove the first node
            Path.RemoveAt(0);
        }
        else
        {
            transform.LookAt(new Vector3(Path[0].nodePosition.y, 1f, Path[0].nodePosition.x));
            transform.position = Vector3.MoveTowards(
                transform.position,
                new Vector3(currentPos.y, 1f, currentPos.x), 
                moveSpeed * Time.deltaTime
            );
        }
    }

    private IEnumerator StartGetPath(Vector2 startPos, Vector2 endPos, GameObject[,] grid)
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

        while (!t.IsCompleted)
        {
            yield return null;
        }

        if (!t.IsCompletedSuccessfully)
        {
            // Debug.Log($"Failed to get path: {t.Exception}");
            throw new Exception($"Failed to get path from {startPos} to {endPos}. {grid[(int)startPos.x, (int)startPos.y]}");
        }

        if (!t.IsCompleted)
        {
            yield break;
        }
        
        Path = t.Result;
        findingPath = false;
        // Debug.Log($"Nodes in list: {t.Result.Count}");
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

    public void SetMaxHealth(int health)
    {
        maxHealth = health;
        currentHealth = maxHealth;
    }

    private void OnTriggerEnter(Collider collides)
    {
        if (!enabled) return;
    
        // test if the other unit has a component of BaseUnit
        if (!collides.gameObject.TryGetComponent<BaseUnit>(out var unit)) return;
        if (unit.unitInformation.Team == unitInformation.Team) return;
        if (unit.currentHealth <= 0) return;
        enemies.Add(unit.gameObject);
    }

    private void OnTriggerExit(Collider collides)
    {
        if (!enabled) return;
        
        // test if the other unit has a component of BaseUnit
        if (!collides.gameObject.TryGetComponent<BaseUnit>(out var unit)) return;
        if (unit.unitInformation.Team == unitInformation.Team) return;
        if (unit.currentHealth <= 0) return;
        enemies.Remove(unit.gameObject);
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