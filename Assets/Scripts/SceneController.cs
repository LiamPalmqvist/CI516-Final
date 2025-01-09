using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using static Resource;
using Random = System.Random;

public class SceneController : MonoBehaviour
{
    // Variables
    public GameObject[,] Grid = new GameObject[100, 100];
    public bool gameOver;
    //public bool playersWin;
    // switch this to a GameObject[,]
    // Have empty fields be empty node Objects - blocks be Obstacle GameObjects
    // Maybe try to generate a mesh based on where empty spots are?
    
    // Amount of teams to spawn
    // The Team class will have individual parameters
    // that can be changed
    [Header("Teams Data")]
    public GameObject teamPrefab;
    public List<GameObject> activeTeams = new();
    public List<GameObject> inactiveTeams = new();
    public Color[] teamColors = { Color.red, Color.blue, Color.green, Color.yellow };
    public string[] teamColourNames = { "red", "blue", "green", "yellow" };
    public TeamClass playerTeam;
    public GameObject playerUnitPrefab;
    
    [Header("Customisable Data")]
    public Vector2[] teamPositions =
    {
        new Vector2(25, 25), 
        new Vector2(25, 75),
        new Vector2(75, 25), 
        new Vector2(75, 75)
    };

    [Header("Obstacles")]
    public GameObject obstaclePrefab;
    // public int obstacleCount = 0;
    public List<GameObject> obstacles = new();

    [Header("Resources")] 
    public int maxResources;
    public int resourcesSpawned = 0;
    public GameObject resourcePrefab;
    public List<GameObject> activeResources = new();
    public List<GameObject> inactiveResources = new();
    public bool canSpawnResource = true;
    
    [Header("Selection tools")]
    private Camera playerCamera;
    public GameObject spinnerPrefab;
    public GameObject activeSpinnerPrefab;
    private GameObject spinner;
    private GameObject activeSpinner;
    private SpinnerControl spinnerControl;
    private SpinnerControl activeSpinnerControl;
    private List<GameObject> selectedUnits = new();
    
    // UI
    [FormerlySerializedAs("endPanel")] public Canvas endCanvas;
    public TMP_Text endText;
    


    // Start is called before the first frame update
    void Start()
    {
        // Set the player's camera so that rayCasts work
        playerCamera = Camera.main;

        // Import maps from txt files
        // Map files will be x amount of characters long with defined width and height
        // of 100

        // int[][] map1 = GenerateMap();
        if (SceneInformation.currentMap == null)
        {
            SpawnFromMap(Maps.map2);
            SceneInformation.currentMap = Maps.map2;
        }
        else
        {
            SpawnFromMap(SceneInformation.currentMap);
        }
    }

    void FixedUpdate()
    {
        if (gameOver) return;

        if (inactiveTeams.Count >= 3 || inactiveTeams.Contains(playerTeam.gameObject))
        {
            //Debug.Log($"{activeTeams[0].name} is the winner");
            
            if (inactiveTeams.Contains(playerTeam.gameObject))
                ShowLoseScreen();
            else
                ShowWinScreen();
            
            gameOver = true;
            return;
        }
        
        
        GetPlayerMousePosition();
        
        if (!canSpawnResource) return;
        
        canSpawnResource = false;
        SpawnResource();
        StartCoroutine(CountDown(5));

    }

    // This takes an array of arrays of integers and creates a map
    // using the "width" and "height" of the arrays to get the dimensions
    private void SpawnFromMap(int[][] mapArray)
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
                        break;
                    
                    case 2:
                        GameObject team = Instantiate(teamPrefab);
                        team.name = $"{teamColourNames[teamNumber]}";
                        TeamClass teamClass = team.GetComponent<TeamClass>();
                        teamClass.spawnCoords = new Vector2(x, y);
                        teamClass.teamColour = teamColors[teamNumber];
                        teamClass.teamNumber = teamNumber;
                        teamClass.teamName = team.name;
                        if (teamNumber == 0)
                        {
                            teamClass.unitPrefab = playerUnitPrefab;
                            playerTeam = team.GetComponent<TeamClass>();
                        }
                        
                        activeTeams.Add(team);
                        Grid[y, x] = team;
                        //team.gameObject.SetActive(false);
                        //Instantiate(team);
                        //Debug.Log($"{teamColourNames[teamNumber]} position is at {x}, {y}");
                        teamNumber++;
                        break;
                    
                    case 3:
                        GameObject resource = Instantiate(resourcePrefab);
                        activeResources.Add(resource);
                        resource.GetComponent<Resource>().SpawnResource(resourceTypes[new Random().Next(resourceTypes.Count-1)], 100, new Vector2(y, x));
                        resource.transform.position = new Vector3(x, 1f, y);
                        resourcesSpawned++;
                        break;
                }
            }
        }
        
        for (int i = resourcesSpawned; i < maxResources; i++)
        {
            GameObject resource = Instantiate(resourcePrefab, new Vector3(0 + i, 1, -1 - 4), Quaternion.identity);
            inactiveResources.Add(resource);
        }
        
        spinner = Instantiate(spinnerPrefab, Vector3.zero, Quaternion.identity);
        activeSpinner = Instantiate(activeSpinnerPrefab, Vector3.zero, Quaternion.identity);
        spinnerControl = spinner.GetComponent<SpinnerControl>();
        activeSpinnerControl = activeSpinner.GetComponent<SpinnerControl>();
        activeSpinner.SetActive(false);
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
        if (!Grid[newZ, newX])
        {
            Grid[newZ, newX] = insertedObject;
            // Debug.Log($"Set {newZ}, {newX} to {insertedObject.name}");
        }
        // Otherwise reiterate
        else
        {
            // Debug.Log($"Position {newPos} full, iteration {iteration}");
            newPos = FindOpenPosition(iteration + 1, spawnCoords, spawnRadius, insertedObject);
        }

        // Return found position
        return newPos;
    }

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

            // Set the start position of drawing the selector
            // and set active

            /*
             * TODO: Show a rectangle of size start.xy, end.xy
             * TODO: Allow for formations by getting the size of the array - if more than 1, use array formation
             */

            // if the amount of selected units is less than or equal to 0, let the user select them
            if (selectedUnits.Count <= 0)
            {
                
                if (Input.GetMouseButtonDown(0)) // 1
                {
                    foreach (GameObject unit in selectedUnits)
                    {
                        unit.transform.GetChild(0).gameObject.SetActive(false);
                    }
                    activeSpinner.SetActive(true);
                    activeSpinner.transform.position = rayHit.point;
                    selectedUnits = new List<GameObject>();
                } 
                else if (Input.GetMouseButton(0)) // 1
                {
                    // Debug.Log("Mouse button held");
                    
                    // Create variables for vectors which translate the positions to 2D Vectors
                    Vector2 startPos = new Vector2(spinner.transform.position.z, spinner.transform.position.x);
                    Vector2 endPos = new Vector2(activeSpinner.transform.position.z, activeSpinner.transform.position.x);
                    
                    // Get the selected units from the static GetUnitsInArea function in PathFinder
                    selectedUnits = PathFinder.GetUnitsInArea(new Vector2Int((int)startPos.x, (int)startPos.y), new Vector2Int((int)endPos.x, (int)endPos.y), playerTeam.activeUnits);
                    
                    // Debug.Log(selectedUnits.Count);
                    foreach (GameObject unit in selectedUnits)
                    {
                        unit.transform.GetChild(0).gameObject.SetActive(true);
                    }
                }
                else if (Input.GetMouseButtonUp(0)) // 1
                {
                    activeSpinner.SetActive(false);
                    // foreach (GameObject unit in selectedUnits)
                    // {
                    //     unit.transform.GetChild(0).gameObject.SetActive(false);
                    // }
                }
            }

            // if the amount of selected units is more than 0, allow the user to position them or get rid of them
            if (Input.GetMouseButtonDown(0))
            {
                // if the left mouse button is pressed
                if (!PathFinder.CheckValidSpace(new Vector2((int)hitPoint.z, (int)hitPoint.x), Grid)) return;
                // if the space the player clicked is not valid, return
                if (selectedUnits.Count < 1)
                {
                    SetUnitTraversalPositions(selectedUnits, new Vector2((int)hitPoint.z, (int)hitPoint.x));
                }
                else
                {
                    selectedUnits[0].GetComponent<BaseUnit>().targetPos = new Vector2((int)hitPoint.z, (int)hitPoint.x);
                }
                selectedUnits = ClearUnits(selectedUnits);
            }
            // if mouse 1 is pressed, clear the array of selected units
            else if (Input.GetMouseButtonDown(1))
            {
                selectedUnits = ClearUnits(selectedUnits);
            }
        }
    }

    private List<GameObject> ClearUnits(List<GameObject> units)
    {
        foreach (var baseUnit in units.Select(unit => unit.GetComponent<BaseUnit>()))
        {
            baseUnit.transform.GetChild(0).gameObject.SetActive(false);
        }
        
        activeSpinner.SetActive(false);

        return new List<GameObject>();
    }

    private void SetUnitTraversalPositions(List<GameObject> units, Vector2 targetPosition)
    {
        switch (units.Count)
        {
            case 2:
                units[0].GetComponent<BaseUnit>().targetPos = targetPosition;
                units[1].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x + 1, targetPosition.y);
                break;
            case 3:
                units[0].GetComponent<BaseUnit>().targetPos = targetPosition;
                units[1].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x + 1, targetPosition.y - 1);
                units[2].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x - 1, targetPosition.y - 1);
                break;
            case 4:
                units[0].GetComponent<BaseUnit>().targetPos = targetPosition;
                units[1].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x + 1, targetPosition.y);
                units[2].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x, targetPosition.y - 1);
                units[3].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x + 1, targetPosition.y - 1);
                break;
            case 5:
                units[0].GetComponent<BaseUnit>().targetPos = targetPosition;
                units[1].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x + 1, targetPosition.y - 1);
                units[2].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x - 1, targetPosition.y - 1);
                units[3].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x + 1, targetPosition.y + 1);
                units[4].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x - 1, targetPosition.y + 1);
                break;
            case 6:
                units[0].GetComponent<BaseUnit>().targetPos = targetPosition;
                units[1].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x + 1, targetPosition.y - 1);
                units[2].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x - 1, targetPosition.y - 1);
                units[0].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x, targetPosition.y + 1);
                units[1].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x + 1, targetPosition.y + 1);
                units[2].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x - 1, targetPosition.y + 1);
                break;
            case 7:
                units[0].GetComponent<BaseUnit>().targetPos = targetPosition;
                units[1].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x + 1, targetPosition.y);
                units[2].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x - 1, targetPosition.y);
                units[3].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x + 1, targetPosition.y - 1);
                units[4].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x - 1, targetPosition.y - 1);
                units[5].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x + 1, targetPosition.y - 2);
                units[6].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x - 1, targetPosition.y - 2);
                break;
            case 8:
                units[0].GetComponent<BaseUnit>().targetPos = targetPosition;
                units[1].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x + 1, targetPosition.y);
                units[2].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x - 1, targetPosition.y);
                units[3].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x + 1, targetPosition.y - 1);
                units[4].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x, targetPosition.y - 1);
                units[5].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x - 1, targetPosition.y - 1);
                units[6].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x + 1, targetPosition.y - 2);
                units[7].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x - 1, targetPosition.y - 2);
                break;
            case 9:
                units[0].GetComponent<BaseUnit>().targetPos = targetPosition;
                units[1].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x + 1, targetPosition.y);
                units[2].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x - 1, targetPosition.y);
                units[3].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x + 1, targetPosition.y - 1);
                units[4].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x, targetPosition.y - 1);
                units[5].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x - 1, targetPosition.y - 1);
                units[6].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x + 1, targetPosition.y - 2);
                units[7].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x - 1, targetPosition.y - 2);
                units[8].GetComponent<BaseUnit>().targetPos = new Vector2(targetPosition.x, targetPosition.y - 2);
                break;
            default:
                break;
        }
    }

    private void SpawnResource()
    {
        Vector2 randomPos = new Vector2(new Random().Next(0, 99), new Random().Next(0, 99));
        if (PathFinder.CheckValidSpace(randomPos, Grid))
        {
            GameObject resource = inactiveResources.Last();
            resource.GetComponent<Resource>().SpawnResource(resourceTypes[new Random().Next(0, resourceTypes.Count-2)], 100, randomPos);
            activeResources.Add(resource);
            inactiveResources.Remove(resource);
            Grid[(int)randomPos.y, (int)randomPos.x] = resource;
        }

        // Has a chance to spawn, if not, oh well
        canSpawnResource = false;
    }

    private IEnumerator CountDown(int seconds)
    {
        yield return new WaitForSeconds(seconds);

        canSpawnResource = true;
    }

    // Not in use
    // private void DisplayText(int[][] map)
    // {
    //     _text.text = "";
    //     for (int i = map.Length-1; i >= 0; i--)
    //     {
    //         for (int j = 0; j < map[i].Length; j++)
    //         {
    //             _text.text += map[i][j] + " ";
    //         }
    //         _text.text += "\n";
    //     }
    // }

    private void ShowWinScreen()
    {
        endCanvas.enabled = true;
        GameObject endPanel = endCanvas.transform.GetChild(0).gameObject;
        endPanel.GetComponent<Image>().color = Color.green;
        endText.text = "You win!";
    }

    private void ShowLoseScreen()
    {
        endCanvas.enabled = true;
        GameObject endPanel = endCanvas.transform.GetChild(0).gameObject;
        endPanel.GetComponent<Image>().color = Color.red;
        endText.text = "You lose!";
    }


    
    // Not in use at the moment, used for generating random maps
    private int[][] GenerateMap()
    {
        Random random = new Random();
        int[][] map = new int[100][];
        for (int i = 0; i < 100; i++)
        {
            int[] row = new int[100];
            for (int j = 0; j < 100; j++)
            {
                row[j] = random.Next(0, 2);
            }
            map[i] = row;
        }
        
        map[40][40] = 2;
        
        return map;
    }
}
