using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class PlayerControl : NetworkBehaviour
{
    public static event Action<Transform> LocalPlayerSpawned;

    [Header("Movimiento")]
    public float speed = 5f;
    public float groundCheckDistance = 1.1f;
    [SerializeField] private float rotationSpeed = 10f;
    private bool canMove;
    public LayerMask groundLayer;

    [Header("Combate")]
    [SerializeField] private float punchCooldown = 1.0f;
    private bool canPunch = true;
    [SerializeField] private int maxHealth = 100;
    private NetworkVariable<int> health = new NetworkVariable<int>();
    [SerializeField] private int baseAttack = 10;
    private NetworkVariable<int> attackPower = new NetworkVariable<int>();

    [Header("Respawn")]
    [SerializeField] private float respawnRange = 15f;
    [SerializeField] private float respawnHeight = 1f;
    [SerializeField] private float respawnDelay = 1f;

    [Header("PopUpDamage")]
    [SerializeField] private GameObject popupPrefab;

    private Rigidbody rb;
    private Animator animator;
    private UIPlayer uiPlayer;
    private Renderer myRend;
    private float horizontal;
    private float vertical;
    private Vector3 targetDirection = Vector3.zero;
    private bool isDead = false;

    void Awake()
    {
        canMove = true;
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        myRend = GetComponentInChildren<Renderer>();
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
            uiPlayer.UpdateHealthBar(health.Value, maxHealth);
    }

    private void OnDisable()
    {
        health.OnValueChanged -= OnHealthChanged;
    }

    private new void OnDestroy()
    {
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
            UpdateRotationServerRpc(move.normalized);
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
            PerformTauntServerRpc();
    }

    private void TryPunch()
    {
        if (Input.GetMouseButtonDown(0) && canPunch && canMove)
            PlayPunchServerRpc();
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }

    [Rpc(SendTo.Server)]
    private void PerformTauntServerRpc()
    {
        PerformTauntClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PerformTauntClientRpc()
    {
        if (animator != null)
        {
            if (IsOwner)
            {
                canMove = false;
            }

            animator.SetTrigger("isTaunt");
        }
    }

    public void EndTaunt()
    {
        if (IsOwner)
            canMove = true;
    }

    [Rpc(SendTo.Server)]
    private void UpdateMoveAnimationServerRpc(float h, float v)
    {
        UpdateMoveAnimationClientRpc(h, v);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateMoveAnimationClientRpc(float h, float v)
    {
        if (animator == null) return;
        animator.SetFloat("VelX", h);
        animator.SetFloat("VelY", v);
        animator.SetBool("isMove", (h != 0 || v != 0));
    }

    [Rpc(SendTo.Server)]
    private void UpdateRotationServerRpc(Vector3 dir)
    {
        UpdateRotationClientRpc(dir);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateRotationClientRpc(Vector3 dir)
    {
        targetDirection = dir;
    }

    [Rpc(SendTo.Server)]
    private void PlayPunchServerRpc()
    {
        if (!canPunch) return;
        canPunch = false;
        PlayPunchClientRpc();
        StartCoroutine(PunchCooldownRoutine());
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayPunchClientRpc()
    {
        if (animator != null)
        {
            canMove = false;
            rb.linearVelocity = Vector3.zero; // <- respetando tu preferencia
            animator.SetTrigger("isPunch");
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

        if (other.CompareTag("Player") && other != null)
        {
            ulong targetId = other.GetComponent<NetworkObject>().OwnerClientId;
            DealDamageServerRpc(targetId, attackPower.Value);
        }

        if (other.CompareTag("Buff"))
        {
            myRend.material.DOColor(Color.yellow, 0.2f).OnComplete(() =>
            {
                myRend.material.DOColor(Color.white, 0.2f);
            });
        }
    }

    [Rpc(SendTo.Server)]
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
        ShowPopupServerRpc(-dmg, false);
        Debug.Log($"{OwnerClientId} recibió {dmg} de daño. Vida actual: {health.Value}");

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
        rb.position = spawnPos;
        rb.linearVelocity = Vector3.zero; // <- respetando tu preferencia
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

    [Rpc(SendTo.ClientsAndHost)]
    private void HidePlayerClientRpc()
    {
        SetPlayerVisibility(false);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ShowPlayerClientRpc()
    {
        SetPlayerVisibility(true);
    }

    private void SetPlayerVisibility(bool visible)
    {
        canMove = visible;

        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
            r.enabled = visible;

        var colliders = GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders)
            c.enabled = visible;

        var animators = GetComponentsInChildren<Animator>(true);
        foreach (var a in animators)
            a.enabled = visible;

        if (uiPlayer != null)
            uiPlayer.gameObject.SetActive(visible);

        if (rb != null)
            rb.isKinematic = !visible;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayHitClientRpc()
    {
        if (animator != null)
            animator.SetTrigger("isHit");
    }

    [Rpc(SendTo.Server)]
    public void ApplyBuffServerRpc(int bonus)
    {
        attackPower.Value += bonus;
        ShowPopupServerRpc(bonus, true);
        Debug.Log($"Nuevo ataque de {OwnerClientId}: {attackPower.Value}");
    }

    private void OnHealthChanged(int oldValue, int newValue)
    {
        if (uiPlayer != null)
            uiPlayer.UpdateHealthBar(newValue, maxHealth);
    }

    [Rpc(SendTo.Server)]
    public void ShowPopupServerRpc(int value, bool isBuff)
    {
        Vector3 spawnPos = transform.position + Vector3.up * 2f;
        GameObject popupObj = Instantiate(popupPrefab, spawnPos, Quaternion.identity);
        var netObj = popupObj.GetComponent<NetworkObject>();
        netObj.Spawn();

        Color popupColor = isBuff ? Color.yellow : Color.red;
        popupObj.GetComponent<DamagePopUps>().SetupClientRpc(value, popupColor);
    }
}
