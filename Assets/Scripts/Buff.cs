using DG.Tweening;
using Unity.Netcode;
using UnityEngine;

public class Buff : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            PlayerControl player = other.GetComponent<PlayerControl>();
            if (player != null)
            {
                int bonus = Random.Range(1, 4); 
                player.ApplyBuffServerRpc(bonus);
                Debug.Log("Jugador agarró buff + " + bonus);

                var renderer = player.GetComponentInChildren<Renderer>();
                renderer.material.DOColor(Color.yellow, 0.2f).OnComplete(() => {
                    renderer.material.DOColor(Color.white, 0.2f);
                });
            }

            GetComponent<NetworkObject>().Despawn();
        }
    }
}
