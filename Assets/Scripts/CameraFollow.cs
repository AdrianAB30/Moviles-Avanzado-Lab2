using UnityEngine;
using Unity.Netcode;
using Cinemachine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera vcam;

    void Awake()
    {
        vcam = GetComponentInChildren<CinemachineVirtualCamera>();
    }
    void OnEnable()
    {
        PlayerControl.LocalPlayerSpawned += OnLocalPlayerSpawned;
    }

    void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            PlayerControl.LocalPlayerSpawned -= OnLocalPlayerSpawned;

        }
    }

    void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkObject local = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (local != null)
            {
                OnLocalPlayerSpawned(local.transform);
            }
        }
    }
    private void OnLocalPlayerSpawned(Transform player)
    {
        if (vcam == null) return;
        vcam.Follow = player;
        vcam.LookAt = player;
    }
}
