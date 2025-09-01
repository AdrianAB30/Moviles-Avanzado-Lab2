using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class SpawnerManager : NetworkBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject buffPrefab;
    [SerializeField] private GameObject enemyPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private Vector3[] buffSpawnPoints; 
    [SerializeField] private float buffSpawnInterval = 10f;

    [SerializeField] private Vector3 enemySpawnArea = new Vector3(20, 0, 20); 
    [SerializeField] private float enemySpawnInterval = 5f;

    public override void OnNetworkSpawn()
    {
        if (IsServer) 
        {
            StartCoroutine(SpawnBuffsCoroutine());
            StartCoroutine(SpawnEnemiesCoroutine());
        }
    }

    private IEnumerator SpawnBuffsCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(buffSpawnInterval);

            Vector3 spawnPos = buffSpawnPoints[Random.Range(0, buffSpawnPoints.Length)];

            GameObject buff = Instantiate(buffPrefab, spawnPos, Quaternion.identity);
            buff.GetComponent<NetworkObject>().Spawn();
        }
    }

    private IEnumerator SpawnEnemiesCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(enemySpawnInterval);

            Vector3 randomPos = new Vector3(
                Random.Range(-enemySpawnArea.x, enemySpawnArea.x),
                enemySpawnArea.y,
                Random.Range(-enemySpawnArea.z, enemySpawnArea.z)
            );

            GameObject enemy = Instantiate(enemyPrefab, randomPos, Quaternion.identity);
            enemy.GetComponent<NetworkObject>().Spawn();
        }
    }
}
