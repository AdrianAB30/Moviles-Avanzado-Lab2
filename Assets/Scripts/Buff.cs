using UnityEngine;
using Unity.Netcode;

public class Buff : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            Debug.Log("Jugador agarró buff");

            GetComponent<NetworkObject>().Despawn();
        }
    }
}
