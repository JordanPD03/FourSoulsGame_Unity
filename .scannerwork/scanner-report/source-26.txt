using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using DG.Tweening;
using UnityEngine.UI;

/// <summary>
/// GameManager centralizado que controla el flujo del juego
/// Preparado para multiplayer: separa lógica de vista y emite eventos
/// </summary>
public class GameManager : MonoBehaviour
{
    // Singleton
    public static GameManager Instance { get; private set; }

    // Estado del juego
    [Header("Game State")]
        [SerializeField] private GamePhase currentPhase = GamePhase.Start;
    [SerializeField] private int currentPlayerIndex = 0;
    
    // Jugadores
    [Header("Players")]
    [SerializeField] private List<PlayerData> players = new List<PlayerData>();
    
    // Mazos
    [Header("Decks")]
    [SerializeField] private List<CardData> lootDeck = new List<CardData>();
    [SerializeField] private List<CardData> treasureDeck = new List<CardData>();
    [SerializeField] private List<CardData> monsterDeck = new List<CardData>();
    
    // Configuraciones de mazos (ScriptableObjects)
    [Header("Deck Configurations")]
    [Tooltip("Configuración del mazo de Loot (con cantidades por carta)")]
    [SerializeField] private DeckConfiguration lootDeckConfig;
    [Tooltip("Configuración del mazo de Treasure")]
    [SerializeField] private DeckConfiguration treasureDeckConfig;
    [Tooltip("Configuración del mazo de Monster")]
    [SerializeField] private DeckConfiguration monsterDeckConfig;
    
    [Header("Game Start Settings")]
    [Tooltip("Cartas que cada jugador roba al inicio de la partida")]
    [SerializeField] private int startingHandSize = 3;
    [Tooltip("Monedas que cada jugador recibe al inicio")]
    [SerializeField] private int startingCoins = 3;
    
    // Pilas de descarte
    [Header("Discard Piles")]
    [SerializeField] private List<CardData> lootDiscard = new List<CardData>();
    [SerializeField] private List<CardData> treasureDiscard = new List<CardData>();
    [SerializeField] private List<CardData> monsterDiscard = new List<CardData>();
    
    // Monstruos activos en el tablero
    [Header("Active Cards")]
    [SerializeField] private List<CardData> activeMonsters = new List<CardData>();

    [Header("Shop (Treasure)")]
    [SerializeField] private int initialShopSlots = 2;
    [SerializeField] private int maxShopSlots = 4;
    [SerializeField] private List<CardData> shopRevealed = new List<CardData>();
    
    // Configuración
    [Header("Game Rules")]
    [SerializeField] private int maxHandSize = 10;
    [SerializeField] private int soulsToWin = 4;
    
    // Referencias UI
    [Header("UI References")]
    [SerializeField] private PlayerHandUI playerHandUI;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Sprite cardBackSprite;
    [SerializeField] private RectTransform animationLayer;
    [SerializeField] private TurnAnnouncerUI turnAnnouncerUI; // Referencia al anunciador de turnos
    [SerializeField] private DiceRollerUI diceRollerUI;       // UI del lanzamiento de dado
    [Header("Selection Overlay (Optional)")]
    [SerializeField] private Image selectionDimOverlay;       // Panel semitransparente para enfocar la mano durante selección
    
    [Header("Draw Animation Settings")]
    [SerializeField] private Vector2 animCardSize = new Vector2(220, 308);
    [SerializeField] private bool usePrefabSizeForAnim = true;
    [SerializeField] [Range(0.1f, 1.0f)] private float prefabSizeAnimScale = 0.4f;
    [SerializeField] private float slideDuration = 0.6f;
    [SerializeField] private Ease slideEase = Ease.InQuad;
    [SerializeField] private float appearScale = 0.9f;
    [SerializeField] private float appearDuration = 0.2f;
    
    private bool isDrawing = false;
    private int turnTimerPauseCount = 0; // contador de pausas anidadas
    // Posición de inicio pendiente para la animación de monedas (si se desea origen específico)
    private Vector2? pendingCoinAnimStartScreenPos = null;
    private bool pendingCoinShouldDelay = false;
    
    [Header("Turn Timer")]
    [SerializeField] private bool useTurnTimer = true;
    [Tooltip("Duración del turno en segundos (derivado automáticamente de minutos/segundos)")]
    [HideInInspector] [SerializeField] private float turnDurationSeconds = 300f; // 5 minutos
    [Tooltip("Minutos por turno (solo editor)")]
    [Min(0)] [SerializeField] private int turnMinutes = 5;
    [Tooltip("Segundos por turno (0-59, solo editor)")]
    [Range(0,59)] [SerializeField] private int turnSeconds = 0;
    private float turnTimeRemaining = 0f;
    private bool turnTimerActive = false;
    
    // Eventos del sistema de turnos (para UI y networking)
    public event Action<GamePhase> OnPhaseChanged;
    public event Action<int> OnPlayerTurnChanged;
    public event Action<PlayerData, CardData> OnCardDrawn;
    public event Action<PlayerData, CardData> OnCardPlayed;
    public event Action<PlayerData, CardData> OnCardDiscarded;
    public event Action<PlayerData, int> OnPlayerHealthChanged;  // Player, nueva vida
    public event Action<PlayerData, int> OnPlayerCoinsChanged;   // Player, nuevas monedas
    public event Action<PlayerData, CardData> OnSoulCollected;   // Player, alma recolectada
    public event Action<PlayerData> OnPlayerDamaged;
    public event Action<PlayerData> OnPlayerDied;
    public event Action<PlayerData> OnPlayerWon;
    public event Action<float, float> OnTurnTimerUpdated; // remaining, total
    public event Action OnTurnTimerExpired;
    public event Action<int> OnDiceRolled; // Resultado de dado (1..N)

    // Combate (estado de bloqueo y límites de ataques)
    public event Action<PlayerData, MonsterSlot> OnCombatStarted;
    public event Action<PlayerData, MonsterSlot> OnCombatEnded;
    public event Action<List<CardData>> OnShopUpdated; // Lista de cartas reveladas (puede contener null en slots vacíos)
    public event Action<PlayerData, CardData, int> OnTreasurePurchased; // player, card, slotIndex (-1 si desde mazo)

    private bool isInCombat = false;
    private PlayerData combatPlayer = null;
    private MonsterSlot combatSlot = null;
    private bool combatAwaitingRoll = false;   // true cuando el jugador puede solicitar la siguiente tirada
    private bool combatIsRolling = false;      // true durante la resolución de una tirada
    private bool combatRollRequested = false;  // bandera para avanzar a la siguiente tirada
    // Anuncios de turno: no anunciar hasta que el setup esté completo (tras selección de personaje)
    private bool isReadyForTurnAnnouncements = false;
    public bool IsReadyForTurnAnnouncements => isReadyForTurnAnnouncements;
    // Ataques restantes por turno por jugadorId
    private System.Collections.Generic.Dictionary<int, int> remainingAttacksByPlayer = new System.Collections.Generic.Dictionary<int, int>();

    // Selección de descarte (muerte): estado para elegir carta de Loot
    private bool awaitingLootDiscardSelection = false;
    private System.Action<CardData> onLootDiscardSelected = null;
    private PlayerData lootSelectionPlayer = null;
    private CardData pendingLootSelectionCandidate = null;

    // Monedas: animación pendiente esperando llegada al descarte (para Loot)
    private bool pendingCoinWaitForDiscard = false;
    
    [Header("Dev/Testing")]
    [Tooltip("Simular que la mano local siempre muestra la del jugador actual al cambiar de turno")]
    public bool simulateLocalHandFollowsCurrentTurn = true;
    // Control de supresión visual de la pila de descarte hasta que termine la animación
    private readonly System.Collections.Generic.Dictionary<CardType, int> _discardVisualSuppressCount = new System.Collections.Generic.Dictionary<CardType, int>();
    private PlayerData pendingCoinPlayer = null;
    private int pendingCoinAmount = 0;

    // Selección de descarte (muerte): estado para elegir OBJETO no eterno (activo/pasivo)
    private bool awaitingItemDiscardSelection = false;
    private System.Action<CardData> onItemDiscardSelected = null;
    private PlayerData itemSelectionPlayer = null;

    // Ataque al tope del mazo de Monstruos → colocar overlay en un slot
    private bool awaitingDeckOverlayPlacement = false;
    private CardData pendingDeckOverlayMonster = null;
    private PlayerData pendingDeckOverlayPlayer = null;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Debug.Log("[GameManager] ===== INICIANDO GAME MANAGER =====");
        Debug.Log($"[GameManager] TurnAnnouncerUI asignado: {turnAnnouncerUI != null}");
        Debug.Log($"[GameManager] PlayerHandUI asignado: {playerHandUI != null}");
        Debug.Log($"[GameManager] DiceRollerUI asignado: {diceRollerUI != null}");
        EnsureUISingletons();
        
        // Suscribirse a eventos del TurnAnnouncer si está asignado
        TrySubscribeTurnAnnouncer();
        
        InitializeGame();
    }

    private void EnsureUISingletons()
    {
        // UIRegistry
        if (UIRegistry.Instance == null)
        {
            var go = new GameObject("UIRegistry");
            go.AddComponent<UIRegistry>();
        }
        // CoinGainAnimator
        if (CoinGainAnimator.Instance == null)
        {
            var go2 = new GameObject("CoinGainAnimator");
            var anim = go2.AddComponent<CoinGainAnimator>();
            // Si no se asigna manualmente, usará GameManager.animationLayer por defecto
        }

        // Registrar cualquier PlayerStatsUI existente (por si Start() corrió antes de crear el UIRegistry)
        if (UIRegistry.Instance != null)
        {
            var allStats = FindObjectsOfType<PlayerStatsUI>(includeInactive: true);
            foreach (var stats in allStats)
            {
                UIRegistry.Instance.RegisterPlayerStats(stats.playerIndex, stats);
            }
        }
    }

    private void TrySubscribeTurnAnnouncer()
    {
        Debug.Log("[GameManager] TrySubscribeTurnAnnouncer llamado");
        if (turnAnnouncerUI == null)
        {
            Debug.LogWarning("[GameManager] TurnAnnouncerUI es NULL, no se puede suscribir");
            return;
        }
        Debug.Log("[GameManager] Suscribiendo a eventos de TurnAnnouncerUI...");
        // Evitar dobles suscripciones: quitar por si acaso y volver a añadir
        turnAnnouncerUI.OnAnimationStarted -= HandleTurnBannerStarted;
        turnAnnouncerUI.OnAnimationComplete -= HandleTurnBannerCompleted;
        turnAnnouncerUI.OnAnimationStarted += HandleTurnBannerStarted;
        turnAnnouncerUI.OnAnimationComplete += HandleTurnBannerCompleted;
        Debug.Log("[GameManager] Suscripción a TurnAnnouncerUI completada");
    }

    private void OnValidate()
    {
        // Mantener coherencia: calcular segundos desde los campos de minutos/segundos del editor
        if (turnMinutes < 0) turnMinutes = 0;
        if (turnSeconds < 0) turnSeconds = 0;
        if (turnSeconds > 59) turnSeconds = 59;
        turnDurationSeconds = (turnMinutes * 60f) + turnSeconds;
    }

    private void Update()
    {
        // ========== CONTROLES DE PRUEBA (TEMPORAL) ==========
        PlayerData testPlayer = GetCurrentPlayer();
        if (testPlayer == null) return;

        // Temporizador de turno
        if (turnTimerActive)
        {
            // Pausar durante animaciones/eventos críticos
            if (turnTimerPauseCount <= 0)
            {
                turnTimeRemaining -= Time.deltaTime;
            }
            if (turnTimeRemaining < 0f) turnTimeRemaining = 0f;
            OnTurnTimerUpdated?.Invoke(turnTimeRemaining, turnDurationSeconds);

            if (turnTimeRemaining <= 0f)
            {
                turnTimerActive = false;
                OnTurnTimerExpired?.Invoke();
                Debug.Log("[GameManager] Turn time expired. Ending turn.");
                EndCurrentTurn();
                return;
            }
        }

        // VIDA
        if (Input.GetKeyDown(KeyCode.H))
            DamagePlayer(testPlayer, 1);  // H = -1 vida
        
        if (Input.GetKeyDown(KeyCode.J))
            HealPlayer(testPlayer, 1);    // J = +1 vida

        // MONEDAS
        if (Input.GetKeyDown(KeyCode.K))
            ChangePlayerCoins(testPlayer, -1);  // K = -1 moneda
        
        if (Input.GetKeyDown(KeyCode.L))
            ChangePlayerCoins(testPlayer, +1);  // L = +1 moneda

        // ALMAS
        if (Input.GetKeyDown(KeyCode.N))
        {
            // N = +1 alma
            CardData soul = new CardData(
                id: 999 + testPlayer.activeItems.Count,
                name: $"Test Soul {testPlayer.activeItems.Count + 1}",
                type: CardType.Soul,
                spritePath: ""
            );
            CollectSoul(testPlayer, soul);
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            // M = -1 alma (remover última)
            var souls = testPlayer.activeItems.FindAll(c => c.cardType == CardType.Soul);
            if (souls.Count > 0)
            {
                testPlayer.activeItems.Remove(souls[souls.Count - 1]);
                Debug.Log($"[GameManager] Removida un alma. Total: {souls.Count - 1}");
            }
        }

        // VIDA MÁXIMA (Bonus)
        if (Input.GetKeyDown(KeyCode.U))
        {
            testPlayer.maxHealth++;
            OnPlayerHealthChanged?.Invoke(testPlayer, testPlayer.health);
            Debug.Log($"[GameManager] Vida máxima aumentada a {testPlayer.maxHealth}");
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            if (testPlayer.maxHealth > 1)
            {
                testPlayer.maxHealth--;
                if (testPlayer.health > testPlayer.maxHealth)
                    testPlayer.health = testPlayer.maxHealth;
                OnPlayerHealthChanged?.Invoke(testPlayer, testPlayer.health);
                Debug.Log($"[GameManager] Vida máxima reducida a {testPlayer.maxHealth}");
            }
        }

        // PASAR DE TURNO
        if (Input.GetKeyDown(KeyCode.T)) // T = Turno (Terminar turno)
        {
            if (CanPerformAction(testPlayer, "EndTurn"))
            {
                Debug.Log("[GameManager] Tecla T presionada: terminar turno");
                EndCurrentTurn();
            }
            else
            {
                Debug.LogWarning($"[GameManager] No puedes terminar el turno durante la fase {currentPhase}");
            }
        }

        // LANZAR DADO (prueba)
        if (Input.GetKeyDown(KeyCode.R)) // R = Roll dice 1d6
        {
            Debug.Log("[GameManager] Tecla R: Lanzar 1d6");
            RollDice(6, result =>
            {
                Debug.Log($"[GameManager] Resultado del dado: {result}");
            });
        }
        // ====================================================
    }

    private void HandleTurnBannerStarted()
    {
        PauseTurnTimer();
    }

    private void HandleTurnBannerCompleted()
    {
        ResumeTurnTimer();
    }

    public void PauseTurnTimer()
    {
        turnTimerPauseCount++;
    }

    public void ResumeTurnTimer()
    {
        turnTimerPauseCount = Mathf.Max(0, turnTimerPauseCount - 1);
    }

    // Forzar que el temporizador no quede pausado por error
    private void ResetTurnTimerPause()
    {
        turnTimerPauseCount = 0;
    }

    /// <summary>
    /// Inicializa el juego con jugadores y mazos
    /// </summary>
    private void InitializeGame()
    {
        Debug.Log("[GameManager] InitializeGame() llamado");
        
        // Asegurar que exista al menos un jugador y que tengan nombres válidos
        EnsurePlayersInitialized();
        
        Debug.Log($"[GameManager] Jugadores inicializados: {players.Count}");
        if (players.Count > 0)
            Debug.Log($"[GameManager] Jugador 0: {players[0].playerName}");

        // Iniciar selección de personajes antes de continuar
        var csUI = CharacterSelectionUI.Instance;
        if (csUI == null)
        {
            // Búsqueda de respaldo, incluso en objetos inactivos
            csUI = FindObjectOfType<CharacterSelectionUI>(includeInactive: true);
            if (csUI != null)
            {
                Debug.Log("[GameManager] CharacterSelectionUI encontrado por búsqueda (incl. inactivos)");
            }
        }

        if (csUI != null)
        {
            Debug.Log("[GameManager] Iniciando selección de personajes...");
            // Asegurar que el canvas raíz esté activo para que el UI sea visible
            if (!csUI.gameObject.activeSelf)
            {
                csUI.gameObject.SetActive(true);
            }
            csUI.StartCharacterSelection(players, OnCharacterSelectionComplete);
        }
        else
        {
            Debug.LogWarning("[GameManager] CharacterSelectionUI no encontrado, continuando sin selección de personajes");
            ContinueGameSetup();
        }
    }
    
    /// <summary>
    /// Llamado cuando todos los jugadores han seleccionado sus personajes
    /// </summary>
    private void OnCharacterSelectionComplete()
    {
        Debug.Log("[GameManager] Selección de personajes completada");
        ContinueGameSetup();
    }
    
    /// <summary>
    /// Continúa con la configuración del juego después de la selección de personajes
    /// </summary>
    private void ContinueGameSetup()
    {
        // Crear y mezclar mazos
        CreateTestCards();

        // Otorgar ítems eternos de personaje a cada jugador para que sean funcionales/visibles
        GrantCharacterEternals();

        // Llenar los slots iniciales de monstruos ahora que los mazos existen
        if (MonsterSlotManager.Instance != null)
        {
            MonsterSlotManager.Instance.FillInitialSlots();
        }

        // Inicializar tienda de tesoros
        InitializeShop(initialShopSlots);

        // Dar cartas y monedas iniciales a cada jugador
        SetupStartingResources();

        // Construir la mano visual del jugador actual con las cartas iniciales
        RebuildCurrentPlayerHandUI();

        Debug.Log("[GameManager] Llamando a StartPlayerTurn(0)...");
        // Comenzar el juego
        StartPlayerTurn(0);
    }

    /// <summary>
    /// Agrega a cada jugador los objetos eternos definidos por su personaje (como tesoros pasivos/activos permanentes).
    /// </summary>
    private void GrantCharacterEternals()
    {
        foreach (var p in players)
        {
            if (p == null) continue;
            var so = FindCharacterSO(p.character);
            if (so == null || so.eternalItems == null) continue;

            foreach (var itemSO in so.eternalItems)
            {
                if (itemSO == null) continue;
                var data = itemSO.ToCardData();
                data.isEternal = true;
                // Añadir al set correspondiente
                if (data.isPassive) p.passiveItems.Add(data);
                else p.activeItems.Add(data);
                // Notificar para refrescar UI de items (PlayerBoardDisplay escucha OnCardPlayed)
                OnCardPlayed?.Invoke(p, data);
            }
        }
    }

    private CharacterDataSO FindCharacterSO(CharacterType type)
    {
        var all = Resources.FindObjectsOfTypeAll<CharacterDataSO>();
        foreach (var c in all)
        {
            if (c != null && c.characterType == type) return c;
        }
        return null;
    }

    #region Shop Logic

    /// <summary>
    /// Inicializa la tienda revelando N cartas del mazo de tesoros.
    /// </summary>
    public void InitializeShop(int slots)
    {
        int clamped = Mathf.Clamp(slots, 1, maxShopSlots);
        shopRevealed.Clear();
        for (int i = 0; i < clamped; i++)
        {
            var c = DrawTopTreasure();
            shopRevealed.Add(c);
        }
        OnShopUpdated?.Invoke(new List<CardData>(shopRevealed));
        Debug.Log($"[GameManager] Tienda inicializada con {clamped} slots");
    }

    /// <summary>
    /// Devuelve una copia de las cartas reveladas en la tienda.
    /// </summary>
    public List<CardData> GetShopCards()
    {
        return new List<CardData>(shopRevealed);
    }

    /// <summary>
    /// Intenta comprar una carta de la tienda en el slot indicado (costo 10 monedas).
    /// Reemplaza ese slot con la carta superior del mazo de tesoros.
    /// </summary>
    public bool TryBuyFromShopSlot(PlayerData player, int slotIndex, out string reason)
    {
        reason = null;
        if (player == null)
        {
            reason = "Jugador inválido"; return false;
        }
        if (!CanPerformAction(player, "Buy"))
        {
            reason = "No puedes comprar ahora"; return false;
        }
        if (slotIndex < 0 || slotIndex >= shopRevealed.Count)
        {
            reason = "Slot de tienda inválido"; return false;
        }
        var card = shopRevealed[slotIndex];
        if (card == null)
        {
            reason = "No hay carta en ese slot"; return false;
        }
        if (player.coins < 10)
        {
            reason = "No tienes suficientes monedas"; return false;
        }

        // Cobrar 10 monedas
        ChangePlayerCoins(player, -10);

        // Entregar carta al jugador (tesoro activo o pasivo)
        (card.isPassive ? player.passiveItems : player.activeItems).Add(card);

        // Notificar como si se jugara/obtuvo el tesoro para refrescar UIs
        OnCardPlayed?.Invoke(player, card);
        OnTreasurePurchased?.Invoke(player, card, slotIndex);

        // Reemplazar el slot con la cima del mazo
        var replacement = DrawTopTreasure();
        shopRevealed[slotIndex] = replacement;
        OnShopUpdated?.Invoke(new List<CardData>(shopRevealed));

        Debug.Log($"[GameManager] {player.playerName} compró '{card.cardName}' del slot {slotIndex}. Reemplazado por '{replacement?.cardName ?? "(vacío)"}'");
        return true;
    }

    /// <summary>
    /// Intenta comprar del mazo de tesoros (costo 10). No afecta la tienda revelada.
    /// </summary>
    public bool TryBuyFromTreasureDeck(PlayerData player, out CardData bought, out string reason)
    {
        bought = null; reason = null;
        if (player == null) { reason = "Jugador inválido"; return false; }
        if (!CanPerformAction(player, "Buy")) { reason = "No puedes comprar ahora"; return false; }
        if (player.coins < 10) { reason = "No tienes suficientes monedas"; return false; }

        var card = DrawTopTreasure();
        if (card == null) { reason = "No hay cartas en el mazo de tesoros"; return false; }

        ChangePlayerCoins(player, -10);
        (card.isPassive ? player.passiveItems : player.activeItems).Add(card);
        OnCardPlayed?.Invoke(player, card);
        OnTreasurePurchased?.Invoke(player, card, -1);
        bought = card;
        Debug.Log($"[GameManager] {player.playerName} compró del mazo '{card.cardName}'");
        return true;
    }

    /// <summary>
    /// Aumenta el número de slots de tienda hasta un máximo.
    /// Rellena los nuevos slots con cartas del mazo.
    /// </summary>
    public bool ExpandShop(int newSize)
    {
        int target = Mathf.Clamp(newSize, 1, maxShopSlots);
        if (target <= shopRevealed.Count) return false;
        int toAdd = target - shopRevealed.Count;
        for (int i = 0; i < toAdd; i++)
        {
            shopRevealed.Add(DrawTopTreasure());
        }
        OnShopUpdated?.Invoke(new List<CardData>(shopRevealed));
        Debug.Log($"[GameManager] Tienda expandida a {shopRevealed.Count} slots");
        return true;
    }

    private CardData DrawTopTreasure()
    {
        if (treasureDeck.Count == 0)
        {
            // Rebarajar desde descarte si existe
            if (treasureDiscard.Count > 0)
            {
                treasureDeck.AddRange(treasureDiscard);
                treasureDiscard.Clear();
                ShuffleDeck(treasureDeck);
                Debug.Log("[GameManager] Tesoro: mazo vacío, rebarajado desde descarte");
            }
        }
        if (treasureDeck.Count == 0) return null;
        var top = treasureDeck[treasureDeck.Count - 1];
        treasureDeck.RemoveAt(treasureDeck.Count - 1);
        return top;
    }

    #endregion

    private void EnsureAttackBudgetForAllPlayers(int defaultPerTurn = 1)
    {
        if (players == null) return;
        foreach (var p in players)
        {
            if (p == null) continue;
            if (!remainingAttacksByPlayer.ContainsKey(p.playerId))
                remainingAttacksByPlayer[p.playerId] = defaultPerTurn;
        }
    }

    /// <summary>
    /// Reconstruye la mano visual del jugador actual a partir de sus datos (para cartas iniciales)
    /// </summary>
    private void RebuildCurrentPlayerHandUI()
    {
        if (playerHandUI == null || cardPrefab == null)
        {
            Debug.LogWarning("[GameManager] No se puede reconstruir la mano: falta PlayerHandUI o CardPrefab");
            return;
        }

        var player = GetCurrentPlayer();
        if (player == null) return;

        // Limpiar cartas actuales en la UI
        for (int i = playerHandUI.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(playerHandUI.transform.GetChild(i).gameObject);
        }

        // Instanciar cada carta de la mano del jugador
        foreach (var card in player.hand)
        {
            GameObject cardGO = Instantiate(cardPrefab, playerHandUI.transform);
            CardUI cardUI = cardGO.GetComponent<CardUI>();
            if (cardUI != null)
            {
                // Usar sprites directos si están disponibles
                Sprite front = card.frontSprite;
                Sprite back = cardBackSprite != null ? cardBackSprite : card.backSprite;
                cardUI.SetCardData(card, front, back);
            }

            RectTransform rect = cardGO.GetComponent<RectTransform>();
            if (rect != null)
            {
                playerHandUI.AddCardToHand(rect);
            }
        }

        Debug.Log($"[GameManager] Mano inicial construida: {player.hand.Count} cartas");
    }

    /// <summary>
    /// Da a cada jugador sus cartas y monedas iniciales
    /// </summary>
    private void SetupStartingResources()
    {
        Debug.Log($"[GameManager] Dando recursos iniciales: {startingHandSize} cartas y {startingCoins} monedas por jugador");
        
        foreach (var player in players)
        {
            // Dar monedas iniciales
            player.coins = startingCoins;
            Debug.Log($"[GameManager] {player.playerName} recibe {startingCoins} monedas iniciales");
            
            // Robar cartas iniciales (sin animación)
            for (int i = 0; i < startingHandSize; i++)
            {
                CardData card = DrawCard(DeckType.Loot, player);
                if (card != null)
                {
                    Debug.Log($"[GameManager] {player.playerName} roba carta inicial: {card.cardName}");
                }
                else
                {
                    Debug.LogWarning($"[GameManager] No hay suficientes cartas en el mazo para {player.playerName}");
                    break;
                }
            }
            
            Debug.Log($"[GameManager] {player.playerName} comienza con {player.hand.Count} cartas y {player.coins}¢");
        }
    }

    /// <summary>
    /// Garantiza que la lista de jugadores exista, tenga al menos uno y con nombres por defecto si faltan
    /// </summary>
    private void EnsurePlayersInitialized()
    {
        if (players == null)
            players = new List<PlayerData>();

        // Si no hay jugadores configurados en el Inspector, crear uno por defecto
        if (players.Count == 0)
        {
            players.Add(new PlayerData(0, "Player 1"));
            // Para multiplayer local, puedes agregar más aquí o configurarlos desde el Inspector
            // players.Add(new PlayerData(1, "Player 2"));
        }

        // Rellenar elementos nulos y nombres vacíos
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] == null)
            {
                players[i] = new PlayerData(i, $"Player {i + 1}");
                continue;
            }

            if (string.IsNullOrWhiteSpace(players[i].playerName))
            {
                players[i].playerName = $"Player {i + 1}";
            }
        }
    }

    /// <summary>
    /// Crea cartas de prueba para desarrollo
    /// TODO: Reemplazar con ScriptableObjects
    /// </summary>
    private void CreateTestCards()
    {
        // Si hay configuraciones de mazos asignadas, úsalas
        if (lootDeckConfig != null)
        {
            LoadDeckFromConfiguration(lootDeckConfig, ref lootDeck);
            Debug.Log($"[GameManager] Mazo de Loot cargado desde configuración: {lootDeck.Count} cartas");
        }
        else
        {
            // Fallback: crear cartas de prueba hardcodeadas
            Debug.LogWarning("[GameManager] No hay configuración de mazo de Loot, usando cartas de prueba hardcodeadas");
            for (int i = 0; i < 20; i++)
            {
                CardData card = new CardData(
                    id: i,
                    name: $"Test Card {i + 1}",
                    type: CardType.Loot,
                    spritePath: $"Cards/Front/Loot/card{i % 4}"
                );
                lootDeck.Add(card);
            }
        }

        if (treasureDeckConfig != null)
        {
            LoadDeckFromConfiguration(treasureDeckConfig, ref treasureDeck);
            Debug.Log($"[GameManager] Mazo de Treasure cargado: {treasureDeck.Count} cartas");
        }

        if (monsterDeckConfig != null)
        {
            LoadDeckFromConfiguration(monsterDeckConfig, ref monsterDeck);
            Debug.Log($"[GameManager] Mazo de Monster cargado: {monsterDeck.Count} cartas");
        }

        // Mezclar los mazos
        ShuffleDeck(lootDeck);
        ShuffleDeck(treasureDeck);
        ShuffleDeck(monsterDeck);
        
        Debug.Log($"[GameManager] Mazos creados y mezclados");
    }

    /// <summary>
    /// Carga un mazo desde una configuración de DeckConfiguration
    /// </summary>
    private void LoadDeckFromConfiguration(DeckConfiguration config, ref List<CardData> deck)
    {
        if (config == null)
        {
            Debug.LogWarning("[GameManager] Configuración de mazo es null");
            return;
        }

        deck.Clear();
        
        // Obtener todas las cartas expandidas (con duplicados)
        CardDataSO[] expandedCards = config.GetExpandedDeck();
        
        int skippedEternals = 0;
        foreach (var cardSO in expandedCards)
        {
            if (cardSO != null)
            {
                // Evitar que objetos eternos entren al mazo de tesoros del juego
                if (config.deckType == DeckType.Treasure && cardSO.isEternal)
                {
                    skippedEternals++;
                    continue;
                }

                // Convertir CardDataSO a CardData
                CardData cardData = cardSO.ToCardData();
                deck.Add(cardData);
            }
        }
        if (skippedEternals > 0)
        {
            Debug.Log($"[GameManager] Excluidas {skippedEternals} cartas eternas del mazo de Tesoros ({config.deckName})");
        }
        
        Debug.Log($"[GameManager] Cargadas {deck.Count} cartas desde {config.deckName}");
    }

    /// <summary>
    /// Mezcla un mazo usando el algoritmo Fisher-Yates
    /// </summary>
    private void ShuffleDeck(List<CardData> deck)
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            CardData temp = deck[i];
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

    #region Draw Card With Animation

    /// <summary>
    /// Intenta robar una carta con animación (llamado desde Input o UI)
    /// </summary>
    public void TryDrawCardWithAnimation()
    {
        if (isDrawing)
        {
            Debug.Log("[GameManager] Ya se está robando una carta, espera...");
            return;
        }

        if (playerHandUI == null || cardPrefab == null)
        {
            Debug.LogError("[GameManager] Falta asignar PlayerHandUI o CardPrefab en el Inspector!");
            return;
        }

        // Validar que sea la fase de robo
        if (currentPhase != GamePhase.Draw)
        {
            Debug.LogWarning($"[GameManager] No puedes robar cartas durante la fase {currentPhase}!");
            return;
        }

    // Robar carta del mazo
        PlayerData currentPlayer = GetCurrentPlayer();
        CardData drawnCard = DrawCard(DeckType.Loot, currentPlayer);

        if (drawnCard == null)
        {
            Debug.Log("[GameManager] No hay más cartas en el mazo.");
            // Avanzar igualmente a fase de Acción para no atascar el flujo
            ChangePhase(GamePhase.Action);
            return;
        }

    // Pausar timer durante la animación de robo
    PauseTurnTimer();

    // Iniciar animación hacia la mano del jugador actual
        StartCoroutine(DrawCardAnimationRoutine(drawnCard, currentPlayer));

        // Cambiar a fase de acción después de robar
        ChangePhase(GamePhase.Action);
    }

    /// <summary>
    /// Obtiene la posición en pantalla del contenedor de mano del jugador en el board.
    /// Si no hay board, retorna null para usar el destino legacy.
    /// </summary>
    private Vector2? TryGetPlayerHandScreenPosition(PlayerData player)
    {
        if (player == null || UIRegistry.Instance == null) return null;
        
        if (UIRegistry.Instance.TryGetPlayerBoard(player.playerId, out var board) && board != null)
        {
            var handContainer = board.handFanContainer as RectTransform;
            if (handContainer != null)
            {
                Camera cam = null;
                var canvas = handContainer.GetComponentInParent<Canvas>();
                if (canvas != null) cam = canvas.worldCamera;
                if (cam == null) cam = Camera.main;
                
                Vector3 worldPos = handContainer.TransformPoint(handContainer.rect.center);
                return UnityEngine.RectTransformUtility.WorldToScreenPoint(cam, worldPos);
            }
        }
        
        return null;
    }

    /// <summary>
    /// Animación de robo de carta desde el centro de la mesa hacia la mano del jugador
    /// </summary>
    private IEnumerator DrawCardAnimationRoutine(CardData cardData, PlayerData targetPlayer = null)
    {
        isDrawing = true;
        
        // Si no se especifica jugador, usar el actual
        if (targetPlayer == null)
            targetPlayer = GetCurrentPlayer();

        // Asegurar capa de animación
        RectTransform layer = animationLayer;
        if (layer == null)
        {
            Canvas canvas = playerHandUI.GetComponentInParent<Canvas>();
            if (canvas != null)
                layer = canvas.transform as RectTransform;
        }
        if (layer == null)
        {
            Debug.LogWarning("[GameManager] No se encontró una capa para animación (Canvas).");
            isDrawing = false;
            // Asegurar reanudar el temporizador en caso de fallo temprano
            ResumeTurnTimer();
            yield break;
        }

        // Determinar tamaño de carta para la animación
        Vector2 size = animCardSize;
        RectTransform prefabRect = cardPrefab.GetComponent<RectTransform>();
        if (usePrefabSizeForAnim && prefabRect != null && prefabRect.sizeDelta != Vector2.zero)
        {
            size = prefabRect.sizeDelta * prefabSizeAnimScale;
        }

        // Crear objeto temporal para animación
        GameObject temp = new GameObject("DrawAnimCard", typeof(RectTransform), typeof(Image));
        RectTransform tempRect = temp.GetComponent<RectTransform>();
        tempRect.SetParent(layer, worldPositionStays: false);
        tempRect.anchorMin = new Vector2(0.5f, 0.5f);
        tempRect.anchorMax = new Vector2(0.5f, 0.5f);
        tempRect.pivot = new Vector2(0.5f, 0.5f);
        tempRect.sizeDelta = size;
        tempRect.anchoredPosition = Vector2.zero; // centro de la mesa

        Image tempImg = temp.GetComponent<Image>();
        tempImg.sprite = cardBackSprite;
        tempImg.raycastTarget = false;

        // Intentar obtener la posición de la mano del jugador en el board
        Vector2? handScreenPos = TryGetPlayerHandScreenPosition(targetPlayer);
        Vector2 targetLocalPos;
        
        if (handScreenPos.HasValue)
        {
            // Convertir posición de pantalla a local del layer de animación
            var canvas = layer.GetComponentInParent<Canvas>();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, handScreenPos.Value, canvas?.worldCamera, out targetLocalPos))
            {
                // Animar hacia la posición de la mano en el board
                yield return tempRect.DOAnchorPos(targetLocalPos, slideDuration).SetEase(slideEase).WaitForCompletion();
            }
            else
            {
                // Fallback: fuera de cámara por abajo
                float targetY = -(layer.rect.height * 0.5f + size.y);
                yield return tempRect.DOAnchorPosY(targetY, slideDuration).SetEase(slideEase).WaitForCompletion();
            }
        }
        else
        {
            // Fallback: fuera de cámara por abajo (comportamiento original para HUD legacy)
            float targetY = -(layer.rect.height * 0.5f + size.y);
            yield return tempRect.DOAnchorPosY(targetY, slideDuration).SetEase(slideEase).WaitForCompletion();
        }

        Destroy(temp);

        // Obtener sprite frontal: primero intentar Resources path, si no usar referencia directa
        Sprite cardFrontSprite = null;
        if (!string.IsNullOrEmpty(cardData.frontSpritePath))
        {
            cardFrontSprite = Resources.Load<Sprite>(cardData.frontSpritePath);
            if (cardFrontSprite == null)
            {
                Debug.LogWarning($"[GameManager] No se pudo cargar sprite desde path: '{cardData.frontSpritePath}'. Usando referencia directa si existe.");
            }
        }
        if (cardFrontSprite == null && cardData.frontSprite != null)
        {
            cardFrontSprite = cardData.frontSprite;
        }

        // Instanciar la carta real en la mano del jugador
        GameObject cardGO = Instantiate(cardPrefab, playerHandUI.transform);

        // Configurar la carta con CardData
        CardUI cardUI = cardGO.GetComponent<CardUI>();
        if (cardUI != null)
        {
            cardUI.SetCardData(cardData, cardFrontSprite, cardBackSprite);
        }
        else
        {
            Debug.LogWarning("[GameManager] El prefab no tiene componente CardUI!");
        }

        // Añadir a la mano con animación
        RectTransform newCardRect = cardGO.GetComponent<RectTransform>();
        if (newCardRect != null)
        {
            // efecto de aparición sutil
            Vector3 originalScale = newCardRect.localScale;
            newCardRect.localScale = originalScale * appearScale;

            playerHandUI.AddCardToHand(newCardRect);

            // pequeño pop-in
            newCardRect.DOScale(originalScale, appearDuration).SetEase(Ease.OutQuad);
        }

        isDrawing = false;
        // Reanudar timer tras terminar animación de robo
        ResumeTurnTimer();
        // Seguridad: limpiar pausas anidadas si alguna quedó colgada
        if (turnTimerPauseCount > 0) ResetTurnTimerPause();
    }

    /// <summary>
    /// Añade visualmente una carta a la mano del jugador actual (sin animación de robo completa).
    /// Útil para recompensas (loot/treasure) fuera de la fase de Draw.
    /// </summary>
    public void AddCardToCurrentHandUI(CardData cardData)
    {
        if (playerHandUI == null || cardPrefab == null || cardData == null)
        {
            Debug.LogWarning("[GameManager] No se pudo agregar carta a la mano (falta PlayerHandUI/CardPrefab o cardData null)");
            return;
        }

        // Obtener sprite frontal: intentar por path primero, luego referencia directa
        Sprite cardFrontSprite = null;
        if (!string.IsNullOrEmpty(cardData.frontSpritePath))
        {
            cardFrontSprite = Resources.Load<Sprite>(cardData.frontSpritePath);
            if (cardFrontSprite == null && cardData.frontSprite != null)
            {
                cardFrontSprite = cardData.frontSprite;
            }
        }
        else if (cardData.frontSprite != null)
        {
            cardFrontSprite = cardData.frontSprite;
        }

        // Instanciar carta en la mano
        GameObject cardGO = Instantiate(cardPrefab, playerHandUI.transform);
        CardUI cardUI = cardGO.GetComponent<CardUI>();
        if (cardUI != null)
        {
            cardUI.SetCardData(cardData, cardFrontSprite, cardBackSprite);
        }
        else
        {
            Debug.LogWarning("[GameManager] El prefab no tiene componente CardUI!");
        }

        // Añadir con pequeño pop
        RectTransform rect = cardGO.GetComponent<RectTransform>();
        if (rect != null)
        {
            Vector3 original = rect.localScale;
            rect.localScale = original * appearScale;
            playerHandUI.AddCardToHand(rect);
            rect.DOScale(original, appearDuration).SetEase(Ease.OutQuad);
        }
    }

    /// <summary>
    /// Anima una carta desde una posición de pantalla hasta la pila de descarte correspondiente a su tipo.
    /// Usado cuando un jugador usa una carta de Loot (vuela al descarte con su sprite frontal).
    /// </summary>
    public void AnimateCardToDiscard(CardData card, Vector2 startScreenPos)
    {
        if (card == null) return;
        StartCoroutine(AnimateCardToDiscardRoutine(card, startScreenPos));
    }

    private IEnumerator AnimateCardToDiscardRoutine(CardData card, Vector2 startScreenPos)
    {
        // Capa de animación
        RectTransform layer = animationLayer;
        if (layer == null)
        {
            if (playerHandUI != null)
            {
                var handCanvas = playerHandUI.GetComponentInParent<Canvas>();
                if (handCanvas != null) layer = handCanvas.transform as RectTransform;
            }
        }
        if (layer == null)
        {
            yield break;
        }

        // Tamaño de carta para la animación
        Vector2 size = animCardSize;
        RectTransform prefabRect = cardPrefab != null ? cardPrefab.GetComponent<RectTransform>() : null;
        if (usePrefabSizeForAnim && prefabRect != null && prefabRect.sizeDelta != Vector2.zero)
        {
            size = prefabRect.sizeDelta * prefabSizeAnimScale;
        }

        // Crear objeto temporal
        GameObject temp = new GameObject("DiscardAnimCard", typeof(RectTransform), typeof(Image));
        RectTransform tempRect = temp.GetComponent<RectTransform>();
        tempRect.SetParent(layer, worldPositionStays: false);
        tempRect.anchorMin = new Vector2(0.5f, 0.5f);
        tempRect.anchorMax = new Vector2(0.5f, 0.5f);
        tempRect.pivot = new Vector2(0.5f, 0.5f);
        tempRect.sizeDelta = size;

        // Posición inicial (convertir desde pantalla a local del layer)
        var canvas = layer.GetComponentInParent<Canvas>();
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, startScreenPos, canvas?.worldCamera, out var startLocal))
        {
            tempRect.anchoredPosition = startLocal;
        }
        else
        {
            tempRect.anchoredPosition = Vector2.zero;
        }

        // Sprite de la carta (frontal si está disponible)
        Image tempImg = temp.GetComponent<Image>();
        Sprite front = null;
        if (!string.IsNullOrEmpty(card.frontSpritePath))
        {
            front = Resources.Load<Sprite>(card.frontSpritePath);
        }
        if (front == null) front = card.frontSprite;
        if (front == null) front = cardBackSprite;
        tempImg.sprite = front;
        tempImg.raycastTarget = false;

        // Posición objetivo: centro de la pila de descarte del tipo de carta
        Vector2 targetLocal = Vector2.zero;
        var targetScreen = TryGetDiscardPileScreenCenter(card.cardType);
        if (targetScreen.HasValue)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, targetScreen.Value, canvas?.worldCamera, out targetLocal))
            {
                targetLocal = Vector2.zero;
            }
        }
        else
        {
            // Fallback: mover fuera de la pantalla arriba-derecha
            targetLocal = new Vector2(layer.rect.width * 0.6f, layer.rect.height * 0.6f);
        }

        yield return tempRect.DOAnchorPos(targetLocal, slideDuration).SetEase(slideEase).WaitForCompletion();
    Destroy(temp);

    // Reanudar visuals de descarte y refrescar la carta superior ahora que la animación llegó
    ResumeDiscardVisuals(card.cardType, refreshNow: true);

        // Si estamos esperando animación de monedas por Loot, lanzarla ahora que la carta llegó al descarte
        if (card.cardType == CardType.Loot && pendingCoinWaitForDiscard)
        {
            if (CoinGainAnimator.Instance != null && pendingCoinPlayer != null && pendingCoinAmount != 0)
            {
                Vector2 startPos = pendingCoinAnimStartScreenPos ?? targetScreen ?? WorldToScreenPoint(Vector3.zero);
                CoinGainAnimator.Instance.PlayCoinGain(pendingCoinPlayer, pendingCoinAmount, startPos);
            }
            // limpiar flags
            pendingCoinWaitForDiscard = false;
            pendingCoinPlayer = null;
            pendingCoinAmount = 0;
            pendingCoinAnimStartScreenPos = null;
            pendingCoinShouldDelay = false;
        }
    }

    private void SuppressDiscardVisuals(CardType type)
    {
        if (_discardVisualSuppressCount.TryGetValue(type, out var c))
            _discardVisualSuppressCount[type] = c + 1;
        else
            _discardVisualSuppressCount[type] = 1;
    }

    private void ResumeDiscardVisuals(CardType type, bool refreshNow)
    {
        if (_discardVisualSuppressCount.TryGetValue(type, out var c))
        {
            c = Mathf.Max(0, c - 1);
            if (c == 0) _discardVisualSuppressCount.Remove(type); else _discardVisualSuppressCount[type] = c;
        }
        if (refreshNow && UIRegistry.Instance != null)
        {
            if (UIRegistry.Instance.TryGetDiscardPile(type, out var pile))
            {
                pile.RefreshTop();
            }
            else
            {
                // Fallback: si aún no está registrado en UIRegistry (orden de inicialización), buscar en escena
                var allPiles = GameObject.FindObjectsOfType<DiscardPileUI>(includeInactive: true);
                foreach (var p in allPiles)
                {
                    if (p != null && p.isDiscardPile && p.pileType == type)
                    {
                        p.RefreshTop();
                    }
                }
            }
        }
    }

    public bool ShouldSuppressDiscardVisuals(CardType type)
    {
        return _discardVisualSuppressCount.TryGetValue(type, out var c) && c > 0;
    }

    /// <summary>
    /// Añade una carta a la mano del jugador actual con animación de robo (para recompensas).
    /// No cambia la fase del turno. Si ya hay otra animación de robo en curso, hace un fallback sin animación.
    /// </summary>
    public void AddCardToCurrentHandUIWithAnimation(CardData cardData)
    {
        if (cardData == null)
        {
            Debug.LogWarning("[GameManager] AddCardToCurrentHandUIWithAnimation: cardData es NULL");
            return;
        }
        if (playerHandUI == null || cardPrefab == null)
        {
            Debug.LogWarning("[GameManager] No se pudo animar carta a la mano (falta PlayerHandUI/CardPrefab)");
            AddCardToCurrentHandUI(cardData);
            return;
        }
        if (isDrawing)
        {
            // Evitar animaciones concurrentes
            AddCardToCurrentHandUI(cardData);
            return;
        }

        PauseTurnTimer();
        StartCoroutine(DrawCardAnimationRoutine(cardData, GetCurrentPlayer()));
    }

    /// <summary>
    /// Reconstruye la mano visual para el jugador dado (para simulación local de multijugador).
    /// Elimina las cartas actuales del PlayerHandUI y crea nuevas basadas en player.hand.
    /// </summary>
    public void RebuildHandUIForPlayer(PlayerData player)
    {
        if (playerHandUI == null || cardPrefab == null || player == null) return;

        // Limpiar UI actual
        playerHandUI.ResetHandVisual();
        playerHandUI.BindToPlayer(player);

        // Reconstruir cada carta de la mano
        foreach (var cardData in player.hand)
        {
            if (cardData == null) continue;

            // Obtener sprites frontal y trasero
            Sprite cardFrontSprite = null;
            if (!string.IsNullOrEmpty(cardData.frontSpritePath))
            {
                cardFrontSprite = Resources.Load<Sprite>(cardData.frontSpritePath);
                if (cardFrontSprite == null && cardData.frontSprite != null)
                {
                    cardFrontSprite = cardData.frontSprite;
                }
            }
            else if (cardData.frontSprite != null)
            {
                cardFrontSprite = cardData.frontSprite;
            }

            GameObject cardGO = Instantiate(cardPrefab, playerHandUI.transform);
            CardUI cardUI = cardGO.GetComponent<CardUI>();
            if (cardUI != null)
            {
                cardUI.SetCardData(cardData, cardFrontSprite, cardBackSprite);
            }

            RectTransform rect = cardGO.GetComponent<RectTransform>();
            if (rect != null)
            {
                playerHandUI.AddCardToHand(rect);
            }
        }

        // Organizar visualmente
        playerHandUI.ArrangeCards();
    }

    #endregion

    #region Turn System

    /// <summary>
    /// Inicia el turno de un jugador
    /// </summary>
    public void StartPlayerTurn(int playerIndex)
    {
        Debug.Log($"[GameManager] ===== INICIANDO TURNO DEL JUGADOR {playerIndex} =====");
        // Habilitar anuncios de turno a partir del primer StartPlayerTurn
        if (!isReadyForTurnAnnouncements)
        {
            isReadyForTurnAnnouncements = true;
            Debug.Log("[GameManager] isReadyForTurnAnnouncements = true");
        }
        currentPlayerIndex = playerIndex;
        
        // Cerrar cualquier preview de carta que haya quedado abierta del turno anterior
        if (CardPreviewUI.Instance != null)
        {
            CardPreviewUI.Instance.HidePreview();
        }

        ChangePhase(GamePhase.Start);
        
        // Efectos de inicio de turno
        ProcessStartTurnEffects();
        
        Debug.Log($"[GameManager] Disparando evento OnPlayerTurnChanged para jugador {playerIndex}");
        Debug.Log($"[GameManager] TurnAnnouncerUI != null: {turnAnnouncerUI != null}");
        
        // Disparar evento de cambio de turno (esto mostrará "Turno de <Jugador>")
        OnPlayerTurnChanged?.Invoke(playerIndex);

        // Iniciar/Resetear temporizador de turno
        if (useTurnTimer)
        {
            turnTimeRemaining = Mathf.Max(0f, turnDurationSeconds);
            turnTimerActive = true;
            OnTurnTimerUpdated?.Invoke(turnTimeRemaining, turnDurationSeconds);
            Debug.Log($"[GameManager] Timer iniciado: {turnTimeRemaining}s");
        }
        else
        {
            turnTimerActive = false;
            Debug.Log("[GameManager] Timer desactivado");
        }
        
        // Esperar a que termine el anuncio del turno antes de pasar a Draw
        if (turnAnnouncerUI != null)
        {
            Debug.Log("[GameManager] Esperando animación del TurnAnnouncer...");
            System.Action onTurnAnnounceComplete = null;
            onTurnAnnounceComplete = () =>
            {
                Debug.Log("[GameManager] Animación de turno completada, cambiando a Draw");
                turnAnnouncerUI.OnAnimationComplete -= onTurnAnnounceComplete;
                ChangePhase(GamePhase.Draw);
            };
            turnAnnouncerUI.OnAnimationComplete += onTurnAnnounceComplete;
        }
        else
        {
            Debug.LogWarning("[GameManager] TurnAnnouncerUI es NULL! Pasando a Draw inmediatamente");
            // Sin UI, pasar inmediatamente a Draw
            ChangePhase(GamePhase.Draw);
        }

        // Resetear ataques disponibles del jugador activo
        EnsureAttackBudgetForAllPlayers(1);
        remainingAttacksByPlayer[currentPlayerIndex] = 1;

        // Sanear estado de combate por si quedó residual
        isInCombat = false;
        combatPlayer = null;
        combatSlot = null;
        combatAwaitingRoll = false;
        combatIsRolling = false;
        combatRollRequested = false;

        // Simulación: actualizar la mano local para que muestre la del jugador actual
        if (simulateLocalHandFollowsCurrentTurn && playerHandUI != null)
        {
            var current = GetCurrentPlayer();
            if (current != null)
            {
                RebuildHandUIForPlayer(current);
            }
        }
    }

    /// <summary>
    /// Cambia la fase del turno
    /// </summary>
    public void ChangePhase(GamePhase newPhase)
    {
        currentPhase = newPhase;
        OnPhaseChanged?.Invoke(newPhase);
        
        Debug.Log($"[GameManager] Phase changed to: {newPhase}");

        // Automatizar la fase de robo: al entrar a Draw, mostrar "Loteando..." y luego robar
        if (newPhase == GamePhase.Draw)
        {
            if (turnAnnouncerUI != null)
            {
                // Mostrar "Loteando..." y esperar que termine antes de robar
                turnAnnouncerUI.PlayMessage("Loteando...");
                
                // Suscribirse temporalmente al evento de fin de animación
                System.Action onDrawAnimComplete = null;
                onDrawAnimComplete = () =>
                {
                    turnAnnouncerUI.OnAnimationComplete -= onDrawAnimComplete;
                    TryDrawCardWithAnimation();
                };
                turnAnnouncerUI.OnAnimationComplete += onDrawAnimComplete;
            }
            else
            {
                // Sin UI, robar inmediatamente
                TryDrawCardWithAnimation();
            }
        }
    }

    /// <summary>
    /// Termina el turno del jugador actual
    /// </summary>
    public void EndCurrentTurn()
    {
        // No permitir terminar turno en medio de un combate
        if (isInCombat)
        {
            Debug.LogWarning("[GameManager] No puedes terminar el turno durante un combate activo");
            return;
        }
        // Detener temporizador del turno actual
        turnTimerActive = false;
        ChangePhase(GamePhase.End);
        ProcessEndTurnEffects();
        
        // Pasar al siguiente jugador
        int nextPlayer = (currentPlayerIndex + 1) % players.Count;
        StartPlayerTurn(nextPlayer);
    }

    #endregion

    #region Card Actions

    #region Dice

    /// <summary>
    /// Lanza un dado con 'sides' caras. Pausa el temporizador durante la animación si existe UI.
    /// Invoca el callback y el evento OnDiceRolled con el resultado (1..sides).
    /// </summary>
    public void RollDice(int sides = 6, Action<int> onResult = null)
    {
        sides = Mathf.Max(2, sides);

        if (diceRollerUI != null)
        {
            PauseTurnTimer();
            diceRollerUI.RollDice(sides, (value) =>
            {
                ResumeTurnTimer();
                OnDiceRolled?.Invoke(value);
                onResult?.Invoke(value);
            });
        }
        else
        {
            // Sin UI, devolver un valor aleatorio instantáneo
            int value = UnityEngine.Random.Range(1, sides + 1);
            OnDiceRolled?.Invoke(value);
            onResult?.Invoke(value);
        }
    }

    #endregion

    /// <summary>
    /// Inicia el flujo de "atacar el tope del mazo de Monstruos": revela la carta superior y pide elegir un slot para colocarla encima.
    /// El combate comenzará contra ese monstruo overlay.
    /// </summary>
    public bool BeginAttackDeckTop(PlayerData player)
    {
        if (player == null) return false;
        // Validar que puede atacar
        if (!CanPerformAction(player, "Attack"))
        {
            Debug.LogWarning("[GameManager] No puedes atacar en este momento");
            return false;
        }
        if (awaitingDeckOverlayPlacement)
        {
            Debug.LogWarning("[GameManager] Ya estás colocando un monstruo del mazo");
            return false;
        }

        // Revelar y tomar la carta superior del mazo de Monstruos
        CardData top = DrawCard(DeckType.Monster, null);
        if (top == null)
        {
            Debug.LogWarning("[GameManager] El mazo de Monstruos está vacío");
            return false;
        }

        pendingDeckOverlayMonster = top;
        pendingDeckOverlayPlayer = player;
        awaitingDeckOverlayPlacement = true;

        // Mensaje persistente + overlay de selección
        if (turnAnnouncerUI != null)
        {
            turnAnnouncerUI.ShowPersistentMessage("Elige un slot de monstruo para colocar encima");
        }
        SetSelectionOverlay(true);
        PauseTurnTimer();

        return true;
    }

    public bool IsAwaitingDeckOverlayPlacement()
    {
        return awaitingDeckOverlayPlacement && pendingDeckOverlayMonster != null && pendingDeckOverlayPlayer != null;
    }

    /// <summary>
    /// Confirmación al hacer clic en un slot: coloca el overlay, muestra preview y empieza el combate contra ese monstruo.
    /// </summary>
    public void ConfirmDeckOverlayPlacement(MonsterSlot slot)
    {
        if (!IsAwaitingDeckOverlayPlacement() || slot == null) return;

        // Colocar overlay visual y lógico
        if (MonsterSlotManager.Instance != null)
        {
            MonsterSlotManager.Instance.PlaceOverlayMonster(slot, pendingDeckOverlayMonster);
        }
        else
        {
            Debug.LogError("[GameManager] No hay MonsterSlotManager para colocar overlay");
        }

        // Limpiar estado de selección
        awaitingDeckOverlayPlacement = false;
        var player = pendingDeckOverlayPlayer;
        pendingDeckOverlayPlayer = null;
        pendingDeckOverlayMonster = null;
        SetSelectionOverlay(false);
        if (turnAnnouncerUI != null)
        {
            turnAnnouncerUI.HideMessage();
        }
        ResumeTurnTimer();

        // Mostrar preview del monstruo overlay brevemente antes de iniciar combate
        if (CardPreviewUI.Instance != null)
        {
            CardPreviewUI.Instance.ShowMonster(slot);
        }

        // Esperar a que termine la animación de spawn (aproximadamente 0.5s) + tiempo para ver preview
        StartCoroutine(DelayedCombatStart(player, slot, 1.2f));
    }

    private System.Collections.IEnumerator DelayedCombatStart(PlayerData player, MonsterSlot slot, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Cerrar preview automáticamente antes de iniciar combate
        if (CardPreviewUI.Instance != null)
        {
            CardPreviewUI.Instance.HidePreview();
        }

        // Iniciar combate contra el slot (esto consumirá el ataque restante del jugador)
        bool started = BeginCombat(player, slot);
        if (started)
        {
            // Lanzar inmediatamente la primera tirada de dado
            RequestCombatRollNext(slot);
        }
    }

    /// <summary>
    /// Roba una carta de un mazo específico
    /// </summary>
    public CardData DrawCard(DeckType deckType, PlayerData player = null)
    {
        List<CardData> deck = GetDeck(deckType);
        
        if (deck.Count == 0)
        {
            Debug.LogWarning($"[GameManager] {deckType} deck is empty!");
            // TODO: Mezclar pila de descarte de vuelta al mazo
            return null;
        }

        // Robar la primera carta
        CardData drawnCard = deck[0];
        deck.RemoveAt(0);
        
        // Si es para un jugador, agregarlo a su mano
        if (player != null)
        {
            player.hand.Add(drawnCard);
            OnCardDrawn?.Invoke(player, drawnCard);
            
            Debug.Log($"[GameManager] {player.playerName} drew: {drawnCard.cardName}");
        }
        
        return drawnCard;
    }

    /// <summary>
    /// Descarta una carta de la mano del jugador
    /// </summary>
    public void DiscardCard(PlayerData player, CardData card)
    {
        if (!player.hand.Contains(card))
        {
            Debug.LogWarning($"[GameManager] Card not in player's hand!");
            return;
        }

        player.hand.Remove(card);
        
        // Agregar a la pila de descarte correspondiente
        List<CardData> discardPile = GetDiscardPile(card.cardType);
        // Si es Loot desde la mano, suprimir visuals para que la pila se actualice cuando llegue la animación
        if (card.cardType == CardType.Loot)
        {
            SuppressDiscardVisuals(card.cardType);
        }
        discardPile.Add(card);
        
        OnCardDiscarded?.Invoke(player, card);
        
        Debug.Log($"[GameManager] {player.playerName} discarded: {card.cardName}");
    }

    /// <summary>
    /// Juega una carta desde la mano
    /// </summary>
    public bool PlayCard(PlayerData player, CardData card)
    {
        // Validar que es el turno del jugador
        if (players[currentPlayerIndex] != player)
        {
            Debug.LogWarning($"[GameManager] Not {player.playerName}'s turn!");
            return false;
        }

        // Validar fase correcta
        if (currentPhase != GamePhase.Action)
        {
            Debug.LogWarning($"[GameManager] Cannot play cards during {currentPhase} phase!");
            return false;
        }

        if (!player.hand.Contains(card))
        {
            Debug.LogWarning($"[GameManager] Card not in player's hand!");
            return false;
        }

        // Procesar según tipo de carta
        bool success = ProcessCardPlay(player, card);
        
        if (success)
        {
            player.hand.Remove(card);
            OnCardPlayed?.Invoke(player, card);
            Debug.Log($"[GameManager] {player.playerName} played: {card.cardName}");
        }
        
        return success;
    }

    /// <summary>
    /// Flujo para jugar una carta desde la UI: si requiere objetivo, inicia selección y resuelve al elegir.
    /// </summary>
    public void RequestPlayCard(PlayerData player, CardData card)
    {
        if (player == null || card == null) return;

        // Validaciones básicas
        if (players[currentPlayerIndex] != player)
        {
            Debug.LogWarning($"[GameManager] No es el turno de {player.playerName}");
            return;
        }
        if (currentPhase != GamePhase.Action)
        {
            Debug.LogWarning($"[GameManager] No se pueden jugar cartas en fase {currentPhase}");
            return;
        }
        if (!player.hand.Contains(card))
        {
            Debug.LogWarning("[GameManager] La carta no está en la mano del jugador");
            return;
        }

        // Determinar si algún efecto requiere objetivo
        var so = card.sourceScriptableObject;
        bool requiresTarget = false;
        System.Collections.Generic.HashSet<TargetType> allowed = new System.Collections.Generic.HashSet<TargetType>();
        if (so != null && so.effects != null)
        {
            foreach (var e in so.effects)
            {
                if (e == null) continue;
                if (e.RequiresTarget)
                {
                    requiresTarget = true;
                    foreach (var t in e.AllowedTargets) allowed.Add(t);
                }
            }
        }

        if (!requiresTarget)
        {
            // Jugar inmediatamente (sin objetivo)
            PlayCard(player, card);
            
            // Cerrar preview después de jugar la carta
            if (CardPreviewUI.Instance != null)
            {
                CardPreviewUI.Instance.HidePreview();
            }
            return;
        }

        // Iniciar modo targeting
        if (TargetingManager.Instance == null)
        {
            Debug.LogError("[GameManager] No hay TargetingManager en escena para seleccionar objetivo");
            return;
        }

        Debug.Log("[GameManager] Esta carta requiere objetivo. Iniciando selección...");
        TargetingManager.Instance.BeginTargeting(allowed, (selection) =>
        {
            // Ejecutar efectos con el objetivo seleccionado y finalizar el juego de carta
            ResolvePlayCardWithTarget(player, card, selection);
            
            // Cerrar preview después de resolver con objetivo
            if (CardPreviewUI.Instance != null)
            {
                CardPreviewUI.Instance.HidePreview();
            }
        });
    }

    private void ResolvePlayCardWithTarget(PlayerData player, CardData card, TargetSelection selection)
    {
        if (card.sourceScriptableObject != null)
        {
            card.sourceScriptableObject.ExecuteEffects(player, this, selection);
        }

        // Mover la carta desde la mano a su destino (descartar si single-use)
        player.hand.Remove(card);
        if (card.cardType == CardType.Loot && card.isSingleUse)
        {
            // Suprimir visual del descarte antes de notificar, para que no aparezca hasta que llegue la animación
            SuppressDiscardVisuals(card.cardType);
            lootDiscard.Add(card);
            // Notificar descarte para actualizar UI de pilas
            OnCardDiscarded?.Invoke(player, card);
        }
        else if (card.cardType == CardType.Treasure)
        {
            (card.isPassive ? player.passiveItems : player.activeItems).Add(card);
        }

        OnCardPlayed?.Invoke(player, card);
        Debug.Log($"[GameManager] {player.playerName} jugó {card.cardName} con objetivo {selection?.targetType}");
    }

    #endregion

    #region Validation

    /// <summary>
    /// Valida si una acción es permitida
    /// </summary>
    public bool CanPerformAction(PlayerData player, string actionType)
    {
        // No es el turno del jugador
        if (players[currentPlayerIndex] != player)
            return false;

        switch (actionType)
        {
            case "DrawCard":
                return currentPhase == GamePhase.Draw;
            
            case "PlayCard":
                // Se permite jugar cartas durante el combate (loot/activaciones), por lo que no bloqueamos por isInCombat
                return currentPhase == GamePhase.Action;
            
            case "Attack":
                // Debe estar en Acción, no estar en combate y tener ataques restantes
                return currentPhase == GamePhase.Action && !isInCombat && GetRemainingAttacks(player) > 0;
            
            case "EndTurn":
                    return currentPhase == GamePhase.Action && !isInCombat;
            
            case "Buy":
                    // Placeholder de compra: prohibido durante combate
                    return currentPhase == GamePhase.Action && !isInCombat;
            
            default:
                return false;
        }
    }

    #endregion

    #region Internal Helpers

    private void ProcessStartTurnEffects()
    {
        // TODO: Activar efectos pasivos de inicio de turno
        Debug.Log($"[GameManager] Processing start turn effects for {GetCurrentPlayer().playerName}");

        // Recargar objetos ACTIVOS del jugador actual al inicio de su turno
        var player = GetCurrentPlayer();
        if (player != null)
        {
            foreach (var item in player.activeItems)
            {
                if (item == null) continue;
                if (item.cardType == CardType.Treasure && !item.isPassive)
                {
                    item.isReady = true;
                }
            }
        }
    }

    private void ProcessEndTurnEffects()
    {
        PlayerData player = GetCurrentPlayer();
        
        // Verificar límite de mano
        while (player.hand.Count > maxHandSize)
        {
            // TODO: Pedir al jugador que descarte
            Debug.LogWarning($"[GameManager] {player.playerName} exceeded hand limit! Must discard.");
        }
        
        // TODO: Otros efectos de fin de turno
    }

    private bool ProcessCardPlay(PlayerData player, CardData card)
    {
        switch (card.cardType)
        {
            case CardType.Loot:
                // Ejecutar efectos de la carta de Loot
                Debug.Log($"[GameManager] Ejecutando carta de Loot: {card.cardName}");
                
                if (card.sourceScriptableObject != null)
                {
                    // Ejecutar efectos desde el ScriptableObject
                    card.sourceScriptableObject.ExecuteEffects(player, this);
                }
                else
                {
                    Debug.LogWarning($"[GameManager] La carta {card.cardName} no tiene ScriptableObject asociado (carta hardcodeada)");
                }
                
                // Descartar la carta si es de un solo uso
                if (card.isSingleUse)
                {
                    // Suprimir visual del descarte antes de notificar, para que no aparezca hasta que llegue la animación
                    SuppressDiscardVisuals(card.cardType);
                    lootDiscard.Add(card);
                    // Notificar descarte para actualizar UI
                    OnCardDiscarded?.Invoke(player, card);
                }
                else
                {
                    // Si no es de un solo uso, va a objetos activos
                    player.activeItems.Add(card);
                }
                return true;
            
            case CardType.Treasure:
                // Los tesoros van a objetos activos/pasivos
                if (card.isPassive)
                {
                    player.passiveItems.Add(card);
                    Debug.Log($"[GameManager] {card.cardName} agregado a objetos pasivos");
                }
                else
                {
                    player.activeItems.Add(card);
                    Debug.Log($"[GameManager] {card.cardName} agregado a objetos activos");
                }
                return true;
            
            default:
                Debug.LogWarning($"[GameManager] Cannot play card type: {card.cardType}");
                return false;
        }
    }

    private List<CardData> GetDeck(DeckType deckType)
    {
        return deckType switch
        {
            DeckType.Loot => lootDeck,
            DeckType.Treasure => treasureDeck,
            DeckType.Monster => monsterDeck,
            _ => lootDeck
        };
    }

    private List<CardData> GetDiscardPile(CardType cardType)
    {
        return cardType switch
        {
            CardType.Loot => lootDiscard,
            CardType.Treasure => treasureDiscard,
            CardType.Monster => monsterDiscard,
            CardType.Boss => monsterDiscard,
            _ => lootDiscard
        };
    }

    /// <summary>
    /// Devuelve la carta superior de la pila de descarte indicada (o null si está vacía)
    /// </summary>
    public CardData GetTopDiscard(CardType cardType)
    {
        var pile = GetDiscardPile(cardType);
        if (pile == null || pile.Count == 0) return null;
        return pile[pile.Count - 1];
    }

    /// <summary>
    /// Descarta un monstruo derrotado a la pila de monstruos (si no otorgó almas)
    /// </summary>
    public void DiscardMonster(CardData monsterCard)
    {
        if (monsterCard == null) return;
        monsterDiscard.Add(monsterCard);
        // Notificar para actualizar UI de descarte de monstruos
        OnCardDiscarded?.Invoke(null, monsterCard);
        Debug.Log($"[GameManager] Monstruo '{monsterCard.cardName}' descartado");
    }

    #endregion

    #region Public Accessors

    public PlayerData GetCurrentPlayer()
    {
        if (players == null || players.Count == 0)
            return null;
        if (currentPlayerIndex < 0 || currentPlayerIndex >= players.Count)
            return null;
        return players[currentPlayerIndex];
    }

    public PlayerData GetPlayer(int index)
    {
        return index >= 0 && index < players.Count ? players[index] : null;
    }

    public GamePhase GetCurrentPhase()
    {
        return currentPhase;
    }

    public List<PlayerData> GetAllPlayers()
    {
        return new List<PlayerData>(players);
    }

    #endregion

    #region Active Item Usage

    /// <summary>
    /// Verifica si un objeto activo puede activarse en este momento.
    /// Regla: Los objetos ACTIVOS pueden usarse en turnos de cualquier jugador, siempre que estén listos (isReady).
    /// </summary>
    public bool CanActivateItem(PlayerData owner, CardData item)
    {
        if (owner == null || item == null) return false;
        // Debe ser un tesoro activo del propietario
        bool owned = owner.activeItems.Contains(item);
        if (!owned) return false;
        if (item.cardType != CardType.Treasure || item.isPassive) return false;

        // Debe estar cargado/listo
        if (!item.isReady) return false;

        // Por regla, se permite en cualquier turno; no bloqueamos por turno o fase aquí.
        // Si alguna carta quisiera restringirlo, podría modelarse con efectos/condiciones futuras.
        return true;
    }

    /// <summary>
    /// Intenta activar un objeto activo inmediatamente (solo efectos sin objetivo).
    /// Marca el objeto como agotado (isReady=false) si se ejecuta con éxito.
    /// </summary>
    public bool UseActiveItem(PlayerData owner, CardData item)
    {
        if (!CanActivateItem(owner, item))
        {
            Debug.LogWarning("[Items] No se puede activar el objeto ahora (no es del jugador, no es activo o no está listo)");
            return false;
        }

        // Ejecutar efectos definidos en el ScriptableObject
        if (item.sourceScriptableObject != null)
        {
            item.sourceScriptableObject.ExecuteEffects(owner, this);
        }
        else
        {
            Debug.LogWarning($"[Items] {item.cardName} no tiene efectos asociados (SO null)");
        }

        // Agotar hasta recarga en inicio del turno del propietario
        item.isReady = false;
        OnCardPlayed?.Invoke(owner, item);
        Debug.Log($"[Items] {owner.playerName} activó: {item.cardName}");
        return true;
    }

    /// <summary>
    /// Variante que soporta objetivos: inicia selección si algún efecto lo requiere, y al confirmar ejecuta y agota el objeto.
    /// </summary>
    public void RequestUseActiveItem(PlayerData owner, CardData item)
    {
        if (!CanActivateItem(owner, item))
        {
            Debug.LogWarning("[Items] No se puede activar este objeto en este momento");
            return;
        }

        // Determinar si algún efecto requiere objetivo
        var so = item.sourceScriptableObject;
        bool requiresTarget = false;
        System.Collections.Generic.HashSet<TargetType> allowed = new System.Collections.Generic.HashSet<TargetType>();
        if (so != null && so.effects != null)
        {
            foreach (var e in so.effects)
            {
                if (e == null) continue;
                if (e.RequiresTarget)
                {
                    requiresTarget = true;
                    foreach (var t in e.AllowedTargets) allowed.Add(t);
                }
            }
        }

        if (!requiresTarget)
        {
            UseActiveItem(owner, item);
            return;
        }

        if (TargetingManager.Instance == null)
        {
            Debug.LogError("[Items] No TargetingManager available to select a target for item activation");
            return;
        }

        TargetingManager.Instance.BeginTargeting(allowed, (selection) =>
        {
            // Ejecutar con objetivo
            if (!CanActivateItem(owner, item)) return; // revalidar estado
            if (item.sourceScriptableObject != null)
            {
                item.sourceScriptableObject.ExecuteEffects(owner, this, selection);
            }
            item.isReady = false;
            OnCardPlayed?.Invoke(owner, item);
            Debug.Log($"[Items] {owner.playerName} activó: {item.cardName} con objetivo {selection?.targetType}");
        });
    }

    #endregion

    #region Player Stats Management

    /// <summary>
    /// Modifica la vida de un jugador
    /// </summary>
    public void ChangePlayerHealth(PlayerData player, int amount)
    {
        if (player == null) return;

        int oldHealth = player.health;
        player.health += amount;

        // Asegurar que la vida no sea negativa
        if (player.health < 0)
            player.health = 0;

        Debug.Log($"[GameManager] {player.playerName} vida: {oldHealth} → {player.health}");

        // Disparar eventos
        OnPlayerHealthChanged?.Invoke(player, player.health);

        if (amount < 0)
            OnPlayerDamaged?.Invoke(player);

        // Verificar muerte
        if (player.health <= 0)
        {
            OnPlayerDied?.Invoke(player);
            Debug.Log($"[GameManager] {player.playerName} ha muerto!");
        }
    }

    /// <summary>
    /// Modifica las monedas de un jugador
    /// </summary>
    public void ChangePlayerCoins(PlayerData player, int amount)
    {
        if (player == null) return;

        int oldCoins = player.coins;
        player.coins += amount;

        // Asegurar que las monedas no sean negativas
        if (player.coins < 0)
            player.coins = 0;

        Debug.Log($"[GameManager] {player.playerName} monedas: {oldCoins} → {player.coins}");

        // Si se ganan monedas, preparar animación visual hacia el icono del jugador
        bool gained = amount > 0;
        if (gained && UIRegistry.Instance != null && UIRegistry.Instance.TryGetPlayerStats(player.playerId, out var stats) && stats != null)
        {
            // Suprimir el pulso inmediato para que el pulso ocurra al llegar las monedas animadas
            stats.SuppressNextCoinsPulse();
        }

        OnPlayerCoinsChanged?.Invoke(player, player.coins);

        // Disparar animación de monedas: si estamos esperando a que llegue una carta de Loot al descarte,
        // posponer la animación hasta que termine AnimateCardToDiscardRoutine
        if (gained && CoinGainAnimator.Instance != null)
        {
            if (pendingCoinWaitForDiscard)
            {
                // Guardar datos y no lanzar todavía
                pendingCoinPlayer = player;
                pendingCoinAmount = amount;
            }
            else
            {
                // Si se requiere un pequeño delay (p.ej. para que la pila de descarte actualice su top card), usar corrutina
                if (pendingCoinShouldDelay)
                {
                    StartCoroutine(PlayCoinAnimDelayed(player, amount, pendingCoinAnimStartScreenPos, 0.08f));
                }
                else
                {
                    if (pendingCoinAnimStartScreenPos.HasValue)
                    {
                        CoinGainAnimator.Instance.PlayCoinGain(player, amount, pendingCoinAnimStartScreenPos.Value);
                    }
                    else
                    {
                        CoinGainAnimator.Instance.PlayCoinGain(player, amount);
                    }
                }
            }
        }
        // Limpiar origen/flags pendientes
        pendingCoinAnimStartScreenPos = null;
        pendingCoinShouldDelay = false;
    }

    /// <summary>
    /// Otorga monedas y anima el origen desde la pila de descarte de Loot (si está registrada).
    /// </summary>
    public void ChangePlayerCoinsFromLoot(PlayerData player, int amount)
    {
        pendingCoinAnimStartScreenPos = TryGetDiscardPileScreenCenter(CardType.Loot);
        // Esperar explícitamente a que la carta usada llegue al descarte antes de lanzar las monedas
        pendingCoinWaitForDiscard = true;
        pendingCoinPlayer = player;
        pendingCoinAmount = amount;
        ChangePlayerCoins(player, amount);
    }

    /// <summary>
    /// Otorga monedas y anima el origen desde el monstruo (slot) indicado.
    /// </summary>
    public void ChangePlayerCoinsFromMonster(PlayerData player, int amount, MonsterSlot slot)
    {
        pendingCoinAnimStartScreenPos = TryGetMonsterSlotScreenCenter(slot);
        ChangePlayerCoins(player, amount);
    }

    private Vector2? TryGetDiscardPileScreenCenter(CardType type)
    {
        if (UIRegistry.Instance != null && UIRegistry.Instance.TryGetDiscardPile(type, out var pile) && pile != null)
        {
            // Priorizar UI Image
            if (pile.uiImage != null)
            {
                var rect = pile.uiImage.rectTransform;
                return WorldToScreenPoint(rect.TransformPoint(rect.rect.center));
            }
            // Si es worldRenderer, usar su bounds center
            if (pile.worldRenderer != null)
            {
                return WorldToScreenPoint(pile.worldRenderer.bounds.center);
            }
        }
        // Fallback: buscar en escena una DiscardPileUI marcada como pila de descarte del tipo solicitado
        var allPiles = GameObject.FindObjectsOfType<DiscardPileUI>(includeInactive: true);
        foreach (var p in allPiles)
        {
            if (p != null && p.isDiscardPile && p.pileType == type)
            {
                if (p.uiImage != null)
                {
                    var rect = p.uiImage.rectTransform;
                    return WorldToScreenPoint(rect.TransformPoint(rect.rect.center));
                }
                if (p.worldRenderer != null)
                {
                    return WorldToScreenPoint(p.worldRenderer.bounds.center);
                }
            }
        }
        return null;
    }

    private Vector2? TryGetMonsterSlotScreenCenter(MonsterSlot slot)
    {
        if (slot == null) return null;
        // Intentar obtener el objeto visual del monstruo
        var obj = slot.GetMonsterObject();
        Vector3 world = obj != null ? obj.transform.position : slot.transform.position;
        return WorldToScreenPoint(world);
    }

    private Vector2 WorldToScreenPoint(Vector3 world)
    {
        Camera cam = null;
        // Intentar obtener la cámara del Canvas del animationLayer
        if (animationLayer != null)
        {
            var canvas = animationLayer.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                cam = canvas.worldCamera;
            }
        }
        if (cam == null) cam = Camera.main;
        return (Vector2)UnityEngine.RectTransformUtility.WorldToScreenPoint(cam, world);
    }

    private IEnumerator PlayCoinAnimDelayed(PlayerData player, int amount, Vector2? startPos, float delay)
    {
        // Esperar final del frame + delay opcional
        yield return new WaitForEndOfFrame();
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (CoinGainAnimator.Instance == null) yield break;
        if (startPos.HasValue)
            CoinGainAnimator.Instance.PlayCoinGain(player, amount, startPos.Value);
        else
            CoinGainAnimator.Instance.PlayCoinGain(player, amount);
    }

    /// <summary>
    /// Agrega un alma al jugador (carta de tipo Soul)
    /// </summary>
    public void CollectSoul(PlayerData player, CardData soul)
    {
        if (player == null || soul == null) return;

        if (soul.cardType != CardType.Soul)
        {
            Debug.LogWarning($"[GameManager] {soul.cardName} no es un alma!");
            return;
        }

        player.activeItems.Add(soul);
        OnSoulCollected?.Invoke(player, soul);

        Debug.Log($"[GameManager] {player.playerName} recolectó alma: {soul.cardName}");

        // Verificar condición de victoria
        int soulCount = player.activeItems.FindAll(c => c.cardType == CardType.Soul).Count;
        if (soulCount >= soulsToWin)
        {
            OnPlayerWon?.Invoke(player);
            Debug.Log($"[GameManager] ¡{player.playerName} ha ganado con {soulCount} almas!");
        }
    }

    /// <summary>
    /// Cura al jugador (agrega vida)
    /// </summary>
    public void HealPlayer(PlayerData player, int amount)
    {
        ChangePlayerHealth(player, Mathf.Abs(amount));
    }

    /// <summary>
    /// Daña al jugador (resta vida)
    /// </summary>
    public void DamagePlayer(PlayerData player, int amount)
    {
        ChangePlayerHealth(player, -Mathf.Abs(amount));
    }

    /// <summary>
    /// Agrega una cantidad de almas al jugador y notifica a la UI
    /// </summary>
    public void AddSouls(PlayerData player, int count)
    {
        if (player == null || count <= 0) return;

        int previousSouls = player.souls;
        player.souls += count;
        Debug.Log($"[GameManager] AddSouls: {player.playerName} recibió {count} almas. Total: {previousSouls} -> {player.souls}");
        
        for (int i = 0; i < count; i++)
        {
            // No tenemos una CardData específica de alma en este flujo
            Debug.Log($"[GameManager] Invocando OnSoulCollected para {player.playerName} (evento {i+1}/{count})");
            OnSoulCollected?.Invoke(player, null);
        }
    }

    /// <summary>
    /// Obtiene el número de almas necesarias para ganar
    /// </summary>
    public int GetSoulsToWin()
    {
        return soulsToWin;
    }

    /// <summary>
    /// Obtiene el límite máximo de cartas en mano
    /// </summary>
    public int GetMaxHandSize()
    {
        return maxHandSize;
    }

    #region Combat System (Looped until death)

    /// <summary>
    /// Inicia un combate contra un monstruo y bloquea otras acciones hasta que uno muera.
    /// Reduce en 1 el número de ataques disponibles del jugador en este turno.
    /// </summary>
    public bool BeginCombat(PlayerData player, MonsterSlot slot)
    {
        if (player == null || slot == null || !slot.HasMonster()) return false;
        if (isInCombat)
        {
            Debug.LogWarning("[GameManager] Ya hay un combate en curso");
            return false;
        }
        if (players[currentPlayerIndex] != player)
        {
            Debug.LogWarning("[GameManager] Solo el jugador activo puede iniciar combate");
            return false;
        }
        if (GetRemainingAttacks(player) <= 0)
        {
            Debug.LogWarning("[GameManager] No tienes ataques restantes este turno");
            return false;
        }

        // Consumir un ataque
        remainingAttacksByPlayer[player.playerId] = Mathf.Max(0, GetRemainingAttacks(player) - 1);

        isInCombat = true;
        combatPlayer = player;
        combatSlot = slot;
        combatAwaitingRoll = true;      // permitir que el jugador decida cuándo tirar
        combatIsRolling = false;
        combatRollRequested = false;
        PauseTurnTimer();
        OnCombatStarted?.Invoke(player, slot);

        // Resaltar visualmente el monstruo en combate
        if (combatSlot != null)
        {
            ApplyCombatHighlight(combatSlot, true);
        }

        StartCoroutine(CombatRoutine());
        return true;
    }

    private System.Collections.IEnumerator CombatRoutine()
    {
        // Bucle: esperar a que el jugador solicite la tirada y resolver, hasta que uno muera
        while (isInCombat && combatPlayer != null && combatSlot != null && combatSlot.HasMonster())
        {
            // Capturar referencia al monstruo actual para detectar muerte/cambio
            var monsterBefore = combatSlot.CurrentMonster;
            if (monsterBefore == null)
                break;

            // Esperar a que el jugador pida tirar (permite jugar Loot/activar objetos antes de la tirada)
            yield return new WaitUntil(() => combatRollRequested && !combatIsRolling);
            combatRollRequested = false;
            combatAwaitingRoll = false;
            combatIsRolling = true;

            int? roll = null;
            RollDice(6, (value) => { roll = value; });
            yield return new WaitUntil(() => roll.HasValue);

            int rollValue = roll.Value;
            Debug.Log($"[Combat] {combatPlayer.playerName} tiró {rollValue} contra {monsterBefore.cardName} (req {monsterBefore.diceRequirement}+)");

            if (rollValue >= monsterBefore.diceRequirement)
            {
                // Hit al monstruo - la animación se maneja en DamageMonster()
                MonsterSlotManager.Instance.DamageMonster(combatSlot, combatPlayer.attackDamage);
                yield return new WaitForSeconds(0.4f);

                // Si el monstruo murió, el slot se vaciará/rellenará en DefeatMonster()
                if (!combatSlot.HasMonster() || combatSlot.CurrentMonster != monsterBefore)
                {
                    EndCombat(monsterDefeated: true, playerDefeated: false);
                    yield break;
                }
            }
            else
            {
                // Fallo, el jugador recibe daño
                combatSlot.PlayAttackAnimation();
                yield return new WaitForSeconds(0.3f);
                ChangePlayerHealth(combatPlayer, -monsterBefore.attackDamage);
                if (combatPlayer.health <= 0)
                {
                    // Penalizaciones de muerte (con selección interactiva)
                    if (CardPreviewUI.Instance != null)
                    {
                        CardPreviewUI.Instance.HidePreview();
                    }
                    yield return StartCoroutine(ApplyDeathPenaltiesRoutine(combatPlayer));
                    EndCombat(monsterDefeated: false, playerDefeated: true);
                    // Tras pagar penalizaciones por muerte, pasar el turno al siguiente jugador
                    EndCurrentTurn();
                    yield break;
                }
            }

            // Triggers de combate (curas/daño adicional)
            MonsterSlotManager.Instance.ProcessCombatRoll(combatSlot, combatPlayer, rollValue);

            // Verificar muertes post-trigger
            if (combatPlayer.health <= 0)
            {
                if (CardPreviewUI.Instance != null)
                {
                    CardPreviewUI.Instance.HidePreview();
                }
                yield return StartCoroutine(ApplyDeathPenaltiesRoutine(combatPlayer));
                EndCombat(monsterDefeated: false, playerDefeated: true);
                // Tras pagar penalizaciones por muerte, pasar el turno al siguiente jugador
                EndCurrentTurn();
                yield break;
            }
            if (!combatSlot.HasMonster() || combatSlot.CurrentMonster != monsterBefore)
            {
                EndCombat(monsterDefeated: true, playerDefeated: false);
                yield break;
            }

            // Permitir siguiente tirada a solicitud del jugador
            combatIsRolling = false;
            combatAwaitingRoll = true;
            // Pequeña pausa de respiro
            yield return new WaitForSeconds(0.1f);
        }

        // Seguridad: si salimos del bucle, terminar combate si sigue marcado
        if (isInCombat)
        {
            EndCombat(monsterDefeated: false, playerDefeated: false);
        }
    }

    private void EndCombat(bool monsterDefeated, bool playerDefeated)
    {
        var endedPlayer = combatPlayer;
        var endedSlot = combatSlot;
        isInCombat = false;
        combatPlayer = null;
        combatSlot = null;
        combatAwaitingRoll = false;
        combatIsRolling = false;
        combatRollRequested = false;
        ResumeTurnTimer();
        OnCombatEnded?.Invoke(endedPlayer, endedSlot);
        Debug.Log($"[Combat] Termina combate. MonsterDefeated={monsterDefeated}, PlayerDefeated={playerDefeated}");

        // Quitar highlight del monstruo (si sigue existiendo)
        if (endedSlot != null)
        {
            ApplyCombatHighlight(endedSlot, false);
        }

        // Si el monstruo fue derrotado, cerrar cualquier preview abierta
        if (monsterDefeated && CardPreviewUI.Instance != null)
        {
            CardPreviewUI.Instance.HidePreview();
        }
    }

    /// <summary>
    /// Penalizaciones de muerte: -1 moneda (si tiene), descartar 1 Loot de la mano (si tiene),
    /// descartar 1 objeto controlado no eterno (si tiene). Luego reaparece con vida al máximo.
    /// </summary>
    private System.Collections.IEnumerator ApplyDeathPenaltiesRoutine(PlayerData player)
    {
        if (player == null) yield break;

        Debug.Log($"[Death] Aplicando penalizaciones a {player.playerName}");

        // 1) Perder 1 moneda si tiene
        if (player.coins > 0)
        {
            ChangePlayerCoins(player, -1);
        }

        // 2) Descartar 1 carta de Loot de la mano (selección del jugador si es posible)
        CardData lootToDiscard = null;
        var lootOptions = new System.Collections.Generic.List<CardData>();
        for (int i = 0; i < player.hand.Count; i++)
        {
            var c = player.hand[i];
            if (c != null && c.cardType == CardType.Loot)
                lootOptions.Add(c);
        }
        if (lootOptions.Count == 1)
        {
            // Auto-descartar si hay exactamente una carta
            DiscardCard(player, lootOptions[0]);
        }
        else if (lootOptions.Count > 1)
        {
            bool selectionCompleted = false;
            BeginLootDiscardSelection(player, (selected) =>
            {
                lootToDiscard = selected;
                selectionCompleted = true;
            });
            // Esperar a que el jugador escoja una carta de Loot de su mano, con timeout de 30s
            float lootTimeout = 30f;
            float lootElapsed = 0f;
            while (!selectionCompleted && awaitingLootDiscardSelection && lootElapsed < lootTimeout)
            {
                lootElapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            // Si se agotó el tiempo, elegir una carta al azar
            if (!selectionCompleted && awaitingLootDiscardSelection)
            {
                lootToDiscard = lootOptions[UnityEngine.Random.Range(0, lootOptions.Count)];
                Debug.Log($"[Death] Timeout 30s en selección de Loot. Se descarta al azar: {lootToDiscard.cardName}");
            }

            if (lootToDiscard == null)
            {
                // Fallback: última carta de Loot si no se pudo seleccionar
                lootToDiscard = lootOptions[lootOptions.Count - 1];
            }
            if (lootToDiscard != null)
            {
                DiscardCard(player, lootToDiscard);
            }
            // Asegurar overlay desactivado tras finalizar selección o fallback
            SetSelectionOverlay(false);
            if (turnAnnouncerUI != null)
            {
                turnAnnouncerUI.HideMessage();
            }
        }

        // 3) Descartar 1 objeto controlado no eterno (activo o pasivo) - selección interactiva
        // Construir lista de opciones válidas (excluyendo almas y eternos)
        var validItems = new System.Collections.Generic.List<CardData>();
        foreach (var c in player.activeItems)
        {
            if (c != null && !c.isEternal && c.cardType != CardType.Soul) validItems.Add(c);
        }
        foreach (var c in player.passiveItems)
        {
            if (c != null && !c.isEternal && c.cardType != CardType.Soul) validItems.Add(c);
        }
        if (validItems.Count == 1)
        {
            // Auto-descartar si hay exactamente un objeto válido
            var item = validItems[0];
            // Quitar de activos o pasivos
            if (!player.activeItems.Remove(item))
                player.passiveItems.Remove(item);
            var pile = GetDiscardPile(item.cardType);
            pile.Add(item);
            OnCardDiscarded?.Invoke(player, item);
            Debug.Log($"[Death] {player.playerName} descarta objeto (auto): {item.cardName}");
        }
        else if (validItems.Count > 1)
        {
            CardData itemToDiscard = null;
            bool itemSelectionCompleted = false;
            BeginItemDiscardSelection(player, (selected) =>
            {
                itemToDiscard = selected;
                itemSelectionCompleted = true;
            });
            // Esperar a que el jugador confirme un objeto no eterno a descartar, con timeout de 30s
            float itemTimeout = 30f;
            float itemElapsed = 0f;
            while (!itemSelectionCompleted && awaitingItemDiscardSelection && itemElapsed < itemTimeout)
            {
                itemElapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            // Si se agotó el tiempo, elegir un objeto al azar
            if (!itemSelectionCompleted && awaitingItemDiscardSelection)
            {
                itemToDiscard = validItems[UnityEngine.Random.Range(0, validItems.Count)];
                // Quitar de su lista para simular confirmación
                if (!player.activeItems.Remove(itemToDiscard))
                    player.passiveItems.Remove(itemToDiscard);
                Debug.Log($"[Death] Timeout 30s en selección de item. Se descarta al azar: {itemToDiscard.cardName}");
            }

            if (itemToDiscard == null)
            {
                // Fallback: último elegible (prioriza activos)
                for (int i = player.activeItems.Count - 1; i >= 0 && itemToDiscard == null; i--)
                {
                    var c = player.activeItems[i];
                    if (c != null && !c.isEternal && c.cardType != CardType.Soul)
                    {
                        itemToDiscard = c;
                        player.activeItems.RemoveAt(i);
                    }
                }
                for (int i = player.passiveItems.Count - 1; i >= 0 && itemToDiscard == null; i--)
                {
                    var c = player.passiveItems[i];
                    if (c != null && !c.isEternal && c.cardType != CardType.Soul)
                    {
                        itemToDiscard = c;
                        player.passiveItems.RemoveAt(i);
                    }
                }
            }

            if (itemToDiscard != null)
            {
                // Si no se eliminó en fallback, quitar de su lista
                if (player.activeItems.Contains(itemToDiscard)) player.activeItems.Remove(itemToDiscard);
                else if (player.passiveItems.Contains(itemToDiscard)) player.passiveItems.Remove(itemToDiscard);

                // Enviar al descarte correspondiente
                var pile = GetDiscardPile(itemToDiscard.cardType);
                pile.Add(itemToDiscard);
                OnCardDiscarded?.Invoke(player, itemToDiscard);
                Debug.Log($"[Death] {player.playerName} descarta objeto: {itemToDiscard.cardName}");
            }
            // Asegurar overlay desactivado tras finalizar selección o fallback
            SetSelectionOverlay(false);
            if (turnAnnouncerUI != null)
            {
                turnAnnouncerUI.HideMessage();
            }
        }

        // Reaparecer con vida al máximo
        int toHeal = Mathf.Max(0, player.maxHealth - player.health);
        if (toHeal > 0)
        {
            HealPlayer(player, toHeal);
        }
    }

    /// <summary>
    /// Devuelve los ataques restantes del jugador en este turno.
    /// </summary>
    public int GetRemainingAttacks(PlayerData player)
    {
        if (player == null) return 0;
        if (!remainingAttacksByPlayer.TryGetValue(player.playerId, out int remaining))
            return 0;
        return Mathf.Max(0, remaining);
    }

    /// <summary>
    /// Concede ataques adicionales (p.ej. por carta o efecto de monstruo).
    /// </summary>
    public void GrantExtraAttack(PlayerData player, int count = 1)
    {
        if (player == null || count <= 0) return;
        int current = GetRemainingAttacks(player);
        remainingAttacksByPlayer[player.playerId] = current + count;
        Debug.Log($"[Combat] {player.playerName} gana {count} ataque(s) adicional(es). Restantes: {remainingAttacksByPlayer[player.playerId]}");
    }

    /// <summary>
    /// Solicita la siguiente tirada en el combate actual (p.ej. doble clic al monstruo en combate).
    /// </summary>
    public void RequestCombatRollNext(MonsterSlot slot)
    {
        if (!isInCombat || slot == null || slot != combatSlot) return;
        if (!combatAwaitingRoll || combatIsRolling) return;
        combatRollRequested = true;
    }

    /// <summary>
    /// Consulta si un slot es el monstruo actualmente en combate.
    /// </summary>
    public bool IsInCombatSlot(MonsterSlot slot)
    {
        return isInCombat && combatSlot == slot;
    }

    /// <summary>
    /// Obtiene el slot del monstruo en combate (si existe).
    /// </summary>
    public MonsterSlot GetCombatSlot()
    {
        return combatSlot;
    }

    // Visual helpers
    private void ApplyCombatHighlight(MonsterSlot slot, bool on)
    {
        if (slot == null) return;
        var obj = slot.GetMonsterObject();
        if (obj == null) return;
        var sr = obj.GetComponent<SpriteRenderer>();
        if (sr == null) sr = obj.GetComponentInChildren<SpriteRenderer>();
        if (sr == null) return;
        if (on)
        {
            var tint = new Color(0.8f, 0.9f, 1f, sr.color.a);
            sr.DOKill();
            sr.DOColor(tint, 0.15f).SetEase(Ease.OutQuad);
        }
        else
        {
            // Best-effort to approximate original by removing bluish tint
            var restore = new Color(1f, 1f, 1f, sr.color.a);
            sr.DOKill();
            sr.DOColor(restore, 0.15f).SetEase(Ease.OutQuad);
        }
    }

    private void SetSelectionOverlay(bool on)
    {
        // Preferir el overlay del DiceRoller para unificar estilo (fondo oscuro con fade)
        if (diceRollerUI != null)
        {
            diceRollerUI.SetOverlay(on);
            return;
        }

        // Fallback: usar una Image asignada desde el inspector
        if (selectionDimOverlay != null)
        {
            selectionDimOverlay.gameObject.SetActive(on);
        }
    }

    /// <summary>
    /// Exponer la capa de animación para otros componentes de UI (p.ej. animaciones de monedas).
    /// </summary>
    public RectTransform GetAnimationLayerRect()
    {
        return animationLayer;
    }

    #endregion

    #region Death Penalty Selection Hooks

    /// <summary>
    /// Inicia el modo de selección de descarte de Loot: el jugador debe hacer clic en una carta de Loot en su mano.
    /// </summary>
    private void BeginLootDiscardSelection(PlayerData player, System.Action<CardData> onSelected)
    {
        awaitingLootDiscardSelection = true;
        onLootDiscardSelected = onSelected;
        lootSelectionPlayer = player;
        pendingLootSelectionCandidate = null;
        Debug.Log("[Death] Elige una carta de Loot de tu mano para descartar.");
        // Mostrar mensaje reutilizando el TurnAnnouncer si existe
        if (turnAnnouncerUI != null)
        {
            turnAnnouncerUI.ShowPersistentMessage("Descarta una carta");
        }
        // Activar overlay de enfoque
        SetSelectionOverlay(true);
    }

    /// <summary>
    /// Inicia el modo de selección de descarte de OBJETO no eterno (activos o pasivos).
    /// </summary>
    private void BeginItemDiscardSelection(PlayerData player, System.Action<CardData> onSelected)
    {
        awaitingItemDiscardSelection = true;
        onItemDiscardSelected = onSelected;
        itemSelectionPlayer = player;
        Debug.Log("[Death] Elige un objeto NO eterno para descartar.");
        if (turnAnnouncerUI != null)
        {
            turnAnnouncerUI.ShowPersistentMessage("Descarta un objeto no eterno");
        }
        SetSelectionOverlay(true);
    }

    /// <summary>
    /// Llamado por la UI de la mano cuando se hace clic en una carta. Si estamos en selección de descarte y la carta es Loot, la toma.
    /// </summary>
    public void NotifyHandCardClicked(CardData clicked)
    {
        if (!awaitingLootDiscardSelection || clicked == null) return;
        if (clicked.cardType != CardType.Loot) return; // Solo aceptamos Loot para esta selección
        if (lootSelectionPlayer == null) return;
        // Asegurar que la carta pertenece a la mano del jugador relevante
        if (!lootSelectionPlayer.hand.Contains(clicked)) return;

        // No confirmar inmediatamente: solo marcar la candidata para que el botón de la preview pueda confirmar
        pendingLootSelectionCandidate = clicked;
        Debug.Log($"[Death] Candidata seleccionada (pendiente de confirmar): {clicked.cardName}");
    }

    /// <summary>
    /// Notificación de click sobre un objeto activo/pasivo durante selección de descarte de item.
    /// Nota: La UI correspondiente debe llamar a GameManager.ConfirmItemDiscardSelection(card) desde su botón de confirmar
    /// y abrir un preview que muestre el botón "Descartar"; este método sirve de placeholder por si más adelante
    /// se integran clics directos sobre paneles de items.
    /// </summary>
    public void NotifyItemClicked(CardData clicked)
    {
        // Si en el futuro tenemos UI clicable para items, podríamos marcar una candidata aquí.
        // Por ahora no hacemos nada para evitar confirmar sin preview.
        if (!awaitingItemDiscardSelection || clicked == null) return;
        if (itemSelectionPlayer == null) return;
        if (clicked.isEternal || clicked.cardType == CardType.Soul) return;
        // La confirmación se hará en ConfirmItemDiscardSelection.
    }

    /// <summary>
    /// Confirma el descarte de la carta seleccionada durante el modo de selección de Loot.
    /// </summary>
    public void ConfirmLootDiscardSelection(CardData card)
    {
        if (!awaitingLootDiscardSelection || card == null) return;
        if (lootSelectionPlayer == null) return;
        if (card.cardType != CardType.Loot) return;
        if (!lootSelectionPlayer.hand.Contains(card)) return;

        var cb = onLootDiscardSelected;
        onLootDiscardSelected = null;
        awaitingLootDiscardSelection = false;
        pendingLootSelectionCandidate = null;
        var owner = lootSelectionPlayer;
        lootSelectionPlayer = null;
        cb?.Invoke(card);
        Debug.Log($"[Death] Confirmado descarte: {card.cardName}");
        // Desactivar overlay de enfoque
        SetSelectionOverlay(false);
        if (turnAnnouncerUI != null)
        {
            turnAnnouncerUI.HideMessage();
        }
    }

    /// <summary>
    /// Confirma el descarte del objeto seleccionado (no eterno) durante la selección de penalización por muerte.
    /// Debe llamarse desde el botón "Descartar" del preview correspondiente a un item (cuando exista esa UI).
    /// </summary>
    public void ConfirmItemDiscardSelection(CardData item)
    {
        if (!awaitingItemDiscardSelection || item == null) return;
        if (itemSelectionPlayer == null) return;
        if (item.isEternal || item.cardType == CardType.Soul) return;
        // Verificar que el item pertenezca al jugador (activo o pasivo)
        bool owned = itemSelectionPlayer.activeItems.Contains(item) || itemSelectionPlayer.passiveItems.Contains(item);
        if (!owned) return;

        var cb = onItemDiscardSelected;
        onItemDiscardSelected = null;
        awaitingItemDiscardSelection = false;
        var owner = itemSelectionPlayer;
        itemSelectionPlayer = null;
        cb?.Invoke(item);
        Debug.Log($"[Death] Confirmado descarte de objeto: {item.cardName}");
        SetSelectionOverlay(false);
        if (turnAnnouncerUI != null)
        {
            turnAnnouncerUI.HideMessage();
        }
    }

    public bool IsAwaitingItemDiscardSelection() => awaitingItemDiscardSelection;
    public bool IsValidItemDiscardCandidate(CardData item)
    {
        if (!awaitingItemDiscardSelection || itemSelectionPlayer == null || item == null) return false;
        if (item.isEternal || item.cardType == CardType.Soul) return false;
        return itemSelectionPlayer.activeItems.Contains(item) || itemSelectionPlayer.passiveItems.Contains(item);
    }

    public bool IsAwaitingLootDiscardSelection() => awaitingLootDiscardSelection;
    public PlayerData GetLootSelectionPlayer() => lootSelectionPlayer;
    public bool IsValidLootDiscardCandidate(CardData card)
    {
        return awaitingLootDiscardSelection && lootSelectionPlayer != null && card != null && card.cardType == CardType.Loot && lootSelectionPlayer.hand.Contains(card);
    }

    #endregion

    #endregion
}
