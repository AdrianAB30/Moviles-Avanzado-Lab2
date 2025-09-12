using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections;

public class PlayerControl : NetworkBehaviour
{
    public static event Action<Transform> LocalPlayerSpawned;

    [Header("Movimiento")]
    public float speed = 5f;
    public float rollForce = 8f;
    public float groundCheckDistance = 1.1f;
    public LayerMask groundLayer;

    [Header("Disparo")]
    [SerializeField] private Transform firePoint; 

    private Rigidbody rb;
    private Animator animator;

    private float horizontal;
    private float vertical;
    [SerializeField] private float rotationSpeed = 10f;

    private Vector3 targetDirection = Vector3.zero;
    private bool isRolling = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
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
        if (!IsOwner || isRolling) return;

        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");

        Vector3 move = new Vector3(horizontal, 0, vertical) * speed;
        rb.MovePosition(rb.position + move * Time.deltaTime);

        UpdateMoveAnimationServerRpc(horizontal, vertical);

        if (move != Vector3.zero)
        {
            UpdateRotationServerRpc(move.normalized);
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetMouseButtonDown(0))
        {
            ShootServerRpc(firePoint.position, firePoint.rotation);
        }

        if (targetDirection.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        TryRoll();
    }

    #region Roll
    private void TryRoll()
    {
        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded() && !isRolling)
        {
            Vector3 rollDir = new Vector3(horizontal, 0, vertical).normalized;
            if (rollDir == Vector3.zero)
                rollDir = transform.forward;

            PerformRollServerRpc(rollDir);
        }
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }

    private void OnDrawGizmos()
    {
        if (firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(firePoint.position, 0.1f); 
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PerformRollServerRpc(Vector3 dir, ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId) return;

        rb.AddForce(dir * rollForce, ForceMode.Impulse);
        StartCoroutine(RollCoroutine());

        RollStateClientRpc(true);
    }

    private IEnumerator RollCoroutine()
    {
        yield return new WaitForSeconds(1.5f);
        RollStateClientRpc(false);
    }

    [ClientRpc]
    private void RollStateClientRpc(bool state)
    {
        isRolling = state;
        if (animator != null)
            animator.SetBool("isRoll", state);
    }
    #endregion

    #region Animaciones
    [Rpc(SendTo.Server)]
    private void UpdateMoveAnimationServerRpc(float h, float v)
    {
        UpdateMoveAnimationClientRpc(h, v);
    }

    [Rpc(SendTo.Everyone)]
    private void UpdateMoveAnimationClientRpc(float h, float v)
    {
        if (animator == null) return;

        animator.SetFloat("VelX", h);
        animator.SetFloat("VelY", v);
        animator.SetBool("isMove", (h != 0 || v != 0));
    }
    #endregion

    #region Rotacion
    [Rpc(SendTo.Server)]
    private void UpdateRotationServerRpc(Vector3 dir)
    {
        UpdateRotationClientRpc(dir);
    }

    [Rpc(SendTo.Everyone)]
    private void UpdateRotationClientRpc(Vector3 dir)
    {
        targetDirection = dir;
    }
    #endregion

    #region Disparo
    [ServerRpc]
    private void ShootServerRpc(Vector3 pos, Quaternion rot, ServerRpcParams rpcParams = default)
    {
        GameObject bullet = BulletPool.Instance.GetBullet();
        if (bullet != null)
        {
            bullet.transform.position = pos;
            bullet.transform.rotation = rot;
            bullet.SetActive(true);

            ShootClientRpc(bullet.GetComponent<NetworkObject>().NetworkObjectId);
        }
    }
    [ClientRpc]
    private void ShootClientRpc(ulong bulletId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(bulletId, out NetworkObject netObj))
        {
            netObj.gameObject.SetActive(true);
        }
    }
    #endregion
}
