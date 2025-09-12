using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

public class BulletPool : NetworkBehaviour
{
    public static BulletPool Instance;

    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int poolSize = 10;

    private List<GameObject> bullets = new List<GameObject>();

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject bullet = Instantiate(bulletPrefab);

            var netObj = bullet.GetComponent<NetworkObject>();
            netObj.Spawn(true);

            bullet.transform.SetParent(this.transform);

            bullet.SetActive(false);

            bullets.Add(bullet);
        }
    }

    public GameObject GetBullet()
    {
        for (int i = 0; i < bullets.Count; i++)
        {
            if (!bullets[i].activeInHierarchy)
            {
                return bullets[i];
            }
        }
        return null;
    }

    public void ShootBullet(Transform firePoint)
    {
        if (!IsServer) return;

        GameObject bullet = GetBullet();
        if (bullet != null && firePoint != null)
        {
            bullet.transform.SetParent(null); 
            bullet.transform.position = firePoint.position;
            bullet.transform.rotation = firePoint.rotation;

            bullet.SetActive(true);

            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.AddForce(firePoint.forward * 20f, ForceMode.Impulse);
            }
            StartCoroutine(ReturnToPool(bullet));
        }
    }

    private IEnumerator ReturnToPool(GameObject bullet)
    {
        yield return new WaitUntil(() => !bullet.activeInHierarchy);
        bullet.transform.SetParent(transform); 
    }
}
