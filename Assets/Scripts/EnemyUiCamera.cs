using System.Globalization;
using UnityEditor.PackageManager;
using UnityEngine;
using Unity.Netcode;

public class EnemyUiCamera : NetworkBehaviour
{
    [SerializeField] private Canvas worldCanvas;

    public override void OnNetworkSpawn()
    {
        if (IsOwner || IsClient) 
        {
            if (worldCanvas != null)
            {
                worldCanvas.worldCamera = Camera.main;
            }
        }
    }
}
