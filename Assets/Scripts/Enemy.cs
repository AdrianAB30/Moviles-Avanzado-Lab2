using UnityEngine;
using Unity.Netcode;

public class Enemy : NetworkBehaviour
{
    public float speed = 3f;

    private void Update()
    {
        if (!IsServer) return; 

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length == 0) return;

        GameObject closest = players[0];
        float minDist = Vector3.Distance(transform.position, closest.transform.position);

        for (int i = 1; i < players.Length; i++)
        {
            float dist = Vector3.Distance(transform.position, players[i].transform.position);
            if (dist < minDist)
            {
                closest = players[i];
                minDist = dist;
            }
        }

        Vector3 targetPos = new Vector3(closest.transform.position.x, transform.position.y, closest.transform.position.z);
        Vector3 dir = (targetPos - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }
}
