using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; 

public class UIPlayer : MonoBehaviour
{
    [SerializeField] private Image healthBar;
    private Canvas canvas;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
    }

    private void Start()
    {
        if (canvas != null)
        {
            canvas.worldCamera = Camera.main;
        }
    }

    public void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (healthBar != null)
        {
            float targetFill = (float)currentHealth / maxHealth;

            healthBar.DOKill();

            healthBar.DOFillAmount(targetFill, 0.5f).SetEase(Ease.OutBounce); 
        }
    }
}
