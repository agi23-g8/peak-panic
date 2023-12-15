using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPointManager : Singleton<SpawnPointManager>
{

    private List<Transform> spawnPoints;

    private List<Transform> usedSpawnPoints = new List<Transform>();

    public Transform GetSpawnPoint()
    {
        // return the first spawn point, that has not been used yet
        foreach (Transform spawnPoint in spawnPoints)
        {
            if (!usedSpawnPoints.Contains(spawnPoint))
            {
                usedSpawnPoints.Add(spawnPoint);
                return spawnPoint;
            }
        }

        // if all spawn points have been used, clear the list and return the first spawn point
        ResetSpawnPoints();
        return spawnPoints[0];
    }

    public void ResetSpawnPoints()
    {
        usedSpawnPoints.Clear();
    }


    void Awake()
    {
        // all childrens transforms are spawn points
        spawnPoints = new List<Transform>(GetComponentsInChildren<Transform>());
        spawnPoints.RemoveAt(0);
    }

    void OnDrawGizmos()
    {
        // draw a sphere at each spawn point
        Gizmos.color = Color.red;
        spawnPoints = new List<Transform>(GetComponentsInChildren<Transform>());
        spawnPoints.RemoveAt(0);
        foreach (Transform spawnPoint in spawnPoints)
        {
            Gizmos.DrawSphere(spawnPoint.position, 0.5f);
        }
    }

}
