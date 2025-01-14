using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class <c>TeamClass</c> models a Team in the game
/// </summary>
/// <param name="teamName">The name of the team</param>
/// <param name="teamColour">The colour of the team</param>
/// <param name="maxUnitCount">The MAX amount of units the team can spawn</param>
/// <param name="spawnCoords">Where the team will initially spawn</param>
/// <param name="spawnRadius">The distance from the team base that units will spawn</param>
/// <param name="unitPrefab">The prefab for the units the team will control</param>
/// <param name="buildingPrefabs">The prefabs for the buildings the team will be able to spawn</param>
public class TeamClass : MonoBehaviour
{
    public string teamName;
    public Color teamColour;
    public int teamNumber;
    public int maxUnitCount;      
    public Vector2 spawnCoords;         
    public int spawnRadius;             
    public GameObject unitPrefab;       
    public GameObject[] buildingPrefabs;
    public bool ableToSpawn;
    public int resources = 100;
    public int teamHealth = 100;
    public bool teamActive = true;
    
    [SerializeField] SceneController sceneController;            // Controls which units are in what places 
    [SerializeField] private List<GameObject> inactiveUnits;     // All units will be initially assigned to this list until activated
    [SerializeField] public List<GameObject> activeUnits;       // All ACTIVE units will be put in this list
    [SerializeField] private List<GameObject> activeBuildings;   // All ACTIVE buildings will be put in this list

    /**
     * The cost, type, health, etc., of the units will be controlled via a Unit script
     * This will also be the case for any buildings
     */
    
    // This is called when the class is instantiated
    private void Start()
    {
        // Grab the sceneController
        sceneController = GameObject.Find("SceneController").GetComponent<SceneController>();

        // Instantiate the new lists to length of maxUnits
        inactiveUnits = new List<GameObject>(maxUnitCount);
        activeUnits = new List<GameObject>(maxUnitCount);
        activeBuildings = new List<GameObject>(maxUnitCount);
        
        // Set the colour of the team and flag
        GetComponent<MeshRenderer>().material.color = teamColour;
        GetChildGameObject(gameObject, "TeamFlag").GetComponent<MeshRenderer>().material.color = teamColour;
        transform.position = new Vector3(spawnCoords.x, 0.5f, spawnCoords.y);
        
        // Instantiate all units and place in _inactiveUnits
        for (int i = 0; i < maxUnitCount; i++)
        {
            GameObject unit = Instantiate(unitPrefab, new Vector3(0 + i, 1, -1 - teamNumber), Quaternion.identity);
            unit.GetComponent<Transform>();
            // ^ .SetParent(transform, true); // from https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Transform.SetParent.html
            unit.GetComponent<MeshRenderer>().material.color = teamColour;
            unit.GetComponent<UnitInformation>().SetInformation(this);
            inactiveUnits.Add(unit);
        }
        
        // Debug.Log($"Team Number: {teamNumber}, inactiveUnits: {inactiveUnits.Count}");

        // SpawnUnit();
        // StartCoroutine(CountDown(5));

        teamHealth = 100;
        teamActive = true;
        
        //sceneController.PrintUnitPositions();
        
        ableToSpawn = true;
    }

    private void FixedUpdate()
    {
        // Check if the team has any available units left
        if (!teamActive) return;

        if (activeUnits.Count <= 0 && resources < 100 || teamHealth <= 0)
        {
            // Debug.Log($"Team {teamColour} is inactive. Team {teamColour} Health: {teamHealth}, Units Remaining: {activeUnits.Count}, Resources: {resources}");
            sceneController.inactiveTeams.Add(gameObject);
            sceneController.activeTeams.Remove(gameObject);
            teamActive = false;
        }
        
        if (!ableToSpawn || resources < 100) return;

        ableToSpawn = false;
        resources -= 100;
        SpawnUnit();
        StartCoroutine(CountDown(5));
    }
    
    // from https://discussions.unity.com/t/how-to-find-a-child-gameobject-by-name/31255/2
    private static GameObject GetChildGameObject(GameObject fromGameObject, string withName) {
        //Author: Isaac Dart, June-13.
        Transform[] ts = fromGameObject.transform.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in ts) if (t.gameObject.name == withName) return t.gameObject;
        return null;
    }

    public void DisableUnit(GameObject unit)
    {
        // IF THE UNIT DOESN'T EXIST
        // Throw an error
        if (!unit)
        {
            throw new ArgumentNullException("unit");
        }
        
        // if the unit is in the "activeUnits" array
        if (activeUnits.Contains(unit))
        {
            // Remove it from that array,
            // add it to the inactiveUnits array
            // and set it to be inactive in Unity
            activeUnits.Remove(unit);
            inactiveUnits.Add(unit);
            unit.gameObject.transform.position = new Vector3(inactiveUnits.IndexOf(unit)-10, 1, -teamNumber);
            unit.GetComponent<BaseUnit>().enabled = false;
        }
    }

    /// <summary>
    /// This will spawn a new unit if the inactive unit count is greater than 0
    /// </summary>
    /// <param name="unit"></param>
    private void SpawnUnit()
    {
        if (inactiveUnits.Count > 0)
        {
            GameObject unit = inactiveUnits[0];
            BaseUnit unitScript = unit.GetComponent<BaseUnit>();
            unitScript.enabled = true; // This will set the default values for this unit
            
            // Debug.Log($"Spawn Coords: {spawnCoords}");
            try
            {
                Vector3 position = sceneController.FindOpenPosition(0, spawnCoords, spawnRadius, unit);
                unit.transform.position = new Vector3(position.x, 0, position.z);
                // Debug.Log($"Position {position}");
                unitScript.currentPos = new Vector2(unit.transform.position.z, unit.transform.position.x);
                unitScript.SetMaxHealth(100);
                activeUnits.Add(unit);
                inactiveUnits.Remove(unit);
            }
            catch (IndexOutOfRangeException e)
            {
                Debug.LogError(e);
            } 
            // Debug.Log(unit.transform.position);
        }
    }

    private IEnumerator CountDown(int amount)
    {
        yield return new WaitForSeconds(amount);
        
        ableToSpawn = true;
    }

    /// <summary>
    /// This will spawn a new building in the positions indicated by the mouse click
    /// </summary>
    /// <param name="buildingPrefab">The prefab that will be placed and assigned</param>
    /// <param name="coords">The coordinates at which the building will be placed</param>
    /// <exception cref="NotImplementedException">To Be Implemented</exception>
    public void SpawnBuilding(GameObject buildingPrefab, Vector2 coords)
    {
        throw new NotImplementedException();
    }
}