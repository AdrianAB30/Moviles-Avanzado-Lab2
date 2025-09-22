using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;
using static UnityEditor.Experimental.GraphView.GraphView;

public class GameManager : NetworkBehaviour
{
    private NetworkManager network;
    public Dictionary<string, PlayerData> playerStatesByAccountID = new();

    [SerializeField]
    private static GameManager instance;
    public GameObject playerPrefab;
    public Action OnConnection;
    public UIManager uiManager;

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
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleConnect;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleDisconnect;
        }

        if (IsClient && !IsHost)
        {
            OnConnection?.Invoke();
        }
    }
    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleConnect;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleDisconnect;
        }
    }

    public void StartHost()
    {
        if (NetworkManager.Singleton.StartHost())
        {
            string hostAccountID = "Host_" + NetworkManager.Singleton.LocalClientId;

            var prefabStats = playerPrefab.GetComponent<PlayerControl>();
            int hp = prefabStats != null ? prefabStats.maxHealth : 100;
            int atk = prefabStats != null ? prefabStats.baseAttack.Value : 10;

            RegisterPlayerServerRpc(hostAccountID, NetworkManager.Singleton.LocalClientId, hp, atk);
            print("Se conecto el host con la cuenta: " + hostAccountID + " HP=" + hp + " ATK=" + atk);
        }
    }
    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        uiManager.ShowLoginPanel();
    }
    private void HandleConnect(ulong clientID)
    {
        print("jugador " + clientID + " se conecto");

    }
    private void HandleDisconnect(ulong clientID)
    {
        print("jugador " + clientID + " se fue");

    }
    public void DisconnectLocalClient()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.Shutdown();
            print("El host cerró la sesión");
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            ulong localId = NetworkManager.Singleton.LocalClientId;
            NetworkManager.Singleton.Shutdown();
            print("El cliente " + localId + " se desconectó");
        }
    }

    [Rpc(SendTo.Server)]
    public void RegisterPlayerServerRpc(string accountID, ulong ID, int health, int attack)
    {
        if (!playerStatesByAccountID.TryGetValue(accountID, out PlayerData data))
        {
            PlayerData newData = new PlayerData(accountID, Vector3.zero, health, attack);
            playerStatesByAccountID[accountID] = newData;
            SpawnPlayerServer(ID, newData);
            print("nuevo jugador " + accountID + " con HP=" + health + " ATK=" + attack);
        }
        else
        {
            print("bienvenido de nuevo " + accountID + " con HP=" + data.health + " ATK=" + data.baseAttack);
            SpawnPlayerServer(ID, data);
        }
    }
    public void SpawnPlayerServer(ulong ID, PlayerData data)
    {
        if (!IsServer) return;
        GameObject player = Instantiate(playerPrefab);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(ID, true);
        player.GetComponent<PlayerControl>().SetData(data);
    }

    
    public static GameManager Instance => instance;
}
