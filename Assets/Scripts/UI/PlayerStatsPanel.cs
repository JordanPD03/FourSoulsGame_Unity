using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Panel UI que muestra las estadísticas de un jugador.
/// Actualiza automáticamente cuando cambian los valores.
/// </summary>
public class PlayerStatsPanel : MonoBehaviour
{
    [Header("Player Configuration")]
    [Tooltip("Índice del jugador (0, 1, 2, 3...)")]
    [SerializeField] private int playerIndex = 0;

    [Header("UI References")]
    [Tooltip("Texto del nombre del jugador")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    
    [Tooltip("Texto de vida (actual/máxima)")]
    [SerializeField] private TextMeshProUGUI healthText;
    
    [Tooltip("Texto de monedas")]
    [SerializeField] private TextMeshProUGUI coinsText;
    
    [Tooltip("Texto de cantidad de cartas de loot")]
    [SerializeField] private TextMeshProUGUI lootCardsText;
    
    [Tooltip("Texto de cantidad de tesoros")]
    [SerializeField] private TextMeshProUGUI treasuresText;
    
    [Tooltip("Texto de almas")]
    [SerializeField] private TextMeshProUGUI soulsText;

    [Header("Visual Feedback (Optional)")]
    [Tooltip("Imagen de fondo del panel")]
    [SerializeField] private Image backgroundImage;
    
    [Tooltip("Color cuando es el turno de este jugador")]
    [SerializeField] private Color activeTurnColor = new Color(1f, 1f, 0.5f, 1f);
    
    [Tooltip("Color cuando NO es el turno de este jugador")]
    [SerializeField] private Color inactiveTurnColor = Color.white;

    private PlayerData playerData;

    private void Start()
    {
        // Obtener referencia al jugador
        if (GameManager.Instance != null)
        {
            // Esperar un poco para que GameManager inicialice
            Invoke(nameof(InitializePlayer), 0.5f);
        }
    }

    private void InitializePlayer()
    {
        if (GameManager.Instance == null) return;

        playerData = GameManager.Instance.GetPlayer(playerIndex);
        
        if (playerData == null)
        {
            Debug.LogWarning($"[PlayerStatsPanel] No se encontró jugador con índice {playerIndex}");
            return;
        }

        // Suscribirse a eventos
        SubscribeToEvents();

        // Actualización inicial
        UpdateAllStats();

        Debug.Log($"[PlayerStatsPanel] Panel inicializado para {playerData.playerName}");
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnPlayerHealthChanged += HandleHealthChanged;
        GameManager.Instance.OnPlayerCoinsChanged += HandleCoinsChanged;
        GameManager.Instance.OnCardDrawn += HandleCardDrawn;
        GameManager.Instance.OnCardPlayed += HandleCardPlayed;
        GameManager.Instance.OnSoulCollected += HandleSoulCollected;
        GameManager.Instance.OnPlayerTurnChanged += HandleTurnChanged;
    }

    private void UnsubscribeFromEvents()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnPlayerHealthChanged -= HandleHealthChanged;
        GameManager.Instance.OnPlayerCoinsChanged -= HandleCoinsChanged;
        GameManager.Instance.OnCardDrawn -= HandleCardDrawn;
        GameManager.Instance.OnCardPlayed -= HandleCardPlayed;
        GameManager.Instance.OnSoulCollected -= HandleSoulCollected;
        GameManager.Instance.OnPlayerTurnChanged -= HandleTurnChanged;
    }

    // === EVENT HANDLERS ===

    private void HandleHealthChanged(PlayerData player, int newHealth)
    {
        if (player == playerData)
        {
            UpdateHealthDisplay();
        }
    }

    private void HandleCoinsChanged(PlayerData player, int newCoins)
    {
        if (player == playerData)
        {
            UpdateCoinsDisplay();
        }
    }

    private void HandleCardDrawn(PlayerData player, CardData card)
    {
        if (player == playerData)
        {
            UpdateLootCardsDisplay();
        }
    }

    private void HandleCardPlayed(PlayerData player, CardData card)
    {
        if (player == playerData)
        {
            UpdateLootCardsDisplay();
            UpdateTreasuresDisplay();
        }
    }

    private void HandleSoulCollected(PlayerData player, CardData soul)
    {
        if (player == playerData)
        {
            UpdateSoulsDisplay();
        }
    }

    private void HandleTurnChanged(int currentPlayerIndex)
    {
        UpdateTurnIndicator(currentPlayerIndex == playerIndex);
    }

    // === UPDATE METHODS ===

    private void UpdateAllStats()
    {
        if (playerData == null) return;

        UpdatePlayerName();
        UpdateHealthDisplay();
        UpdateCoinsDisplay();
        UpdateLootCardsDisplay();
        UpdateTreasuresDisplay();
        UpdateSoulsDisplay();
    }

    private void UpdatePlayerName()
    {
        if (playerNameText != null && playerData != null)
        {
            playerNameText.text = playerData.playerName;
        }
    }

    private void UpdateHealthDisplay()
    {
        if (healthText != null && playerData != null)
        {
            healthText.text = $"{playerData.health}/{playerData.maxHealth} ❤";
        }
    }

    private void UpdateCoinsDisplay()
    {
        if (coinsText != null && playerData != null)
        {
            coinsText.text = $"{playerData.coins}¢";
        }
    }

    private void UpdateLootCardsDisplay()
    {
        if (lootCardsText != null && playerData != null)
        {
            lootCardsText.text = $"Loot: {playerData.hand.Count}";
        }
    }

    private void UpdateTreasuresDisplay()
    {
        if (treasuresText != null && playerData != null)
        {
            int totalTreasures = playerData.activeItems.Count + playerData.passiveItems.Count;
            treasuresText.text = $"Tesoros: {totalTreasures}";
        }
    }

    private void UpdateSoulsDisplay()
    {
        if (soulsText != null && playerData != null)
        {
            soulsText.text = $"Almas: {playerData.souls}";
        }
    }

    private void UpdateTurnIndicator(bool isActiveTurn)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = isActiveTurn ? activeTurnColor : inactiveTurnColor;
        }
    }

    // === PUBLIC ACCESSORS ===

    public int GetPlayerIndex()
    {
        return playerIndex;
    }

    public PlayerData GetPlayerData()
    {
        return playerData;
    }
}
