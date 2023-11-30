using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPointManager : Singleton<SpawnPointManager>
{

    private Transform[] spawnPoints;

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
        spawnPoints = GetComponentsInChildren<Transform>();
    }
}
