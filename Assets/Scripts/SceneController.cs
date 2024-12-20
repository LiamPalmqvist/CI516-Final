using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.UIElements;
using UnityEngine;
using Utils;
using Random = System.Random;

public class SceneController : MonoBehaviour
{
    // Variables
    public GameObject[,] Grid = new GameObject[100, 100]; 
    public int[,] gridInt = new int[100, 100];
    // switch this to a GameObject[,]
    // Have empty fields be empty node Objects - blocks be Obstacle GameObjects
    // Maybe try to generate a mesh based on where empty spots are?
    
    // Amount of teams to spawn
    // The Team class will have individual parameters
    // that can be changed
    [Header("Teams Data")]
    public GameObject teamPrefab;
    public List<GameObject> activeTeams = new List<GameObject>();
    public Color[] teamColors = { Color.red, Color.blue, Color.green, Color.yellow };
    public string[] teamColourNames = { "red", "blue", "green", "yellow" };
    
    [Header("Customisable Data")]
    public Vector2[] teamPositions =
        { new Vector2(25, 25), new Vector2(25, 75), new Vector2(75, 25), new Vector2(75, 75) };

    [Header("Obstacles")]
    public GameObject obstaclePrefab;
    public int obstacleCount = 0;
    public List<GameObject> obstacles = new List<GameObject>();
    public GameObject emptyObstacle;
    
    // Map elements from Maps.cs
    Maps maps;
    
    [Header("Selection tools")]
    private Camera playerCamera;
    public GameObject spinnerPrefab;
    public GameObject activeSpinnerPrefab;
    private GameObject spinner;
    private GameObject activeSpinner;
    private SpinnerControl spinnerControl;
    private SpinnerControl activeSpinnerControl;

    // Start is called before the first frame update
    void Start()
    {
        // Set the player's camera so that rayCasts work
        playerCamera = Camera.main;
        
        // Import maps from txt files
        // Map files will be x amount of characters long with defined width and height
        // of 100
        maps = GetComponent<Maps>();

        //SpawnObstacles();
        //SpawnTeams();
        //int[][] map1 = GenerateMap();
        // SpawnFromMap(maps.map2);
        SpawnFromMap(maps.map2);
    }

    void Update()
    {
        GetPlayerMousePosition();
    }

    // This takes an array of arrays of integers and creates a map
    // using the "width" and "height" of the arrays to get the dimensions
    void SpawnFromMap(int[][] mapArray)
    {
        // setup vars
        int height = mapArray.Length;
        int width = mapArray[0].Length;
        int teamNumber = 0;
        
        // Make the grid the new width and height of the map
        Grid = new GameObject[mapArray.Length, mapArray[0].Length];
        // Find the gameBoard and make it the width and height
        // as well as setting the position to the middle of
        // the width and height
        Transform gameBoard = GameObject.Find("GameBoard").gameObject.transform; 
        gameBoard.localScale = new Vector3(width/10, 1, height/10);
        gameBoard.position = new Vector3(width/2, 0, height/2);
        
        // IN THIS INSTANCE, THE ARRAY IS {Y, X} with each one being {Y++ GOING UP, X++ GOING RIGHT}
        
        // iterate through the array's Y AXIS
        for (int y = 0; y < mapArray.Length; y++)
        {
            // iterate through the array's X AXIS
            for (int x = 0; x < mapArray[y].Length; x++)
            {
                switch (mapArray[y][x])
                {
                    case 0:
                        GameObject obstacle = Instantiate(obstaclePrefab);
                        obstacle.name = "Obstacle";
                        obstacle.transform.position = new Vector3(x, 1f, y);
                        obstacles.Add(obstacle);
                        Grid[y, x] = obstacle;
                        gridInt[y, x] = 0;
                        break;
                    // case 1:
                    //     Grid[y, x] = Instantiate(emptyObstacle);
                    //     Grid[y, x].name = "EmptyNode";
                    //     Grid[y, x].transform.position = new Vector3(x, 1f, y);
                    //     gridInt[y, x] = 1;
                    //     break;
                    case 2:
                        GameObject team = Instantiate(teamPrefab);
                        team.name = $"{teamColourNames[teamNumber]}";
                        TeamClass teamClass = team.GetComponent<TeamClass>();
                        teamClass.spawnCoords = new Vector2(x, y);
                        teamClass.teamColour = teamColors[teamNumber];
                        teamClass.teamNumber = teamNumber;
                        teamClass.teamName = team.name;
                        
                        activeTeams.Add(team);
                        Grid[y, x] = team;
                        gridInt[y, x] = 2;
                        //team.gameObject.SetActive(false);
                        //Instantiate(team);
                        //Debug.Log($"{teamColourNames[teamNumber]} position is at {x}, {y}");
                        teamNumber++;
                        break;
                }
                
                
                //Grid[y, x] = mapArray[y][x];
            }
        }
        
        spinner = Instantiate(spinnerPrefab, Vector3.zero, Quaternion.identity);
        activeSpinner = Instantiate(activeSpinnerPrefab, Vector3.zero, Quaternion.identity);
        spinnerControl = spinner.GetComponent<SpinnerControl>();
        activeSpinnerControl = activeSpinner.GetComponent<SpinnerControl>();
        activeSpinner.SetActive(false);

        /*
        Debug.Log(activeTeams.Count);
        for (int i = 0; i < activeTeams.Count; i++ )
        {
            TeamClass teamClass = activeTeams[i].GetComponent<TeamClass>();
            for (int j = 0; j < teamClass.maxUnitCount; j++)
            {
                teamClass.SpawnUnit();
                Debug.Log($"Spawning {i}'s team {j}");
            }
        }
        */
    }

    // Generates a random position within a radius of a set of spawn coordinates
    // if the position is NOT filled, it will return a value. If the position IS filled,
    // the algorithm will keep generating random positions either until one is found or
    // for as many times as the spawnRadius squared
    public Vector3 FindOpenPosition(int iteration, Vector2 spawnCoords, int spawnRadius, GameObject insertedObject)
    {
        // Debug to check that an open position can be found within the spawn radius
        // This keeps going until the algorithm has checked spawnRadius^2 times
        if (iteration == spawnRadius * spawnRadius)
        {
            return new Vector2(-100, -100);
        }

        // Make a random position within the radius of the spawnCoords + spawnRadius
        // Place unit on the radius specified
        // spawnCoords + Random.insideUnitCircle will place the unit inside a radius of 1
        // at the spawnCoords and * spawnRadius will make that circle bigger
        Vector2 randomPos = spawnCoords + UnityEngine.Random.insideUnitCircle * spawnRadius;
        int newX = Math.Abs((int)Decimal.Round((decimal)randomPos.x, 0));
        int newZ = Math.Abs((int)Decimal.Round((decimal)randomPos.y, 0));
        Vector3 newPos = new Vector3(newX, 1f, newZ);

        // Check if position is empty
        if (Grid[newZ, newX] == null)
        {
            Grid[newZ, newX] = insertedObject;
            Debug.Log($"Set {newZ}, {newX} to {insertedObject.name}");
        }
        // Otherwise reiterate
        else
        {
            Debug.Log($"Position {newPos} full, iteration {iteration}");
            newPos = FindOpenPosition(iteration + 1, spawnCoords, spawnRadius, insertedObject);
        }

        // Return found position
        return newPos;
    }

    // public void PrintUnitPositions()
    // {
    //     for (int i = 0; i < 100; i++)
    //     {
    //         for (int j = 0; j < 100; j++)
    //         {
    //             if (Grid[i, j].name == "EntityPrefab(Clone)")
    //             {
    //                 Debug.Log($"Found {Grid[i, j].name} at position {i}, {j}");
    //             }
    //         }
    //     }
    // }

    private void GetPlayerMousePosition()
    {
       
        // Create the raycast from the camera to the mouse's position in the world
        Ray rayCamToMousePosition = playerCamera.ScreenPointToRay(Input.mousePosition);

        // cast ray 100m and store data in rayHit
        if (Physics.Raycast(rayCamToMousePosition, out RaycastHit rayHit, 100F))
        {
            Debug.DrawRay(playerCamera.transform.position, rayHit.point);
            Vector3 hitPoint = new Vector3(Math.Abs((int)rayHit.point.x), 1, Math.Abs((int)rayHit.point.z));
            spinner.transform.position = hitPoint;

            if (!spinnerControl.IsSpinnerSet())
            {
                // if the left mouse button is pressed
                if (Input.GetMouseButtonDown(0))
                {
                    if (Grid[(int)hitPoint.z, (int)hitPoint.x].name == "EntityPrefab(Clone)")
                    {
                        GameObject foundObject = Grid[(int)hitPoint.z, (int)hitPoint.x];
                        // Debug.Log($"Found {foundObject.name} at position {hitPoint.x}, {hitPoint.z}");
                        activeSpinner.SetActive(true);
                        activeSpinnerControl.SetSpinner(true);
                        spinnerControl.SetSpinner(true);
                        activeSpinnerControl.assignedObject = foundObject;
                    }
                }
            }
            else
            {
                if (Input.GetButtonDown("Cancel"))
                {
                    spinnerControl.SetSpinner(false);
                    activeSpinnerControl.SetSpinner(false);
                    activeSpinner.SetActive(false);
                } else if (Input.GetMouseButtonDown(0))
                {
                    spinnerControl.SetSpinner(false);
                    spinnerControl.lastClicked = new Vector2((int)hitPoint.z, (int)hitPoint.x);
                    GameObject assignedObject = activeSpinnerControl.assignedObject;
                    BaseUnit unit = assignedObject.GetComponent<BaseUnit>();
                    activeSpinnerControl.SetSpinner(false);
                    activeSpinner.SetActive(false);
                    // unit.AStar(unit.currentPos, new Vector2((int)hitPoint.z, (int)hitPoint.x), Grid);
                    //StartCoroutine(unit.StartGetPath(unit.currentPos, new Vector2((int)hitPoint.z, (int)hitPoint.x), gridInt));
                    unit._path2 = unit.pathFinder.CalculatePath(unit.currentPos, new Vector2((int)hitPoint.z, (int)hitPoint.x));
                    // unit.StartAStar(unit.currentPos, new Vector2((int)hitPoint.z, (int)hitPoint.x), Grid);
                }
            }
        }
    }

    // private int[][] GenerateMap()
    // {
    //     Random random = new Random();
    //     int[][] map = new int[100][];
    //     for (int i = 0; i < 100; i++)
    //     {
    //         for (int j = 0; j < 100; j++)
    //         {
    //             map[i][j] = random.Next(0, 1);
    //         }
    //     }
    //
    //     for (int i = 0; i < map.Length; i++)
    //     {
    //         for (int j = 0; j < map[i].Length; j++)
    //         {
    //             Debug.Log(map[i][j]);
    //         }
    //     }
    //
    //     return map;
    // }

    /*
    public void SpawnObstacles()
    {
        for (int i = 0; i < obstacleCount; i++)
        {
            GameObject obstacle = Instantiate(obstaclePrefab);

            int randomX = UnityEngine.Random.Range(0, 100);
            int randomY = UnityEngine.Random.Range(0, 100);

            obstacle.transform.position = FindOpenPosition(0, new Vector2(randomX, randomY), 0, 1);
        }
    }


    public void SpawnTeams()
    {
        for (int i = 0; i < teams.Length; i++)
        {
            GameObject team = teams[i];
            team.GetComponent<TeamClass>().spawnCoords = teamPositions[i];
            team.GetComponent<TeamClass>().teamNumber = i;
            team.GetComponent<TeamClass>().teamColour = teamColors[i];
            team.gameObject.tag = $"{i}";
            Instantiate(team);
        }
    }
    
    //https://stackoverflow.com/questions/3514740/how-to-split-an-array-into-a-group-of-n-elements-each
    public int[][] SplitArray(int[] array, int length)
    {
       int i = 0;
       var query = from s in array
           let num = i++
           group s by num / length
           into g
           select g.ToArray();
       return query.ToArray();
    }
    */
}
