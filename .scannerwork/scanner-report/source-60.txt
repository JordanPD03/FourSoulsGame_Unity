using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla la UI de las fases del turno
/// Muestra la fase actual y permite al jugador avanzar entre fases
/// </summary>
public class TurnPhaseUI : MonoBehaviour
{
    [Header("Referencias de Texto")]
    [Tooltip("Texto que muestra la fase actual")]
    public TextMeshProUGUI phaseText;
    
    [Tooltip("Texto que muestra el nombre del jugador actual")]
    public TextMeshProUGUI playerNameText;

    [Header("Botones de Fase")]
    [Tooltip("Botón para robar carta (solo visible en fase Draw)")]
    public Button drawButton;
    
    [Tooltip("Botón para terminar turno (visible en fase Action)")]
    public Button endTurnButton;

    [Header("Configuración Visual")]
    [Tooltip("Colores para cada fase")]
    public Color startPhaseColor = new Color(0.2f, 0.6f, 1f);    // Azul
    public Color drawPhaseColor = new Color(0.3f, 0.8f, 0.3f);   // Verde
    public Color actionPhaseColor = new Color(1f, 0.8f, 0.2f);   // Amarillo
    public Color endPhaseColor = new Color(0.8f, 0.3f, 0.3f);    // Rojo

    [Header("Panel de Fondo (Opcional)")]
    public Image backgroundPanel;

    private bool hasDrawnCard = false; // Para controlar si ya robó en esta fase Draw

    void Start()
    {
        // Suscribirse a eventos del GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged += HandlePhaseChanged;
            GameManager.Instance.OnPlayerTurnChanged += HandlePlayerTurnChanged;
            GameManager.Instance.OnCardDrawn += HandleCardDrawn;
        }

        // Configurar botones
        if (drawButton != null)
            drawButton.onClick.AddListener(OnDrawButtonClicked);
        
        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(OnEndTurnButtonClicked);

        // Actualizar UI inicial
        UpdateUI();
    }

    void OnDestroy()
    {
        // Desuscribirse de eventos
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
            GameManager.Instance.OnPlayerTurnChanged -= HandlePlayerTurnChanged;
            GameManager.Instance.OnCardDrawn -= HandleCardDrawn;
        }
    }

    /// <summary>
    /// Actualiza toda la UI según la fase actual
    /// </summary>
    private void UpdateUI()
    {
        if (GameManager.Instance == null) return;

        GamePhase currentPhase = GameManager.Instance.GetCurrentPhase();
        PlayerData currentPlayer = GameManager.Instance.GetCurrentPlayer();

        if (currentPlayer == null) return;

        // Actualizar texto de fase
        UpdatePhaseText(currentPhase);

        // Actualizar nombre de jugador
        if (playerNameText != null)
            playerNameText.text = currentPlayer.playerName;

        // Actualizar visibilidad y estado de botones
        UpdateButtons(currentPhase);

        // Actualizar color de fondo
        UpdateBackgroundColor(currentPhase);
    }

    /// <summary>
    /// Actualiza el texto de la fase con descripción
    /// </summary>
    private void UpdatePhaseText(GamePhase phase)
    {
        if (phaseText == null) return;

        string phaseDescription = phase switch
        {
            GamePhase.Start => "Fase de Inicio\n<size=70%>Efectos de inicio de turno</size>",
            GamePhase.Draw => "Fase de Robo\n<size=70%>Robando 1 carta...</size>",
            GamePhase.Action => "Fase de Acción\n<size=70%>Compra, ataca o termina turno</size>",
            GamePhase.End => "Fin de Turno\n<size=70%>Descarta y efectos de fin</size>",
            _ => "Fase Desconocida"
        };

        phaseText.text = phaseDescription;
    }

    /// <summary>
    /// Actualiza la visibilidad y estado de los botones
    /// </summary>
    private void UpdateButtons(GamePhase phase)
    {
        // Botón de robo
        if (drawButton != null)
        {
            // Robo ahora es automático; ocultar el botón
            drawButton.gameObject.SetActive(false);
        }

        // Botón de terminar turno
        if (endTurnButton != null)
        {
            bool canEndTurn = phase == GamePhase.Action;
            endTurnButton.gameObject.SetActive(phase == GamePhase.Action);
            endTurnButton.interactable = canEndTurn;
        }
    }

    /// <summary>
    /// Actualiza el color de fondo según la fase
    /// </summary>
    private void UpdateBackgroundColor(GamePhase phase)
    {
        if (backgroundPanel == null) return;

        Color targetColor = phase switch
        {
            GamePhase.Start => startPhaseColor,
            GamePhase.Draw => drawPhaseColor,
            GamePhase.Action => actionPhaseColor,
            GamePhase.End => endPhaseColor,
            _ => Color.white
        };

        backgroundPanel.color = targetColor;
    }

    #region Event Handlers

    /// <summary>
    /// Maneja el cambio de fase
    /// </summary>
    private void HandlePhaseChanged(GamePhase newPhase)
    {
        // Resetear flag de robo cuando entramos a la fase Draw
        if (newPhase == GamePhase.Draw)
        {
            hasDrawnCard = false;
        }

        UpdateUI();
    }

    /// <summary>
    /// Maneja el cambio de jugador
    /// </summary>
    private void HandlePlayerTurnChanged(int playerIndex)
    {
        hasDrawnCard = false;
        UpdateUI();
    }

    /// <summary>
    /// Maneja cuando se roba una carta
    /// </summary>
    private void HandleCardDrawn(PlayerData player, CardData card)
    {
        hasDrawnCard = true;
        UpdateUI();
    }

    #endregion

    #region Button Callbacks

    /// <summary>
    /// Callback del botón de robar carta
    /// </summary>
    private void OnDrawButtonClicked()
    {
        if (hasDrawnCard)
        {
            Debug.LogWarning("[TurnPhaseUI] Ya robaste una carta en este turno!");
            return;
        }

        // Robar carta usando el GameManager
        GameManager.Instance?.TryDrawCardWithAnimation();
        
        // Nota: hasDrawnCard se actualizará en HandleCardDrawn
    }

    /// <summary>
    /// Callback del botón de terminar turno
    /// </summary>
    private void OnEndTurnButtonClicked()
    {
        if (GameManager.Instance == null) return;

        Debug.Log("[TurnPhaseUI] Terminando turno...");
        GameManager.Instance.EndCurrentTurn();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Fuerza la actualización de la UI (útil para debugging)
    /// </summary>
    public void ForceUpdateUI()
    {
        UpdateUI();
    }

    #endregion
}
