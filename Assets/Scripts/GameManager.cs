using UnityEngine;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    private NetworkManager network;
    [SerializeField] private Transform[] spawnPoints;

    private static GameManager instance;

    private int nextSpawnIndex = 0; 

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
            return;
        }

        network = NetworkManager.Singleton;
        network.OnClientConnectedCallback += OnClientConnected;
    }

    public override void OnNetworkSpawn()
    {
        print(NetworkManager.Singleton.LocalClientId);
    }

    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }

    public void StartServer()
    {
        NetworkManager.Singleton.StartServer();
    }

    private void OnClientConnected(ulong clientId)
    {
        if (IsServer)
        {
            Transform spawn = spawnPoints[nextSpawnIndex];

            NetworkClient client;
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out client))
            {
                NetworkObject playerObj = client.PlayerObject;
                if (playerObj != null)
                {
                    playerObj.transform.position = spawn.position;
                    playerObj.transform.rotation = spawn.rotation;
                }
            }

            nextSpawnIndex++;
            if (nextSpawnIndex >= spawnPoints.Length)
            {
                nextSpawnIndex = 0;
            }
        }
    }

    public static GameManager Instance => instance;
}
