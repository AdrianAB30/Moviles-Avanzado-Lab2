using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections;

public class PlayerControl : NetworkBehaviour
{
    public static event Action<Transform> LocalPlayerSpawned;

    public float speed = 5f;
    public float rollForce = 8f;
    public float groundCheckDistance = 1.1f;
    public LayerMask groundLayer;

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
        if (targetDirection.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        TryRoll();
    }

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
        Gizmos.color = Color.green;
        Debug.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
    }

    #region Roll
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

        bool isMoving = (h != 0 || v != 0);
        animator.SetBool("isMove", isMoving);
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

}
