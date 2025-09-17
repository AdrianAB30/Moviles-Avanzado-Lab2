using UnityEngine;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    private NetworkManager network;

    [SerializeField]
    private static GameManager instance;

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
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("Local Client ID: " + NetworkManager.Singleton.LocalClientId);
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
    public static GameManager Instance => instance;
}
