using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

/// <summary>
/// Muestra las estadísticas del jugador: Vida, Monedas y Almas
/// Se actualiza automáticamente suscribiéndose a eventos del GameManager
/// </summary>
public class PlayerStatsUI : MonoBehaviour
{
    [Header("Personaje (Opcional)")]
    [Tooltip("Imagen donde se mostrará el icono del personaje seleccionado")]
    public Image characterIconImage;

    [Header("Referencias de Iconos")]
    [Tooltip("Sprite de corazón para la vida")]
    public Image healthIcon;
    [Tooltip("Sprite de moneda")]
    public Image coinsIcon;
    [Tooltip("Sprite de alma")]
    public Image soulsIcon;
    [Tooltip("Sprite de daño/ataque (opcional)")]
    public Image attackIcon;
    [Tooltip("Sprite de cartas de Loot (opcional)")]
    public Image lootCountIcon;
    [Tooltip("Sprite de tesoros (opcional)")]
    public Image treasureCountIcon;

    [Header("Textos de Cantidad")]
    [Tooltip("Texto que muestra la cantidad de vida")]
    public TextMeshProUGUI healthText;
    [Tooltip("Texto que muestra la cantidad de monedas")]
    public TextMeshProUGUI coinsText;
    [Tooltip("Texto que muestra la cantidad de almas")]
    public TextMeshProUGUI soulsText;
    [Tooltip("Texto que muestra el daño de ataque (opcional)")]
    public TextMeshProUGUI attackText;
    [Tooltip("Texto que muestra la cantidad de cartas de Loot (opcional)")]
    public TextMeshProUGUI lootCountText;
    [Tooltip("Texto que muestra la cantidad de tesoros (opcional)")]
    public TextMeshProUGUI treasureCountText;

    [Header("Configuración")]
    [Tooltip("Índice del jugador a mostrar (0 = Player 1, 1 = Player 2, etc.)")]
    public int playerIndex = 0;
    [Tooltip("Mostrar daño de ataque")]
    public bool showAttackDamage = true;
    [Tooltip("Mostrar cantidad de cartas de Loot en mano")]
    public bool showLootCount = true;
    [Tooltip("Mostrar cantidad de tesoros controlados")]
    public bool showTreasureCount = true;

    [Header("Canvas Sorting (Avanzado)")]
    [Tooltip("Ajustar automáticamente el sorting order del Canvas para renderizarse encima de sprites del mundo")]
    public bool autoAdjustCanvasSorting = true;
    [Tooltip("Sorting order mínimo del Canvas (solo si autoAdjustCanvasSorting está activo)")]
    public int minCanvasSortingOrder = 100;

    [Header("Animación (Opcional)")]
    [Tooltip("Escala al recibir daño o ganar recursos")]
    public bool animateOnChange = true;
    public float punchScale = 1.2f;
    public float punchDuration = 0.3f;

    [Header("Feedback de Daño (Opcional)")]
    [Tooltip("Imagen de fondo del panel para parpadear en rojo al recibir daño (opcional)")]
    public Image panelBackground;
    [Tooltip("Color del flash de daño (incluye alpha)")]
    public Color damageFlashColor = new Color(1f, 0f, 0f, 0.35f);
    [Tooltip("Duración del flash de daño")]
    public float damageFlashDuration = 0.25f;
    [Tooltip("Intensidad del sacudón (escala multiplicadora)")]
    public float damageShakeScale = 1.05f;
    [Tooltip("Duración del sacudón del panel")]
    public float damageShakeDuration = 0.18f;

    private PlayerData currentPlayer;
    private Coroutine healthPunchCoroutine;
    private Coroutine coinsPunchCoroutine;
    private Coroutine soulsPunchCoroutine;
    private Coroutine attackPunchCoroutine;
    private Coroutine lootCountPunchCoroutine;
    private Coroutine treasureCountPunchCoroutine;
    private Coroutine characterIconPunchCoroutine;
    private Coroutine damageFxCoroutine;
    
    // Escalas originales de cada icono
    private Vector3 healthIconOriginalScale;
    private Vector3 coinsIconOriginalScale;
    private Vector3 soulsIconOriginalScale;
    private Vector3 attackIconOriginalScale;
    private Vector3 lootCountIconOriginalScale;
    private Vector3 treasureCountIconOriginalScale;
    private Vector3 characterIconOriginalScale;
    private Color panelBackgroundOriginalColor;
    private RectTransform cachedRect;
    // Evitar doble pulso cuando hay animación especial de monedas
    private bool suppressNextCoinsPulse = false;

    void Awake()
    {
        // Asegurar que el Canvas padre tenga un sorting order alto para renderizarse encima de sprites del mundo
        if (autoAdjustCanvasSorting)
        {
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                // Si es Screen Space - Overlay, no necesita sorting order (siempre está encima)
                if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    // Ya está bien configurado, se renderiza siempre encima
                }
                else if (parentCanvas.renderMode == RenderMode.ScreenSpaceCamera || parentCanvas.renderMode == RenderMode.WorldSpace)
                {
                    // Necesita un sorting order alto para estar encima de los sprites de monstruos (10-20)
                    if (parentCanvas.sortingOrder < minCanvasSortingOrder)
                    {
                        parentCanvas.sortingOrder = minCanvasSortingOrder;
                        Debug.Log($"[PlayerStatsUI] Canvas sorting order ajustado a {minCanvasSortingOrder} para renderizarse encima de monstruos");
                    }
                }
            }
        }
    }

    void Start()
    {
        // Guardar las escalas originales de los iconos
        if (healthIcon != null)
            healthIconOriginalScale = healthIcon.transform.localScale;
        if (coinsIcon != null)
            coinsIconOriginalScale = coinsIcon.transform.localScale;
        if (soulsIcon != null)
            soulsIconOriginalScale = soulsIcon.transform.localScale;
        if (attackIcon != null)
            attackIconOriginalScale = attackIcon.transform.localScale;
        if (lootCountIcon != null)
            lootCountIconOriginalScale = lootCountIcon.transform.localScale;
        if (treasureCountIcon != null)
            treasureCountIconOriginalScale = treasureCountIcon.transform.localScale;
        if (characterIconImage != null)
            characterIconOriginalScale = characterIconImage.transform.localScale;

        cachedRect = GetComponent<RectTransform>();
        if (panelBackground != null)
        {
            panelBackgroundOriginalColor = panelBackground.color;
        }

        // Registrar en UIRegistry para permitir animaciones dirigidas
        if (UIRegistry.Instance != null)
        {
            UIRegistry.Instance.RegisterPlayerStats(playerIndex, this);
        }

        // Suscribirse a eventos del GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerHealthChanged += HandleHealthChanged;
            GameManager.Instance.OnPlayerCoinsChanged += HandleCoinsChanged;
            GameManager.Instance.OnSoulCollected += HandleSoulCollected;
            GameManager.Instance.OnPlayerDamaged += HandlePlayerDamaged;
            GameManager.Instance.OnCardDrawn += HandleCardDrawn;
            GameManager.Instance.OnCardPlayed += HandleCardPlayed;
            GameManager.Instance.OnCardDiscarded += HandleCardDiscarded;
            
            Debug.Log($"[PlayerStatsUI] PlayerStatsUI para playerIndex={playerIndex} suscrito a eventos del GameManager");
        }
        else
        {
            Debug.LogWarning("[PlayerStatsUI] GameManager no encontrado en la escena!");
        }

        // Inicializar con los valores actuales
        UpdateUI();
    }

    void OnDestroy()
    {
        // Desuscribirse de eventos
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerHealthChanged -= HandleHealthChanged;
            GameManager.Instance.OnPlayerCoinsChanged -= HandleCoinsChanged;
            GameManager.Instance.OnSoulCollected -= HandleSoulCollected;
            GameManager.Instance.OnPlayerDamaged -= HandlePlayerDamaged;
            GameManager.Instance.OnCardDrawn -= HandleCardDrawn;
            GameManager.Instance.OnCardPlayed -= HandleCardPlayed;
            GameManager.Instance.OnCardDiscarded -= HandleCardDiscarded;
        }
        // Desregistrar del UIRegistry
        if (UIRegistry.Instance != null)
        {
            UIRegistry.Instance.UnregisterPlayerStats(playerIndex, this);
        }
    }

    void Update()
    {
        // Actualizar cada frame (puedes optimizar esto si prefieres solo actualizar con eventos)
        UpdateUI();
    }

    /// <summary>
    /// Actualiza todos los textos con los valores actuales del jugador
    /// </summary>
    private void UpdateUI()
    {
        if (GameManager.Instance == null) return;

        currentPlayer = GameManager.Instance.GetPlayer(playerIndex);
        if (currentPlayer == null) return;

        // Actualizar textos
        if (healthText != null)
        {
            // Formato: "2/2" (vida actual / vida máxima)
            healthText.text = $"{currentPlayer.health}/{currentPlayer.maxHealth}";
        }
        
        if (coinsText != null)
        {
            // Monedas solo muestran el número actual
            coinsText.text = currentPlayer.coins.ToString();
        }
        
        if (soulsText != null)
        {
            // Formato: "0/4" (almas actuales / almas para ganar)
            // Usar PlayerData.souls, ya que las almas otorgadas por monstruos no siempre tienen una CardData específica
            int currentSouls = currentPlayer.souls;
            int soulsToWin = GameManager.Instance.GetSoulsToWin();

            soulsText.text = $"{currentSouls}/{soulsToWin}";
        }

        // Daño de ataque
        if (showAttackDamage && attackText != null)
        {
            attackText.text = currentPlayer.attackDamage.ToString();
        }

        // Cantidad de cartas de Loot en mano
        if (showLootCount && lootCountText != null)
        {
            lootCountText.text = currentPlayer.hand.Count.ToString();
        }

        // Cantidad de tesoros controlados (activos + pasivos, sin almas)
        if (showTreasureCount && treasureCountText != null)
        {
            int treasures = 0;
            foreach (var item in currentPlayer.activeItems)
            {
                if (item != null && item.cardType == CardType.Treasure)
                {
                    treasures++;
                }
            }
            foreach (var item in currentPlayer.passiveItems)
            {
                if (item != null && item.cardType == CardType.Treasure)
                {
                    treasures++;
                }
            }
            treasureCountText.text = treasures.ToString();
        }
    }

    /// <summary>
    /// Maneja cuando cambia la vida del jugador
    /// </summary>
    private void HandleHealthChanged(PlayerData player, int newHealth)
    {
        if (currentPlayer == null || player.playerId != currentPlayer.playerId) return;

        if (animateOnChange && healthIcon != null)
        {
            AnimatePunch(healthIcon.transform, ref healthPunchCoroutine, healthIconOriginalScale);
        }
        
        UpdateUI();
    }

    /// <summary>
    /// Maneja cuando cambian las monedas del jugador
    /// </summary>
    private void HandleCoinsChanged(PlayerData player, int newCoins)
    {
        if (currentPlayer == null || player.playerId != currentPlayer.playerId) return;
        // Si hay una animación de monedas en curso, se suprime este pulso inmediato
        if (!suppressNextCoinsPulse)
        {
            if (animateOnChange && coinsIcon != null)
            {
                AnimatePunch(coinsIcon.transform, ref coinsPunchCoroutine, coinsIconOriginalScale);
            }
        }
        else
        {
            suppressNextCoinsPulse = false; // consumir la supresión
        }
        
        UpdateUI();
    }

    /// <summary>
    /// Maneja cuando se recolecta un alma
    /// </summary>
    private void HandleSoulCollected(PlayerData player, CardData soul)
    {
        Debug.Log($"[PlayerStatsUI] HandleSoulCollected llamado: player.playerId={player.playerId}, playerName={player.playerName}, this.playerIndex={playerIndex}, currentPlayer={(currentPlayer != null ? currentPlayer.playerId.ToString() : "null")}");
        
        if (currentPlayer == null || player.playerId != currentPlayer.playerId) return;

        if (soul != null)
        {
            Debug.Log($"[PlayerStatsUI] {player.playerName} recolectó: {soul.cardName}");
        }
        else
        {
            Debug.Log($"[PlayerStatsUI] {player.playerName} obtuvo un alma");
        }
        
        if (animateOnChange && soulsIcon != null)
        {
            AnimatePunch(soulsIcon.transform, ref soulsPunchCoroutine, soulsIconOriginalScale);
        }
        
        UpdateUI();
    }

    /// <summary>
    /// Maneja cuando el jugador recibe daño
    /// </summary>
    private void HandlePlayerDamaged(PlayerData player)
    {
        // Volvemos al comportamiento anterior: NO animar el panel por evento de daño.
        // Las animaciones deben dispararse solo por cambios específicos:
        // - Vida -> HandleHealthChanged
        // - Monedas -> HandleCoinsChanged
        // - Almas -> HandleSoulCollected
        // - Daño de ataque -> cuando cambie attackDamage (se actualiza en Update)
        // - Loot/Tesoros -> OnCardDrawn, OnCardPlayed, OnCardDiscarded
        if (player == null) return;
        Debug.Log($"[PlayerStatsUI] Daño recibido por {player.playerName}. Sin animación de panel (solo por cambios de stats).");
    }

    /// <summary>
    /// Maneja cuando el jugador roba una carta (actualizar contador de Loot)
    /// </summary>
    private void HandleCardDrawn(PlayerData player, CardData card)
    {
        if (player == null || currentPlayer == null || player.playerId != currentPlayer.playerId) return;
        if (card == null || card.cardType != CardType.Loot) return;

        if (animateOnChange && showLootCount && lootCountIcon != null)
        {
            AnimatePunch(lootCountIcon.transform, ref lootCountPunchCoroutine, lootCountIconOriginalScale);
        }
        UpdateUI();
    }

    /// <summary>
    /// Maneja cuando el jugador juega una carta (actualizar contador si es Loot o Tesoro)
    /// </summary>
    private void HandleCardPlayed(PlayerData player, CardData card)
    {
        if (player == null || currentPlayer == null || player.playerId != currentPlayer.playerId) return;
        if (card == null) return;

        if (animateOnChange)
        {
            if (card.cardType == CardType.Loot && showLootCount && lootCountIcon != null)
            {
                AnimatePunch(lootCountIcon.transform, ref lootCountPunchCoroutine, lootCountIconOriginalScale);
            }
            else if (card.cardType == CardType.Treasure && showTreasureCount && treasureCountIcon != null)
            {
                AnimatePunch(treasureCountIcon.transform, ref treasureCountPunchCoroutine, treasureCountIconOriginalScale);
            }
        }
        UpdateUI();
    }

    /// <summary>
    /// Maneja cuando el jugador descarta una carta (actualizar contador si es Loot o Tesoro)
    /// </summary>
    private void HandleCardDiscarded(PlayerData player, CardData card)
    {
        if (player == null || currentPlayer == null || player.playerId != currentPlayer.playerId) return;
        if (card == null) return;

        if (animateOnChange)
        {
            if (card.cardType == CardType.Loot && showLootCount && lootCountIcon != null)
            {
                AnimatePunch(lootCountIcon.transform, ref lootCountPunchCoroutine, lootCountIconOriginalScale);
            }
            else if (card.cardType == CardType.Treasure && showTreasureCount && treasureCountIcon != null)
            {
                AnimatePunch(treasureCountIcon.transform, ref treasureCountPunchCoroutine, treasureCountIconOriginalScale);
            }
        }
        UpdateUI();
    }

    private System.Collections.IEnumerator DamageFeedbackRoutine()
    {
        // Flash rojo en background si está asignado
        if (panelBackground != null)
        {
            panelBackground.DOKill();
            var original = panelBackgroundOriginalColor;
            panelBackground.color = damageFlashColor;
            // Fade back to original
            panelBackground.DOColor(original, damageFlashDuration).SetEase(Ease.OutQuad);
        }

        // Sacudón/escala del panel
        if (cachedRect != null)
        {
            cachedRect.DOKill();
            Vector3 baseScale = Vector3.one;
            cachedRect.localScale = baseScale;
            // Pequeño punch y regreso
            cachedRect.DOPunchScale(Vector3.one * (damageShakeScale - 1f), damageShakeDuration, 8, 0.7f);
        }

        yield return new WaitForSeconds(damageFlashDuration);
    }

    /// <summary>
    /// Animación de "punch" cuando cambian los valores
    /// </summary>
    private void AnimatePunch(Transform target, ref Coroutine coroutineRef, Vector3 originalScale)
    {
        if (target == null) return;

        // Detener la corrutina anterior SOLO de este icono específico
        if (coroutineRef != null)
        {
            StopCoroutine(coroutineRef);
            target.localScale = originalScale; // Resetear a escala original guardada
        }

        // Usar DOTween si está disponible, sino usar simple scale
        #if DOTWEEN_ENABLED
        // Cancelar animaciones DOTween previas en este objeto
        target.DOKill();
        target.localScale = originalScale;
      target.DOPunchScale(originalScale * (punchScale - 1f), punchDuration, 1, 0.5f)
          .OnComplete(() => { if (target != null) target.localScale = originalScale; });
        #else
        // Fallback simple sin DOTween
        coroutineRef = StartCoroutine(SimplePunchScale(target, originalScale));
        #endif
    }

    private System.Collections.IEnumerator SimplePunchScale(Transform target, Vector3 originalScale)
    {
        float elapsed = 0f;

        // Fase 1: Escalar hacia arriba
        float halfDuration = punchDuration * 0.5f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float scale = Mathf.Lerp(1f, punchScale, t);
            target.localScale = originalScale * scale;
            yield return null;
        }

        // Fase 2: Volver a escala original
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float scale = Mathf.Lerp(punchScale, 1f, t);
            target.localScale = originalScale * scale;
            yield return null;
        }

        // Asegurar que vuelva exactamente a la escala original
        target.localScale = originalScale;
    }

    /// <summary>
    /// Métodos públicos para modificar stats (llamados desde botones o GameManager)
    /// </summary>
    public void AddHealth(int amount)
    {
        if (currentPlayer != null)
        {
            GameManager.Instance?.ChangePlayerHealth(currentPlayer, amount);
        }
    }

    public void AddCoins(int amount)
    {
        if (currentPlayer != null)
        {
            GameManager.Instance?.ChangePlayerCoins(currentPlayer, amount);
        }
    }

    public void AddSoul(CardData soulCard)
    {
        if (currentPlayer != null)
        {
            GameManager.Instance?.CollectSoul(currentPlayer, soulCard);
        }
    }

    /// <summary>
    /// Llamado por animadores externos para provocar un pulso en el icono de monedas.
    /// </summary>
    public void PulseCoinsIcon()
    {
        if (animateOnChange && coinsIcon != null)
        {
            AnimatePunch(coinsIcon.transform, ref coinsPunchCoroutine, coinsIconOriginalScale);
        }
    }

    /// <summary>
    /// Suprime el próximo pulso de monedas causado por el evento de cambio inmediato.
    /// Útil cuando se reproducirá una animación especial (monedas volando) y el pulso se hará al llegar.
    /// </summary>
    public void SuppressNextCoinsPulse()
    {
        suppressNextCoinsPulse = true;
    }

    /// <summary>
    /// Asigna el icono del personaje en el panel del jugador (si hay Image configurada).
    /// </summary>
    public void SetCharacterIcon(Sprite icon)
    {
        if (characterIconImage == null) return;
        characterIconImage.sprite = icon;
        characterIconImage.enabled = icon != null;
        // Pequeña animación pop al asignar, si está visible
        if (animateOnChange && characterIconImage.gameObject.activeInHierarchy)
        {
            AnimatePunch(characterIconImage.transform, ref characterIconPunchCoroutine, characterIconOriginalScale == Vector3.zero ? Vector3.one : characterIconOriginalScale);
        }
    }
}
