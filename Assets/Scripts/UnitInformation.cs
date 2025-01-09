using UnityEngine;

public class UnitInformation : MonoBehaviour
{
    [SerializeField] internal TeamClass Team;
    [SerializeField] internal int TeamNumber;
    [SerializeField] internal Vector2 SpawnCoords;
    [SerializeField] internal int SpawnRadius;
    [SerializeField] internal int SeeRadius = 10;

    public void SetInformation(TeamClass team)
    {
        Team = team;
        TeamNumber = team.teamNumber;
        SpawnCoords = team.spawnCoords;
        SpawnRadius = team.spawnRadius;
    }
}