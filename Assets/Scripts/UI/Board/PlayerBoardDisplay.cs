using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// World-space UI para mostrar la zona de un jugador sobre el tablero:
/// - Carta de personaje + ítem eterno
/// - Stats: vida, monedas, ataque (iconos + textos)
/// - Nombre del jugador
/// - Tesoros ganados durante partida (cartas físicas)
/// - Abanico de cartas bocabajo (mano)
/// </summary>
public class PlayerBoardDisplay : MonoBehaviour
{
    public enum Corner
    {
        BottomRight,
        TopLeft,
        TopRight,
        BottomLeft
    }

    [Header("Carta de Personaje e Ítem Eterno")]
    public Image characterCardImage;
    public Image eternalItemImage;

    [Header("Nombre del Jugador")]
    public TMP_Text playerNameText;

    [Header("Stats (iconos ya deben tener sprites asignados en prefab)")]
    public RectTransform statsPanel; // Panel contenedor de todas las stats (recomendado asignar)
    [Tooltip("RectTransform objetivo para animaciones de monedas (si no se asigna, usará coinsText o statsPanel)")]
    public RectTransform coinsTargetRect;
    [Tooltip("Icono de monedas (opcional) para coincidir sprite/animación")]
    public Image coinsIconImage;
    public TMP_Text coinsText;
    public TMP_Text healthText;
    public TMP_Text attackText;
    [Tooltip("Icono de almas (opcional) para animación y visibilidad")] 
    public Image soulsIconImage;
    [Tooltip("Texto de almas. Sugerido formato: current/goal")] 
    public TMP_Text soulsText;

    [Header("Tesoros (instanciar durante partida)")]
    public Transform itemsContainer;
    public GameObject itemCardPrefab;
    public Vector2 itemCardSize = new Vector2(150f, 210f);
    public float itemSpacing = 20f;

    [Header("Abanico de Mano (cartas bocabajo)")]
    public Transform handFanContainer;
    public GameObject handCardPrefab; // carta bocabajo genérica
    public Vector2 handCardSize = new Vector2(100f, 140f);
    public float handCardSpacing = 15f;
    public float handCardRotationStep = 5f; // grados entre cartas
    [Tooltip("Separación extra para TOP (positivo = más lejos del borde superior)")]
    public float handFanExtraYTop = 20f;
    [Tooltip("Separación extra para BOTTOM (positivo = más lejos del borde inferior)")]
    public float handFanExtraYBottom = 20f;

    [Header("Layout (offsets en pixeles del RectTransform)")]
    public float sideMargin = 16f;
    public float topMargin = 16f;
    public float bottomMargin = 16f;
    public float betweenCards = 12f;
    [Tooltip("Separación horizontal desde la carta de personaje hacia el panel de stats")]
    public float characterToStatsX = 220f;
    [Tooltip("Separación horizontal desde la carta de personaje hacia el ítem eterno")]
    public float characterToEternalX = 180f;
    [Tooltip("Separación vertical del panel de stats respecto al centro (arriba para top, abajo para bottom)")]
    public float statsOffsetY = 120f;

    private PlayerData boundPlayer;
    private CharacterDataSO boundCharSO;
    private readonly List<GameObject> spawnedItems = new List<GameObject>();
    private readonly List<GameObject> spawnedHandCards = new List<GameObject>();
    private Corner currentCorner = Corner.BottomRight;

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        // Unregister del UIRegistry si corresponde
        if (UIRegistry.Instance != null && boundPlayer != null)
        {
            UIRegistry.Instance.UnregisterPlayerBoard(boundPlayer.playerId, this);
        }
    }

    public void Bind(PlayerData player, CharacterDataSO charSO)
    {
        boundPlayer = player;
        boundCharSO = charSO;

        UpdateCharacterCard();
        UpdateEternalItem();
        UpdatePlayerName();
        UpdateStats();
        RefreshItems();
        RefreshHandFan();

        // Reaplicar layout por si Bind se llama antes de SetOrientation
        ApplyCornerLayout(currentCorner);

        // Registrar este board como destino de UI para este jugador (requiere boundPlayer)
        if (UIRegistry.Instance != null && boundPlayer != null)
        {
            UIRegistry.Instance.RegisterPlayerBoard(boundPlayer.playerId, this);
        }
    }

    private void Subscribe()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnPlayerHealthChanged += HandleHealthChanged;
        GameManager.Instance.OnPlayerCoinsChanged += HandleCoinsChanged;
        GameManager.Instance.OnSoulCollected += HandleSoulCollected;
        GameManager.Instance.OnCardDrawn += HandleCardDrawn;
        GameManager.Instance.OnCardPlayed += HandleCardPlayed;
        GameManager.Instance.OnCardDiscarded += HandleCardDiscarded;
    }

    private void Unsubscribe()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnPlayerHealthChanged -= HandleHealthChanged;
        GameManager.Instance.OnPlayerCoinsChanged -= HandleCoinsChanged;
        GameManager.Instance.OnSoulCollected -= HandleSoulCollected;
        GameManager.Instance.OnCardDrawn -= HandleCardDrawn;
        GameManager.Instance.OnCardPlayed -= HandleCardPlayed;
        GameManager.Instance.OnCardDiscarded -= HandleCardDiscarded;
    }

    private bool IsMyPlayer(PlayerData player)
    {
        return boundPlayer != null && player != null && boundPlayer.playerId == player.playerId;
    }

    // Eventos
    private void HandleHealthChanged(PlayerData player, int newHealth)
    {
        if (!IsMyPlayer(player)) return;
        UpdateStats();
    }

    private void HandleCoinsChanged(PlayerData player, int newCoins)
    {
        if (!IsMyPlayer(player)) return;
        UpdateStats();
    }

    private void HandleSoulCollected(PlayerData player, CardData soul)
    {
        if (!IsMyPlayer(player)) return;
        UpdateSoulsUI(pulse:true);
    }

    private void HandleCardDrawn(PlayerData player, CardData card)
    {
        if (!IsMyPlayer(player)) return;
        RefreshHandFan();
    }

    private void HandleCardPlayed(PlayerData player, CardData card)
    {
        if (!IsMyPlayer(player)) return;
        RefreshItems();
        RefreshHandFan();
    }

    private void HandleCardDiscarded(PlayerData player, CardData card)
    {
        if (!IsMyPlayer(player)) return;
        RefreshHandFan();
    }

    // Actualización de elementos
    private void UpdateCharacterCard()
    {
        if (characterCardImage == null) return;
        if (boundCharSO != null && boundCharSO.characterCardFront != null)
        {
            characterCardImage.enabled = true;
            characterCardImage.sprite = boundCharSO.characterCardFront;
            characterCardImage.preserveAspect = true;
            characterCardImage.color = Color.white;
            characterCardImage.raycastTarget = true; // permitir hover para preview

            // Adjuntar preview target (usa el SO del personaje)
            AttachPreviewTarget(characterCardImage.gameObject, so: null, sprite: boundCharSO.characterCardFront, data: null);
            // Hacer targeteable al jugador (bombas pueden seleccionar jugadores)
            AttachPlayerTargetable(characterCardImage.gameObject);
        }
        else
        {
            characterCardImage.enabled = false;
        }
    }

    private void UpdateEternalItem()
    {
        if (eternalItemImage == null) return;
        if (boundCharSO != null && boundCharSO.eternalItems != null && boundCharSO.eternalItems.Count > 0)
        {
            var firstEternal = boundCharSO.eternalItems[0];
            if (firstEternal != null && firstEternal.frontSprite != null)
            {
                eternalItemImage.enabled = true;
                eternalItemImage.sprite = firstEternal.frontSprite;
                eternalItemImage.preserveAspect = true;
                eternalItemImage.color = Color.white;
                eternalItemImage.raycastTarget = true; // permitir hover para preview

                // Adjuntar preview target (usa CardDataSO del eterno)
                AttachPreviewTarget(eternalItemImage.gameObject, so: firstEternal, sprite: null, data: null);
                return;
            }
        }
        eternalItemImage.enabled = false;
    }

    private void AttachPlayerTargetable(GameObject go)
    {
        if (go == null || boundPlayer == null) return;
        var t = go.GetComponent<Targetable>();
        if (t == null) t = go.AddComponent<Targetable>();
        t.targetType = TargetType.Player;
        // En este proyecto playerId == índice
        t.playerIndex = boundPlayer.playerId;
    }

    private void UpdatePlayerName()
    {
        if (playerNameText != null && boundPlayer != null)
        {
            playerNameText.text = boundPlayer.playerName;
        }
    }

    private void UpdateStats()
    {
        if (boundPlayer == null) return;
        if (healthText != null) healthText.text = $"{boundPlayer.health}/{Mathf.Max(1, boundPlayer.maxHealth)}";
        if (coinsText != null) coinsText.text = boundPlayer.coins.ToString();
        if (attackText != null) attackText.text = boundPlayer.attackDamage.ToString();
        UpdateSoulsUI(pulse:false);
    }

    private void UpdateSoulsUI(bool pulse)
    {
        if (soulsText == null && soulsIconImage == null) return; // nada que actualizar
        if (boundPlayer == null || GameManager.Instance == null) return;

        // Texto: "X/Y" si existe el campo
        if (soulsText != null)
        {
            int currentSouls = boundPlayer.souls;
            int soulsToWin = GameManager.Instance.GetSoulsToWin();
            soulsText.text = $"{currentSouls}/{soulsToWin}";
        }

        // Pequeño punch opcional en el icono
        if (pulse)
        {
            Transform t = null;
            if (soulsIconImage != null) t = soulsIconImage.transform;
            else if (soulsText != null) t = soulsText.transform;
            else if (statsPanel != null) t = statsPanel.transform;
            if (t != null)
            {
                t.DOKill(complete: true);
                var baseScale = t.localScale;
                t.localScale = baseScale;
                t.DOPunchScale(Vector3.one * 0.12f, 0.18f, 10, 0.9f);
            }
        }
    }

    private void RefreshItems()
    {
        // Limpiar tesoros anteriores
        foreach (var go in spawnedItems)
            if (go != null) Destroy(go);
        spawnedItems.Clear();

        if (itemsContainer == null || itemCardPrefab == null || boundPlayer == null) return;

        // Mostrar tesoros (activos + pasivos)
        List<CardData> treasures = new List<CardData>();
        if (boundPlayer.activeItems != null)
        {
            foreach (var item in boundPlayer.activeItems)
                if (item != null && item.cardType == CardType.Treasure && !item.isEternal)
                    treasures.Add(item);
        }
        if (boundPlayer.passiveItems != null)
        {
            foreach (var item in boundPlayer.passiveItems)
                if (item != null && item.cardType == CardType.Treasure && !item.isEternal)
                    treasures.Add(item);
        }

        float x = 0f;
        foreach (var treasure in treasures)
        {
            var go = Instantiate(itemCardPrefab, itemsContainer);
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(x, 0f);
                rt.sizeDelta = itemCardSize;
            }

            var img = go.GetComponent<Image>();
            if (img == null) img = go.AddComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = true; // habilitar raycasts para hover y preview
            img.sprite = treasure.frontSprite;
            img.color = Color.white;

            // Adjuntar preview target para cada tesoro
            AttachPreviewTarget(go, so: treasure.sourceScriptableObject, sprite: treasure.frontSprite, data: treasure);

            spawnedItems.Add(go);
            x += itemCardSize.x + itemSpacing;
        }
    }

    private void RefreshHandFan()
    {
        // Limpiar abanico anterior
        foreach (var go in spawnedHandCards)
            if (go != null) Destroy(go);
        spawnedHandCards.Clear();

        if (handFanContainer == null || handCardPrefab == null || boundPlayer == null) return;

        int handCount = boundPlayer.hand != null ? boundPlayer.hand.Count : 0;
        if (handCount == 0) return;

        // Crear abanico de cartas bocabajo
        for (int i = 0; i < handCount; i++)
        {
            var go = Instantiate(handCardPrefab, handFanContainer);
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);

                // Posición en abanico horizontal
                float offset = (i - (handCount - 1) * 0.5f) * handCardSpacing;
                rt.anchoredPosition = new Vector2(offset, 0f);
                rt.sizeDelta = handCardSize;

                // Rotación para efecto abanico
                float rotation = (i - (handCount - 1) * 0.5f) * handCardRotationStep;
                // Top corners: arco abierto hacia abajo (rotación normal positiva)
                // Bottom corners: arco abierto hacia arriba (rotación invertida)
                float dir = (currentCorner == Corner.BottomLeft || currentCorner == Corner.BottomRight) ? -1f : 1f;
                rt.localRotation = Quaternion.Euler(0f, 0f, rotation * dir);
            }

            var img = go.GetComponent<Image>();
            if (img == null) img = go.AddComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = Color.white;
            // El sprite debe ser el dorso genérico de carta (asignar en prefab handCardPrefab)

            spawnedHandCards.Add(go);
        }
    }

    public void SetOrientation(Corner corner)
    {
        currentCorner = corner;
        ApplyCornerLayout(corner);
    }

    private void ApplyCornerLayout(Corner corner)
    {
        var nameRT = playerNameText != null ? playerNameText.rectTransform : null;
        var charRT = characterCardImage != null ? characterCardImage.rectTransform : null;
        var eternalRT = eternalItemImage != null ? eternalItemImage.rectTransform : null;
        var statsRT = GetStatsPanelRect();
        var itemsRT = itemsContainer as RectTransform;
        var handRT = handFanContainer as RectTransform;

        switch (corner)
        {
            case Corner.TopLeft:
                // Nombre arriba izquierda
                Place(nameRT, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(sideMargin, -topMargin));
                // Carta personaje izquierda centro
                Place(charRT, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(sideMargin, 0f));
                // Ítem eterno a la derecha del personaje
                if (eternalRT != null)
                    Place(eternalRT, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(sideMargin + characterToEternalX, 0f));
                // Stats arriba (para jugadores top)
                if (statsRT != null)
                    Place(statsRT, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(sideMargin + characterToStatsX, statsOffsetY));
                // Tesoros a la derecha
                Place(itemsRT, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-sideMargin, 0f));
                // Mano arriba centro
                Place(handRT, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -(topMargin + handFanExtraYTop)));
                break;
            case Corner.TopRight:
                Place(nameRT, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-sideMargin, -topMargin));
                // Para respetar "eterno a la derecha del personaje", ubicamos eterno más cerca del borde y el personaje a su izquierda
                Place(eternalRT, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-sideMargin, 0f));
                Place(charRT, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-(sideMargin + characterToEternalX), 0f));
                // Stats arriba (para jugadores top)
                if (statsRT != null)
                    Place(statsRT, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-(sideMargin + characterToStatsX), statsOffsetY));
                // Tesoros a la izquierda
                Place(itemsRT, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(sideMargin, 0f));
                // Mano arriba centro
                Place(handRT, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -(topMargin + handFanExtraYTop)));
                break;
            case Corner.BottomLeft:
                Place(nameRT, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(sideMargin, bottomMargin));
                Place(charRT, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(sideMargin, 0f));
                if (eternalRT != null)
                    Place(eternalRT, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(sideMargin + characterToEternalX, 0f));
                // Stats abajo (para jugadores bottom)
                if (statsRT != null)
                    Place(statsRT, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(sideMargin + characterToStatsX, -statsOffsetY));
                // Tesoros a la derecha
                Place(itemsRT, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-sideMargin, 0f));
                // Mano abajo centro
                Place(handRT, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, bottomMargin - handFanExtraYBottom));
                break;
            case Corner.BottomRight:
            default:
                Place(nameRT, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-sideMargin, bottomMargin));
                // Eterno al borde derecho, personaje a su izquierda
                Place(eternalRT, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-sideMargin, 0f));
                Place(charRT, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-(sideMargin + characterToEternalX), 0f));
                // Stats abajo (para jugadores bottom)
                if (statsRT != null)
                    Place(statsRT, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-(sideMargin + characterToStatsX), -statsOffsetY));
                // Tesoros a la izquierda
                Place(itemsRT, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(sideMargin, 0f));
                // Mano abajo centro
                Place(handRT, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, bottomMargin - handFanExtraYBottom));
                break;
        }

        if (statsRT == null)
        {
            Debug.LogWarning("[PlayerBoardDisplay] statsPanel no asignado ni detectado; el StatsPanel conservará su posición por defecto del prefab (posible centro). Asigna el campo 'statsPanel' en el prefab.");
        }
        if (handRT == null)
        {
            Debug.LogWarning("[PlayerBoardDisplay] handFanContainer no asignado; el abanico conservará su posición por defecto.");
        }
    }

    private RectTransform GetStatsPanelRect()
    {
        // Preferimos el panel asignado explícitamente
        if (statsPanel != null) return statsPanel;

        // Si no está asignado, intentamos encontrar el ancestro común de coins/health/attack
        var coinsRT = coinsText != null ? coinsText.rectTransform : null;
        var healthRT = healthText != null ? healthText.rectTransform : null;
        var attackRT = attackText != null ? attackText.rectTransform : null;
        if (coinsRT == null || healthRT == null || attackRT == null) return null;

        // Subir desde coins hasta encontrar un padre que contenga a health y attack
        Transform cursor = coinsRT.transform;
        while (cursor != null)
        {
            var crt = cursor as RectTransform;
            if (crt != null && IsAncestorOf(crt, healthRT) && IsAncestorOf(crt, attackRT))
                return crt;
            cursor = cursor.parent;
        }
        return null;
    }

    private bool IsAncestorOf(RectTransform ancestor, RectTransform desc)
    {
        if (ancestor == null || desc == null) return false;
        Transform t = desc.transform;
        while (t != null)
        {
            if (t == ancestor.transform) return true;
            t = t.parent;
        }
        return false;
    }

    private void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition)
    {
        if (rt == null) return;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPosition;
    }

    /// <summary>
    /// Asegura y configura un CardPreviewTarget sobre un objeto dado.
    /// Usa la prioridad: CardData > CardDataSO > Sprite.
    /// </summary>
    private void AttachPreviewTarget(GameObject go, CardDataSO so, Sprite sprite, CardData data)
    {
        if (go == null) return;
        var target = go.GetComponent<CardPreviewTarget>();
        if (target == null) target = go.AddComponent<CardPreviewTarget>();
        target.cardData = data;
        target.cardDataSO = (data == null) ? so : null; // si hay data, no usamos SO
        target.spriteOverride = (data == null && so == null) ? sprite : null;
        target.showOnHover = false; // hover solo animación; preview al click
        target.showOnClick = true;
        target.hoverScaleEnabled = true;
        // Offset por orientación del borde (levemente hacia el centro de la mesa)
        switch (currentCorner)
        {
            case Corner.TopLeft:
                target.worldOffset = new Vector3(0.12f, -0.12f, 0f);
                break;
            case Corner.TopRight:
                target.worldOffset = new Vector3(-0.12f, -0.12f, 0f);
                break;
            case Corner.BottomLeft:
                target.worldOffset = new Vector3(0.12f, 0.12f, 0f);
                break;
            case Corner.BottomRight:
            default:
                target.worldOffset = new Vector3(-0.12f, 0.12f, 0f);
                break;
        }
    }

    /// <summary>
    /// Hace un pequeño "pop" en el icono/rect de monedas como feedback al recibir monedas.
    /// </summary>
    public void PulseCoins()
    {
        Transform t = null;
        if (coinsIconImage != null) t = coinsIconImage.transform;
        else if (coinsTargetRect != null) t = coinsTargetRect.transform;
        else if (coinsText != null) t = coinsText.transform;
        else if (statsPanel != null) t = statsPanel.transform;
        if (t == null) return;

        // Cancelar tweens previos sobre el mismo transform para evitar acumulación
        t.DOKill(complete: true);
        Vector3 baseScale = t.localScale;
        t.localScale = baseScale; // asegurar base
        // Pequeño punch
        t.DOPunchScale(Vector3.one * 0.12f, 0.18f, 10, 0.9f);
    }

    /// <summary>
    /// Devuelve el RectTransform objetivo ideal para apuntar monedas voladoras.
    /// </summary>
    public RectTransform GetCoinsTargetRect()
    {
        if (coinsTargetRect != null) return coinsTargetRect;
        if (coinsText != null) return coinsText.rectTransform;
        if (coinsIconImage != null) return coinsIconImage.rectTransform;
        return statsPanel;
    }

    /// <summary>
    /// Devuelve el sprite del icono de monedas si está disponible (para que la moneda animada coincida).
    /// </summary>
    public Sprite GetCoinSprite()
    {
        return coinsIconImage != null ? coinsIconImage.sprite : null;
    }
}
