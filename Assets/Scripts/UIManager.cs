using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField loginInput;
    [SerializeField] private Button ConnectButton;
    [SerializeField] private GameObject loginPanel;

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnConnection += ShowLoginPanel;
    }
    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnConnection -= ShowLoginPanel;
    }
    private void Start()
    {
        loginPanel.SetActive(false);
    }
    public void ShowLoginPanel()
    {
        if (NetworkManager.Singleton.IsHost) return;

        loginPanel.SetActive(true);
        loginInput.text = "";
        ConnectButton.interactable = true;
        loginInput.interactable = true;
    }
    public void OnSubmitName()
    {
        string accountID = loginInput.text;
        if (!string.IsNullOrEmpty(accountID))
        {
            var prefabStats = GameManager.Instance.playerPrefab.GetComponent<PlayerControl>();
            int hp = prefabStats != null ? prefabStats.maxHealth : 100;
            int atk = prefabStats != null ? prefabStats.baseAttack.Value : 10;

            GameManager.Instance.RegisterPlayerServerRpc(accountID, NetworkManager.Singleton.LocalClientId, hp, atk);

            ConnectButton.interactable = false;
            loginInput.interactable = false;
            loginPanel.SetActive(false);
        }
    }
}
