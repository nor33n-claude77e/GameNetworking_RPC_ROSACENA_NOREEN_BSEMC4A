using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthAndScore : NetworkBehaviour
{
    [Header("Network Variables")]
    public NetworkVariable<int> Health = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> Score = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("UI References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI scoreText;

    private NetworkAnimator networkAnimator;
    private PlayerController playerController;

    private void Awake()
    {
        networkAnimator = GetComponent<NetworkAnimator>();
        playerController = GetComponent<PlayerController>();
    }

    public override void OnNetworkSpawn()
    {
        Health.OnValueChanged += OnHealthChanged;
        Score.OnValueChanged += OnScoreChanged;

        UpdateHealthUI(Health.Value);
        UpdateScoreUI(Score.Value);
    }

    public override void OnNetworkDespawn()
    {
        Health.OnValueChanged -= OnHealthChanged;
        Score.OnValueChanged -= OnScoreChanged;
    }

    private void OnHealthChanged(int previousValue, int newValue)
    {
        UpdateHealthUI(newValue);
    }

    private void OnScoreChanged(int previousValue, int newValue)
    {
        UpdateScoreUI(newValue);
    }

    private void UpdateHealthUI(int currentHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.value = Mathf.Clamp01(currentHealth / 100f);
        }
    }

    private void UpdateScoreUI(int currentScore)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {currentScore}";
        }
    }

    public void TakeDamage(int damageAmount, ulong shooterClientId)
    {
        if (!IsServer || Health.Value <= 0) return;

        Health.Value = Mathf.Max(0, Health.Value - damageAmount);
        Debug.Log($"Player {OwnerClientId} took {damageAmount} damage. Health: {Health.Value}");
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(shooterClientId, out var shooterClient))
        {
            if (shooterClient.PlayerObject.TryGetComponent(out PlayerHealthAndScore shooterHealthScript))
            {
                shooterHealthScript.Score.Value += 10;
                Debug.Log($"Shooter {shooterClientId} gained 10 pts! Score: {shooterHealthScript.Score.Value}");
            }
        }
        if (Health.Value <= 0)
        {
            HandleDeath();
        }
    }

    private void HandleDeath()
    {
        Debug.Log($"Player {OwnerClientId} has DIED!");

        if (networkAnimator != null)
        {
            networkAnimator.SetTrigger("Die");
        }

        if (playerController != null)
        {
            playerController.SetDeadState(true);
        }
    }
}