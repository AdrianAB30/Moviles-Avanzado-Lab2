using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class SpawnerManager : NetworkBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject buffPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private Transform[] buffSpawnPoints;
    [SerializeField] private float buffSpawnInterval = 10f;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            StartCoroutine(SpawnBuffsCoroutine());
    }

    private IEnumerator SpawnBuffsCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(buffSpawnInterval);

            Transform spawnPoint = buffSpawnPoints[Random.Range(0, buffSpawnPoints.Length)];

            GameObject buff = Instantiate(buffPrefab, spawnPoint.position, spawnPoint.rotation);
            buff.GetComponent<NetworkObject>().Spawn();
        }
    }
}
