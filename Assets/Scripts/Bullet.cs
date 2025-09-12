using UnityEngine;
using Unity.Netcode;

public class Bullet : NetworkBehaviour
{
    public float speed = 20f;
    public float lifeTime = 3f;

    private Rigidbody rb;
    private float timer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        timer = 0f;

        if (IsServer)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            if (float.IsNaN(transform.forward.x) || float.IsNaN(transform.forward.y) || float.IsNaN(transform.forward.z))
            {
                transform.rotation = Quaternion.identity;
            }

            rb.AddForce(transform.forward * speed, ForceMode.Impulse);
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            Deactivate();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Enemy") || other.CompareTag("Ground"))
        {
            Deactivate();
        }
    }

    private void Deactivate()
    {
        gameObject.SetActive(false);
    }
}
