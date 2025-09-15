using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class SpawnerManager : NetworkBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject buffPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private Vector3[] buffSpawnPoints; 
    [SerializeField] private float buffSpawnInterval = 10f;


    public override void OnNetworkSpawn()
    {
        if (IsServer) 
        {
            StartCoroutine(SpawnBuffsCoroutine());
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

}
