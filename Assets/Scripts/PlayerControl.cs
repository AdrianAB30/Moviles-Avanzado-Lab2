using UnityEngine;
using Unity.Netcode;
using System;

public class PlayerControl : NetworkBehaviour
{
    public static event Action<Transform> LocalPlayerSpawned;

    public float speed = 5f;
    public float jumpForce = 5f;
    public float groundCheckDistance = 1.1f;
    public LayerMask groundLayer;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            LocalPlayerSpawned?.Invoke(transform); 
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 move = new Vector3(h, 0, v) * speed;
        rb.MovePosition(rb.position + move * Time.deltaTime);
        Jupming();
    }

    private void Jupming()
    {
        if (Input.GetKey(KeyCode.Space) && IsGrounded())
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Debug.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
    }
}
