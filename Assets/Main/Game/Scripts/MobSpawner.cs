using UnityEngine;
using System.Collections.Generic;

public class MobSpawner : MonoBehaviour
{
    [Header("Настройки спавна")]
    public GameObject[] mobPrefabs;
    public Transform[] spawnPoints;

    public float spawnInterval = 5f;
    public int minMobsPerSpawn = 1;
    public int maxMobsPerSpawn = 3;

    [Header("Ограничения (опционально)")]
    public int maxActiveMobs = 10;
    private List<GameObject> spawnedMobs = new List<GameObject>();

    private float timer;

    void Start()
    {
        if (mobPrefabs == null || mobPrefabs.Length == 0)
        {
            Debug.LogError("Не назначены префабы мобов в MobSpawner!");
            enabled = false;
            return;
        }

        if (minMobsPerSpawn > maxMobsPerSpawn)
        {
            Debug.LogWarning("minMobsPerSpawn не может быть больше maxMobsPerSpawn. Устанавливаю maxMobsPerSpawn = minMobsPerSpawn.");
            maxMobsPerSpawn = minMobsPerSpawn;
        }

        timer = spawnInterval;
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            spawnedMobs.RemoveAll(item => item == null);

            if (spawnedMobs.Count < maxActiveMobs)
            {
                SpawnWave();
            }
            timer = spawnInterval;
        }
    }

    void SpawnWave()
    {
        int mobsToSpawn = Random.Range(minMobsPerSpawn, maxMobsPerSpawn + 1);

        for (int i = 0; i < mobsToSpawn; i++)
        {
            if (spawnedMobs.Count >= maxActiveMobs)
            {
                break;
            }

            SpawnSingleMob();
        }
    }

    void SpawnSingleMob()
    {
        GameObject mobToSpawn = mobPrefabs[Random.Range(0, mobPrefabs.Length)];

        Transform spawnPoint;
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        }
        else
        {
            spawnPoint = transform;
        }

        GameObject spawnedMob = Instantiate(mobToSpawn, spawnPoint.position, spawnPoint.rotation);
        spawnedMobs.Add(spawnedMob);
    }
}