using Unity.Netcode;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class DamagePopUps : NetworkBehaviour
{
    [SerializeField] private TMP_Text textMesh;
    [SerializeField] private float duration = 1f;
    [SerializeField] private float moveUpDistance = 1f;
    [SerializeField] private float spawnYOffset = 2f;

    public void Setup(int value, Color color)
    {
        transform.position += Vector3.up * spawnYOffset;

        string prefix = value > 0 ? "+" : "-";
        textMesh.text = prefix + Mathf.Abs(value);
        textMesh.color = color;

        Sequence seq = DOTween.Sequence();

        if (value < 0)
        {
            seq.Append(textMesh.transform.DOShakePosition(0.8f, strength: new Vector3(0.5f, 0, 0)));
        }

        seq.Append(transform.DOMove(transform.position + Vector3.up * moveUpDistance, duration));
        seq.Join(textMesh.DOFade(0, duration));

        seq.OnComplete(() =>
        {
            if (IsServer)
            {
                GetComponent<NetworkObject>().Despawn();
            }
        });
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void SetupClientRpc(int value, Color color)
    {
        Setup(value, color);
    }
}
