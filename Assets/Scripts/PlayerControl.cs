using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections;
using System.Collections.Generic;

public class PlayerControl : NetworkBehaviour
{
    public static event Action<Transform> LocalPlayerSpawned;

    [Header("Movimiento")]
    public float speed = 5f;
    public float groundCheckDistance = 1.1f;
    private bool canMove;
    public LayerMask groundLayer;

    [Header("Combate")]
    [SerializeField] private float punchCooldown = 1.0f;
    private bool canPunch = true;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int baseDamage = 10;
    private NetworkVariable<int> health = new NetworkVariable<int>();

    [Header("Stats")]
    [SerializeField] private int baseAttack = 10;
    private NetworkVariable<int> attackPower = new NetworkVariable<int>();

    [Header("Respawn")]
    [SerializeField] private float respawnRange = 15f;   
    [SerializeField] private float respawnHeight = 1f;   
    [SerializeField] private float respawnDelay = 1f;    

    private Rigidbody rb;
    private Animator animator;
    private UIPlayer uiPlayer;

    private float horizontal;
    private float vertical;
    [SerializeField] private float rotationSpeed = 10f;

    private Vector3 targetDirection = Vector3.zero;
    private bool isDead = false;

    void Awake()
    {
        canMove = true;
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            health.Value = maxHealth;
            attackPower.Value = baseAttack;
        }

        if (IsOwner)
            LocalPlayerSpawned?.Invoke(transform);

        uiPlayer = GetComponentInChildren<UIPlayer>(true);

        health.OnValueChanged += OnHealthChanged;

        if (uiPlayer != null)
        {
            uiPlayer.UpdateHealthBar(health.Value, maxHealth);
        }
    }

    private void OnDestroy()
    {
        // limpiar suscripción
        health.OnValueChanged -= OnHealthChanged;
    }

    void FixedUpdate()
    {
        if (!IsOwner || !canMove) return;

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

        if (targetDirection.magnitude > 0.01f && canMove)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        TryTaunt();
        TryPunch();
    }

    private void TryTaunt()
    {
        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded() && canMove)
        {
            PerformTauntServerRpc();
        }
    }
    private void TryPunch()
    {
        if (Input.GetMouseButtonDown(0) && canPunch && canMove)
        {
            PlayPunchServerRpc();
        }
    }
    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }

    [ServerRpc(RequireOwnership = false)]
    private void PerformTauntServerRpc()
    {
        PerformTauntClientRpc();
    }

    [ClientRpc]
    private void PerformTauntClientRpc()
    {
        if (animator != null)
        {
            canMove = false;
            animator.SetTrigger("isTaunt");
        }
    }
    public void EndTaunt()
    {
        canMove = true;
    }

    [ServerRpc]
    private void UpdateMoveAnimationServerRpc(float h, float v)
    {
        UpdateMoveAnimationClientRpc(h, v);
    }

    [ClientRpc]
    private void UpdateMoveAnimationClientRpc(float h, float v)
    {
        if (animator == null) return;

        animator.SetFloat("VelX", h);
        animator.SetFloat("VelY", v);
        animator.SetBool("isMove", (h != 0 || v != 0));
    }

    [ServerRpc]
    private void UpdateRotationServerRpc(Vector3 dir)
    {
        UpdateRotationClientRpc(dir);
    }

    [ClientRpc]
    private void UpdateRotationClientRpc(Vector3 dir)
    {
        targetDirection = dir;
    }

    [ServerRpc]
    private void PlayPunchServerRpc()
    {
        PlayPunchClientRpc();
    }

    [ClientRpc]
    private void PlayPunchClientRpc()
    {
        if (animator != null && canPunch)
        {
            canPunch = false;
            canMove = false;
            animator.SetTrigger("isPunch");

            if (IsOwner)
            {
                StartCoroutine(PunchCooldownRoutine());
            }
        }
    }

    private IEnumerator PunchCooldownRoutine()
    {
        yield return new WaitForSeconds(punchCooldown);
        canPunch = true;
    }

    public void EndPunch() => canMove = true;

    private void OnTriggerEnter(Collider other)
    {

        if (!IsOwner) return;

        if (other.CompareTag("Player") && this.CompareTag("Player") == false && other.CompareTag("Punch"))
        {
            return;
        }

        if (other.CompareTag("Player") && other != null)
        {
            ulong targetId = other.GetComponent<NetworkObject>().OwnerClientId;
            DealDamageServerRpc(targetId, attackPower.Value);
        }
    }

    [ServerRpc]
    public void DealDamageServerRpc(ulong targetId, int damage)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(targetId, out var client))
        {
            PlayerControl targetPlayer = client.PlayerObject.GetComponent<PlayerControl>();
            if (targetPlayer != null)
            {
                targetPlayer.TakeDamage(damage);
                targetPlayer.PlayHitClientRpc();
            }
        }
    }

    private void TakeDamage(int dmg)
    {
        if (!IsServer) return;

        if (isDead) return;

        health.Value -= dmg;
        if (health.Value <= 0)
        {
            isDead = true;
            Debug.Log($"{OwnerClientId} murió.");
            attackPower.Value = baseAttack;

            HidePlayerClientRpc();
            StartCoroutine(RespawnCoroutine());
        }
    }

    private IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        Vector3 spawnPos = GetRandomSpawnPosition();

        transform.position = spawnPos;

        health.Value = maxHealth;

        ShowPlayerClientRpc();

        isDead = false;
    }

    private Vector3 GetRandomSpawnPosition()
    {
        float x = UnityEngine.Random.Range(-respawnRange, respawnRange);
        float z = UnityEngine.Random.Range(-respawnRange, respawnRange);
        return new Vector3(x, respawnHeight, z);
    }

    [ClientRpc]
    private void HidePlayerClientRpc()
    {
        SetPlayerVisibility(false);
    }

    [ClientRpc]
    private void ShowPlayerClientRpc()
    {
        SetPlayerVisibility(true);
    }

    private void SetPlayerVisibility(bool visible)
    {
        canMove = visible;

        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            r.enabled = visible;
        }
        var colliders = GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders)
        {
            c.enabled = visible;
        }

        var animators = GetComponentsInChildren<Animator>(true);
        foreach (var a in animators)
        {
            a.enabled = visible;
        }

        if (uiPlayer != null)
        {
            uiPlayer.gameObject.SetActive(visible);
        }

        if (rb != null)
        {
            rb.isKinematic = !visible;
        }
    }

    [ClientRpc]
    private void PlayHitClientRpc()
    {
        if (animator != null)
        {
            animator.SetTrigger("isHit");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ApplyBuffServerRpc(int bonus)
    {
        attackPower.Value += bonus;
        Debug.Log($"Nuevo ataque de {OwnerClientId}: {attackPower.Value}");
    }

    private void OnHealthChanged(int oldValue, int newValue)
    {
        if (uiPlayer != null)
        {
            uiPlayer.UpdateHealthBar(newValue, maxHealth);
        }
    }
}
