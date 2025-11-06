using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

/// <summary>
/// UI para la selección de personajes al inicio de la partida
/// </summary>
public partial class CharacterSelectionUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private Image overlay;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private int forceCanvasSortingOrder = 1000; // 0 para no forzar (si es 0, se intentará subir a 1000 si está bajo)
    
    [Header("Cartas de Personaje")]
    [SerializeField] private Transform cardContainer;
    [SerializeField] private GameObject characterCardPrefab;
    [SerializeField] private float cardSpacing = 300f;
    [SerializeField] private Vector2 characterCardSize = new Vector2(600f, 840f);
    
    [Header("Objetos Eternos")]
    [SerializeField] private Transform eternalItemsContainer;
    [SerializeField] private GameObject eternalItemCardPrefab;
    [SerializeField] private Vector2 eternalItemCardSize = new Vector2(360f, 504f);
    [SerializeField] private float eternalItemsSideOffset = 40f;
    [SerializeField] private float eternalItemSpacing = 220f;

    [Header("Posición de Textos (Opcional)")]
    [SerializeField] private bool overrideTextPositions = false;
    [SerializeField] private Vector2 playerNameTextPos = new Vector2(0, 420);
    [SerializeField] private Vector2 statusTextPos = new Vector2(0, 360);
    [SerializeField] private Vector2 timerTextPos = new Vector2(0, -420);
    
    [Header("Texto de Estado")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text playerNameText;

    [Header("Reusar Temporizador de Turnos (Opcional)")]
    [Tooltip("Si asignas un TurnTimerUI dentro del Canvas de selección, se reutilizará para mostrar la cuenta regresiva de selección.")]
    [SerializeField] private TurnTimerUI selectionTurnTimerUI;

    [Header("Animación de Selección")]
    [SerializeField] private Color selectedTint = new Color(0.65f, 1f, 0.65f, 1f);
    [SerializeField] private float selectScaleUp = 1.1f;
    [SerializeField] private float selectScaleDuration = 0.15f;
    [SerializeField] private float fadeOtherDuration = 0.25f;
    [SerializeField] private float moveToCenterDuration = 0.35f;
    
    [Header("Overlay y Transiciones")]
    [Tooltip("Mantener el overlay visible durante toda la selección (sin desvanecer entre jugadores)")]
    [SerializeField] private bool keepOverlayBetweenPlayers = true;
    
    [Header("Configuración")]
    [SerializeField] private float selectionTimeLimit = 30f;
    [SerializeField] private List<CharacterDataSO> availableCharacters = new List<CharacterDataSO>();
    
    // Estado
    private List<GameObject> characterCards = new List<GameObject>();
    private List<GameObject> eternalItemCards = new List<GameObject>();
    private List<List<GameObject>> eternalItemGroups = new List<List<GameObject>>(); // agrupados por carta
    private List<CharacterDataSO> remainingCharacters = new List<CharacterDataSO>(); // pool global para unicidad
    private CharacterDataSO selectedCharacter;
    private bool isSelectionComplete = false;
    private float remainingTime;
    private int currentPlayerIndex = 0;
    private List<PlayerData> players;
    private Dictionary<int, CharacterDataSO> playerSelections = new Dictionary<int, CharacterDataSO>();
    
    private static CharacterSelectionUI instance;
    public static CharacterSelectionUI Instance => instance;
    private bool isAnimatingSelection = false;
    private bool suppressPerPlayerFade = false;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Asegurar orden de render alto para quedar por encima del UI principal
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Debug.Log($"[CharacterSelectionUI] Canvas encontrado: '{canvas.gameObject.name}'");
            Debug.Log($"[CharacterSelectionUI] Canvas ANTES - RenderMode: {canvas.renderMode}, Enabled: {canvas.enabled}, GameObject activo: {canvas.gameObject.activeInHierarchy}");
            
            // FORZAR Screen Space Overlay si está en modo que requiere cámara y no la tiene
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
            {
                Debug.LogWarning("[CharacterSelectionUI] Canvas en modo Camera pero sin cámara asignada! Forzando a ScreenSpaceOverlay");
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            else if (canvas.renderMode == RenderMode.WorldSpace)
            {
                Debug.LogWarning("[CharacterSelectionUI] Canvas en WorldSpace! Forzando a ScreenSpaceOverlay para UI");
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            
            // Forzar SIEMPRE sorting override y orden muy alto
            canvas.overrideSorting = true;
            int targetOrder = forceCanvasSortingOrder > 0 ? forceCanvasSortingOrder : 5000;
            canvas.sortingOrder = targetOrder;
            Debug.Log($"[CharacterSelectionUI] Canvas sorting FORZADO a {canvas.sortingOrder} (overrideSorting={canvas.overrideSorting})");
            
            // Mover el canvas al FINAL de la jerarquía (se dibuja último = encima)
            canvas.transform.SetAsLastSibling();
            Debug.Log($"[CharacterSelectionUI] Canvas movido a último hermano (índice: {canvas.transform.GetSiblingIndex()})");
            
            // Log crítico de configuración de Canvas
            Debug.Log($"[CharacterSelectionUI] Canvas DESPUÉS - RenderMode: {canvas.renderMode}, PixelPerfect: {canvas.pixelPerfect}");
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace)
            {
                Debug.Log($"[CharacterSelectionUI] Canvas WorldCamera: {(canvas.worldCamera != null ? canvas.worldCamera.name : "NULL")}");
                if (canvas.worldCamera == null)
                {
                    Debug.LogError("[CharacterSelectionUI] ¡PROBLEMA! Canvas requiere cámara pero worldCamera es NULL");
                }
                    else
                    {
                        // Verificar Plane Distance (muy alto = canvas muy lejos = invisible)
                        Debug.Log($"[CharacterSelectionUI] Canvas PlaneDistance: {canvas.planeDistance}");
                        if (canvas.planeDistance > 50f)
                        {
                            Debug.LogWarning($"[CharacterSelectionUI] PlaneDistance muy alto ({canvas.planeDistance})! Ajustando a 10 para visibilidad");
                            canvas.planeDistance = 10f;
                        }
                    }
            }
            
            // Verificar si hay GraphicRaycaster
            var raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            Debug.Log($"[CharacterSelectionUI] GraphicRaycaster presente: {raycaster != null}, enabled: {(raycaster != null ? raycaster.enabled.ToString() : "N/A")}");
        }
        else
        {
            Debug.LogError("[CharacterSelectionUI] No se encontró Canvas padre! El UI no se mostrará.");
        }

        // Asegurar que el overlay bloquee raycasts
        if (overlay != null)
        {
            overlay.raycastTarget = true;
        }

        if (selectionPanel != null)
        {
            // Asegurar que el panel ocupe toda la pantalla para el overlay y las cartas
            var rt = selectionPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                EnsureStretchFull(rt);
            }
            selectionPanel.SetActive(false);
        }

        // Asegurar que los contenedores estén estirados
        if (cardContainer != null)
        {
            var crt = cardContainer.GetComponent<RectTransform>();
            if (crt != null) EnsureStretchFull(crt);
            Debug.Log("[CharacterSelectionUI] Card Container stretch aplicado");
        }
        if (eternalItemsContainer != null)
        {
            var ert = eternalItemsContainer.GetComponent<RectTransform>();
            if (ert != null) EnsureStretchFull(ert);
            Debug.Log("[CharacterSelectionUI] Eternal Container stretch aplicado");
        }

        // Asegurar Overlay detrás del contenido para no tapar cartas
        if (overlay != null)
        {
            try
            {
                overlay.transform.SetAsFirstSibling();
                Debug.Log($"[CharacterSelectionUI] Overlay movido a índice de hermano {overlay.transform.GetSiblingIndex()} (debe ser 0)");
            }
            catch { }
        }
    }
    
    /// <summary>
    /// Inicia el proceso de selección de personajes para todos los jugadores
    /// </summary>
    public void StartCharacterSelection(List<PlayerData> gamePlayers, System.Action onComplete)
    {
        players = gamePlayers;
        currentPlayerIndex = 0;
        playerSelections.Clear();
        // Inicializar pool global de personajes disponibles (unicidad)
        remainingCharacters = new List<CharacterDataSO>(availableCharacters);
        
        Debug.Log("[CharacterSelectionUI] StartCharacterSelection llamado: activando Selection Panel y comenzando secuencia");
        // Ocultar HUD de stats mientras dura la selección
        SetAllPlayerStatsVisible(false);
        // Ocultar cualquier TurnTimerUI que no sea el dedicado a la selección
        DisableOtherTurnTimers();
        StartCoroutine(CharacterSelectionSequence(onComplete));
    }
    
    private IEnumerator CharacterSelectionSequence(System.Action onComplete)
    {
        // Si se mantiene el overlay, hacer un FadeIn general antes de iniciar el ciclo
        if (keepOverlayBetweenPlayers)
        {
            suppressPerPlayerFade = true;
            if (selectionPanel != null)
                selectionPanel.SetActive(true);
            yield return FadeIn();
        }

        // Por cada jugador, mostrar selección
        for (int i = 0; i < players.Count; i++)
        {
            currentPlayerIndex = i;
            PlayerData player = players[i];
            
            Debug.Log($"[CharacterSelectionUI] Mostrando selección para jugador: {player.playerName} (id {player.playerId})");
            yield return StartCoroutine(ShowSelectionForPlayer(player));
        }
        
    // Una vez todos seleccionaron, aplicar las selecciones
    ApplyCharacterSelections();
    // Mostrar nuevamente HUD de stats
    SetAllPlayerStatsVisible(true);
    // Animación de Pop al reaparecer la UI principal
    PopAllPlayerUI();
    // Restaurar timers de turno ocultados
    RestoreOtherTurnTimers();
        
        // Si se mantuvo el overlay, hacer FadeOut general y ocultar panel
        if (keepOverlayBetweenPlayers)
        {
            yield return FadeOut();
            if (selectionPanel != null)
                selectionPanel.SetActive(false);
            suppressPerPlayerFade = false;
        }
        
        // Callback de completado
        onComplete?.Invoke();
    }
    
    private IEnumerator ShowSelectionForPlayer(PlayerData player)
    {
        selectedCharacter = null;
        isSelectionComplete = false;
        remainingTime = selectionTimeLimit;
        
        // Mostrar panel
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(true);
            Debug.Log("[CharacterSelectionUI] Selection Panel activado");
            
            // Forzar rebuild inmediato del Canvas para evitar retraso en renderizado
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                // Re-forzar sorting por si otro canvas lo modificó
                canvas.overrideSorting = true;
                canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 5000);
                canvas.transform.SetAsLastSibling();
                Debug.Log($"[CharacterSelectionUI] Canvas re-forzado: sorting={canvas.sortingOrder}, hermano={canvas.transform.GetSiblingIndex()}");
                
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(selectionPanel.GetComponent<RectTransform>());
                Canvas.ForceUpdateCanvases();
            }
            // Aplicar posiciones de textos si está habilitado
            ApplyTextPositionsIfNeeded();

            // Log de estado de textos y actualización inicial
            LogTextState(playerNameText, nameof(playerNameText));
            LogTextState(statusText, nameof(statusText));
            LogTextState(timerText, nameof(timerText));

            // Ocultar el timer durante la presentación de cartas
            SetSelectionTimerVisible(false);
        }
        
        // Actualizar nombre del jugador
        if (playerNameText != null)
            playerNameText.text = player.playerName;
        
        // Fade in overlay (solo si no se usa fade global)
        if (!suppressPerPlayerFade)
            yield return FadeIn();
        
        // Mostrar cartas boca abajo
        yield return ShowCardsFlipped();
        
        // Voltear cartas
        yield return RevealCards();
        
        // Mostrar timer y activar modo externo ahora que ya salieron ambas cartas
        if (selectionTurnTimerUI != null)
        {
            selectionTurnTimerUI.SetExternalMode(true);
            selectionTurnTimerUI.ExternalUpdate(remainingTime, selectionTimeLimit);
            Debug.Log("[CharacterSelectionUI] TurnTimerUI en modo externo para selección");
        }
        else
        {
            // Fallback: preparar el TMP_Text
            UpdateTimerText();
            EnsureTextRendering(timerText);
        }
        SetSelectionTimerVisible(true);

        // Iniciar temporizador de selección (tras presentaciones)
        StartCoroutine(SelectionTimer());
        
        // Esperar a que el jugador seleccione o se acabe el tiempo
        while (!isSelectionComplete && remainingTime > 0)
        {
            UpdateStatusText();
            yield return null;
        }
        
        // Si no seleccionó, elegir al azar
        if (selectedCharacter == null && availableCharacters.Count > 0)
        {
            selectedCharacter = availableCharacters[Random.Range(0, availableCharacters.Count)];
            Debug.Log($"[CharacterSelection] Tiempo agotado. Selección aleatoria: {selectedCharacter.characterName}");
        }
        
        // Guardar selección
        if (selectedCharacter != null)
        {
            playerSelections[player.playerId] = selectedCharacter;
            // Ya se muestran los objetos eternos junto a cada carta durante la selección
            // Pequeña pausa de confirmación
            yield return new WaitForSeconds(0.75f);
        }
        
        // Limpiar cartas y objetos eternos inmediatamente para evitar parpadeo entre jugadores
        ClearCards();
        ClearEternalItems();
        
        // Fade out (solo si no se usa fade global)
        if (!suppressPerPlayerFade)
            yield return FadeOut();

        // Desactivar modo externo del TurnTimerUI si está asignado
        if (selectionTurnTimerUI != null)
        {
            selectionTurnTimerUI.SetExternalMode(false);
            Debug.Log("[CharacterSelectionUI] TurnTimerUI sale de modo externo tras selección");
        }
        SetSelectionTimerVisible(false);
        
        if (selectionPanel != null && !keepOverlayBetweenPlayers)
            selectionPanel.SetActive(false);
    }
    
    private IEnumerator FadeIn()
    {
        if (canvasGroup != null)
        {
            Debug.Log("[CharacterSelectionUI] FadeIn: habilitando CanvasGroup");
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            
            // Forzar actualización del canvas antes de animar
            Canvas.ForceUpdateCanvases();
            yield return null; // Esperar 1 frame
            
            canvasGroup.DOFade(1f, 0.5f);
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            Debug.LogWarning("[CharacterSelectionUI] FadeIn: canvasGroup es NULL (revisa asignación en Selection Panel)");
        }
    }
    
    private IEnumerator FadeOut()
    {
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0f, 0.5f);
            yield return new WaitForSeconds(0.5f);
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
    
    private IEnumerator ShowCardsFlipped()
    {
        ClearCards();
        ClearEternalItems();
        eternalItemGroups.Clear();
        
        if (characterCardPrefab == null || cardContainer == null) yield break;
        
        // Info de tamaño del contenedor para depurar visibilidad
        var ccRT = cardContainer.GetComponent<RectTransform>();
        if (ccRT != null)
        {
            Debug.Log($"[CharacterSelectionUI] Card Container rect size: {ccRT.rect.size}");
        }

        // Desactivar temporalmente LayoutGroups para no interferir con posiciones/animaciones
        var cgLayout = cardContainer.GetComponent<UnityEngine.UI.LayoutGroup>();
        bool disabledCgLayout = false;
        if (cgLayout != null && cgLayout.enabled)
        {
            cgLayout.enabled = false;
            disabledCgLayout = true;
            Debug.LogWarning("[CharacterSelectionUI] Desactivando LayoutGroup en Card Container para evitar conflicto con posiciones animadas");
        }

        var etLayout = eternalItemsContainer != null ? eternalItemsContainer.GetComponent<UnityEngine.UI.LayoutGroup>() : null;
        bool disabledEtLayout = false;
        if (etLayout != null && etLayout.enabled)
        {
            etLayout.enabled = false;
            disabledEtLayout = true;
            Debug.LogWarning("[CharacterSelectionUI] Desactivando LayoutGroup en Eternal Container para evitar conflicto con posiciones animadas");
        }

        if (availableCharacters == null || availableCharacters.Count < 1)
        {
            Debug.LogWarning("[CharacterSelectionUI] No hay personajes en availableCharacters (agrega 2+ en el inspector)");
        }

        // Crear 2 cartas aleatorias boca abajo
        List<CharacterDataSO> selectedChars = GetTwoRandomCharacters();

        Debug.Log($"[CharacterSelectionUI] Generando {selectedChars.Count} cartas para selección");
        
        for (int i = 0; i < selectedChars.Count; i++)
        {
            GameObject cardObj = Instantiate(characterCardPrefab, cardContainer);
            RectTransform rt = cardObj.GetComponent<RectTransform>();
            
            if (rt != null)
            {
                // Anclas y pivote al centro para posicionar relativo al centro
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                float xPos = (i - 0.5f) * cardSpacing;
                rt.anchoredPosition = new Vector2(xPos, 0);
                // Forzar tamaño deseado para mejorar visibilidad
                rt.sizeDelta = characterCardSize;
                // Visibilidad inmediata como fallback (por si falla DOTween)
                rt.localScale = Vector3.one;
            }
            
            // Buscar Image del contenedor o hijos
            Image cardImage = cardObj.GetComponent<Image>();
            if (cardImage == null)
            {
                cardImage = cardObj.GetComponentInChildren<Image>(true);
            }
            if (cardImage == null)
            {
                cardImage = cardObj.AddComponent<Image>();
                Debug.LogWarning("[CharacterSelectionUI] characterCardPrefab no tenía Image. Se añadió uno automáticamente.");
            }
            cardImage.preserveAspect = true;
            var c = cardImage.color; c.a = 1f; c = Color.white; cardImage.color = c;
            if (cardImage != null && selectedChars[i].characterCardBack != null)
            {
                cardImage.sprite = selectedChars[i].characterCardBack;
                Debug.Log($"[CharacterSelectionUI] Carta {i}: back sprite asignado '{selectedChars[i].characterCardBack.name}'");
            }
            else
            {
                Debug.LogWarning($"[CharacterSelectionUI] Carta {i}: back sprite NULL en '{selectedChars[i].characterName}'. Se mostrará sin sprite.");
            }
            
            // Guardar referencia al CharacterData
            CharacterCard charCard = cardObj.AddComponent<CharacterCard>();
            charCard.characterData = selectedChars[i];
            charCard.selectionUI = this;
            cardObj.name = $"CharacterCard_{selectedChars[i].characterName}";

            // Asegurar interacción visible
            var cg = cardObj.GetComponent<CanvasGroup>();
            if (cg == null) cg = cardObj.AddComponent<CanvasGroup>();
            cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true;
            
            characterCards.Add(cardObj);
            
            Debug.Log($"[CharacterSelectionUI] Carta {i} instanciada: pos={rt.anchoredPosition}, size={rt.sizeDelta}, scale={rt.localScale}, activo={cardObj.activeSelf}");

            // Preparar objetos eternos de este personaje a su lado (instanciar ocultos; se revelan luego)
            if (eternalItemsContainer != null && eternalItemCardPrefab != null)
            {
                var items = selectedChars[i].eternalItems;
                if (items != null && items.Count > 0)
                {
                    // Dirección: -1 para carta izquierda, +1 para carta derecha
                    int dir = (i == 0) ? -1 : +1;
                    float cardX = (i - 0.5f) * cardSpacing;
                    float startX = cardX + dir * ((characterCardSize.x * 0.5f) + eternalItemsSideOffset);
                    var groupList = new List<GameObject>();
                    for (int j = 0; j < items.Count; j++)
                    {
                        var itemSO = items[j];
                        if (itemSO == null) continue;
                        GameObject itemObj = Instantiate(eternalItemCardPrefab, eternalItemsContainer);
                        RectTransform irt = itemObj.GetComponent<RectTransform>();
                        if (irt != null)
                        {
                            irt.anchorMin = new Vector2(0.5f, 0.5f);
                            irt.anchorMax = new Vector2(0.5f, 0.5f);
                            irt.pivot = new Vector2(0.5f, 0.5f);
                            float xPos = startX + dir * (j * eternalItemSpacing);
                            irt.anchoredPosition = new Vector2(xPos, 0f);
                            irt.sizeDelta = eternalItemCardSize;
                            irt.localScale = Vector3.zero; // oculto hasta revelar
                        }
                        Image itemImage = itemObj.GetComponent<Image>();
                        if (itemImage == null) itemImage = itemObj.AddComponent<Image>();
                        itemImage.preserveAspect = true;
                        var ic = itemImage.color; ic.a = 1f; itemImage.color = ic;
                        if (itemSO.frontSprite != null)
                        {
                            itemImage.sprite = itemSO.frontSprite;
                        }
                        eternalItemCards.Add(itemObj);
                        groupList.Add(itemObj);
                    }
                    eternalItemGroups.Add(groupList);
                }
                else
                {
                    eternalItemGroups.Add(new List<GameObject>()); // mantener índice
                }
            }
        }
        
        // Forzar actualización del layout y renderizado ANTES de animar
        Canvas.ForceUpdateCanvases();
        if (cardContainer != null)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(cardContainer.GetComponent<RectTransform>());
        }
        
        // Esperar un frame para que Unity procese el layout
        yield return null;
        
        // Ahora sí, animar las cartas
        for (int i = 0; i < characterCards.Count; i++)
        {
            var cardObj = characterCards[i];
            var rt = cardObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                // Hacer una pequeña animación desde 0.9 a 1.0 para no depender de escala cero
                rt.localScale = Vector3.one * 0.9f;
                rt.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetDelay(i * 0.1f);
            }
        }
        
        yield return new WaitForSeconds(0.8f);

        // Restaurar LayoutGroups si se desactivaron
        if (disabledCgLayout && cgLayout != null) cgLayout.enabled = true;
        if (disabledEtLayout && etLayout != null) etLayout.enabled = true;
    }
    
    private IEnumerator RevealCards()
    {
        if (characterCards.Count == 0)
        {
            Debug.LogWarning("[CharacterSelectionUI] No hay cartas para revelar");
        }
        // Voltear todas a la vez
        float flipIn = 0.2f;
        float flipOut = 0.2f;
        for (int i = 0; i < characterCards.Count; i++)
        {
            GameObject cardObj = characterCards[i];
            CharacterCard charCard = cardObj.GetComponent<CharacterCard>();
            // buscar Image robusto
            Image cardImage = cardObj.GetComponent<Image>();
            if (cardImage == null) cardImage = cardObj.GetComponentInChildren<Image>(true);
            
            if (charCard != null && cardImage != null && charCard.characterData != null)
            {
                RectTransform rt = cardObj.GetComponent<RectTransform>();
                Sequence flipSeq = DOTween.Sequence();
                flipSeq.Append(rt.DOScaleX(0f, flipIn));
                flipSeq.AppendCallback(() => {
                    if (charCard.characterData.characterCardFront != null)
                    {
                        cardImage.sprite = charCard.characterData.characterCardFront;
                        cardImage.preserveAspect = true;
                        cardImage.color = Color.white;
                    }
                });
                flipSeq.Append(rt.DOScaleX(1f, flipOut));
            }
        }
        // Esperar a que finalicen las animaciones (margen de seguridad)
        yield return new WaitForSeconds(flipIn + flipOut + 0.05f);
        
        // Revelar objetos eternos de ambas cartas a la vez
        float itemReveal = 0.25f;
        for (int g = 0; g < eternalItemGroups.Count; g++)
        {
            var list = eternalItemGroups[g];
            foreach (var itemObj in list)
            {
                if (itemObj == null) continue;
                var irt = itemObj.GetComponent<RectTransform>();
                if (irt != null) irt.DOScale(1f, itemReveal).SetEase(Ease.OutBack);
            }
        }
        yield return new WaitForSeconds(itemReveal);
    }
    
    private IEnumerator ShowEternalItems(CharacterDataSO character)
    {
        ClearEternalItems();
        
        if (eternalItemCardPrefab == null || eternalItemsContainer == null) yield break;
        if (character.eternalItems == null || character.eternalItems.Count == 0) yield break;
        
        for (int i = 0; i < character.eternalItems.Count; i++)
        {
            CardDataSO itemSO = character.eternalItems[i];
            if (itemSO == null) continue;
            
            GameObject itemObj = Instantiate(eternalItemCardPrefab, eternalItemsContainer);
            RectTransform rt = itemObj.GetComponent<RectTransform>();
            
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(i * 200f, -200f);
                rt.localScale = Vector3.zero;
            }
            
            // Configurar imagen
            Image itemImage = itemObj.GetComponent<Image>();
            if (itemImage != null && itemSO.frontSprite != null)
            {
                itemImage.sprite = itemSO.frontSprite;
            }
            
            eternalItemCards.Add(itemObj);
            
            // Animación de aparición
            rt.DOScale(0.8f, 0.3f).SetEase(Ease.OutBack).SetDelay(i * 0.15f);
        }
        
        yield return new WaitForSeconds(0.5f);
    }
    
    private IEnumerator SelectionTimer()
    {
        while (remainingTime > 0 && !isSelectionComplete)
        {
            remainingTime -= Time.deltaTime;
            UpdateTimerText();
            if (selectionTurnTimerUI != null)
            {
                selectionTurnTimerUI.ExternalUpdate(remainingTime, selectionTimeLimit);
            }
            yield return null;
        }
        
        if (!isSelectionComplete)
        {
            isSelectionComplete = true;
        }
    }
    
    private void UpdateStatusText()
    {
        if (statusText == null) return;
        
        if (selectedCharacter != null)
        {
            statusText.text = $"Personaje seleccionado: {selectedCharacter.characterName}";
        }
        else
        {
            statusText.text = "Selecciona tu personaje";
        }
    }
    
    private void UpdateTimerText()
    {
        if (timerText == null)
        {
            Debug.LogWarning("[CharacterSelectionUI] timerText no asignado en el Inspector");
            return;
        }
        if (!timerText.gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[CharacterSelectionUI] timerText GameObject no está activo en la jerarquía");
        }
        
        int seconds = Mathf.CeilToInt(remainingTime);
        timerText.text = $"Tiempo: {seconds}s";
        
        // Advertencia visual si queda poco tiempo
        if (seconds <= 10)
        {
            timerText.color = Color.red;
        }
        else
        {
            timerText.color = Color.white;
        }
    }

    private void ApplyTextPositionsIfNeeded()
    {
        if (!overrideTextPositions) return;
        if (playerNameText != null)
        {
            var rt = playerNameText.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = playerNameTextPos;
        }
        if (statusText != null)
        {
            var rt = statusText.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = statusTextPos;
        }
        if (timerText != null)
        {
            var rt = timerText.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = timerTextPos;
        }
    }
    
    private List<CharacterDataSO> GetTwoRandomCharacters()
    {
        List<CharacterDataSO> result = new List<CharacterDataSO>();

        if (remainingCharacters == null || remainingCharacters.Count == 0)
        {
            Debug.LogWarning("[CharacterSelection] No hay personajes disponibles en el pool global!");
            return result;
        }

        // Elegir hasta 2 únicos del pool global
        for (int i = 0; i < 2 && remainingCharacters.Count > 0; i++)
        {
            int index = Random.Range(0, remainingCharacters.Count);
            result.Add(remainingCharacters[index]);
            // Remover del pool global para que no se repita en otros jugadores
            remainingCharacters.RemoveAt(index);
        }

        if (result.Count < 2)
        {
            Debug.LogWarning($"[CharacterSelection] Pool insuficiente para ofrecer 2 personajes (ofrecidos: {result.Count}). Agrega más en availableCharacters para partidas con más jugadores.");
        }

        return result;
    }
    
    /// <summary>
    /// Llamado cuando el jugador hace clic en una carta
    /// </summary>
    public void OnCharacterSelected(CharacterDataSO character)
    {
        // deprecated path kept for compatibility if called elsewhere
        if (isAnimatingSelection || isSelectionComplete) return;
        selectedCharacter = character;
        Debug.Log($"[CharacterSelection] Jugador seleccionó (legacy): {character.characterName}");
        // Buscar carta seleccionada y ejecutar animación antes de completar selección
        int selIndex = FindSelectedCardIndex(character);
        StartCoroutine(PlaySelectionAnimation(selIndex));
    }

    public void OnCharacterClicked(CharacterCard card)
    {
        if (card == null || card.characterData == null) return;
        if (isAnimatingSelection || isSelectionComplete) return;
        selectedCharacter = card.characterData;
        int selIndex = FindSelectedCardIndex(card.characterData);
        StartCoroutine(PlaySelectionAnimation(selIndex));
    }

    private int FindSelectedCardIndex(CharacterDataSO character)
    {
        for (int i = 0; i < characterCards.Count; i++)
        {
            var cc = characterCards[i] != null ? characterCards[i].GetComponent<CharacterCard>() : null;
            if (cc != null && cc.characterData == character) return i;
        }
        return -1;
    }

    private IEnumerator PlaySelectionAnimation(int selIndex)
    {
        if (selIndex < 0 || selIndex >= characterCards.Count)
        {
            // Fallback: completar de inmediato
            isSelectionComplete = true;
            yield break;
        }

        isAnimatingSelection = true;
        // Deshabilitar interacción
        foreach (var go in characterCards)
        {
            if (go == null) continue;
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        // Referencias
        GameObject selectedGO = characterCards[selIndex];
        RectTransform selRT = selectedGO.GetComponent<RectTransform>();
        Image selImg = selectedGO.GetComponent<Image>();
        if (selImg == null) selImg = selectedGO.GetComponentInChildren<Image>(true);
        float cardX = selRT != null ? selRT.anchoredPosition.x : 0f;

        // 1) Desvanecer al otro
        for (int i = 0; i < characterCards.Count; i++)
        {
            if (i == selIndex) continue;
            var otherGO = characterCards[i];
            if (otherGO == null) continue;
            var ocg = otherGO.GetComponent<CanvasGroup>();
            if (ocg == null) ocg = otherGO.AddComponent<CanvasGroup>();
            ocg.DOFade(0f, fadeOtherDuration);
            ocg.interactable = false;
            ocg.blocksRaycasts = false;

            // Eternals del otro
            if (i < eternalItemGroups.Count)
            {
                foreach (var itemGO in eternalItemGroups[i])
                {
                    if (itemGO == null) continue;
                    var icg = itemGO.GetComponent<CanvasGroup>();
                    if (icg == null) icg = itemGO.AddComponent<CanvasGroup>();
                    icg.DOFade(0f, fadeOtherDuration);
                    icg.interactable = false;
                    icg.blocksRaycasts = false;
                }
            }
        }

        // 2) Escala y tinte verde del seleccionado
        if (selRT != null)
        {
            selRT.DOScale(selectScaleUp, selectScaleDuration).SetEase(Ease.OutQuad)
                 .OnComplete(() => selRT.DOScale(1f, selectScaleDuration).SetEase(Ease.OutQuad));
        }
        if (selImg != null)
        {
            var original = selImg.color;
            selImg.DOColor(selectedTint, selectScaleDuration)
                  .OnComplete(() => selImg.DOColor(Color.white, moveToCenterDuration));
        }

        // 3) Mover seleccionado + sus eternos al centro (manteniendo offset relativo)
        float deltaX = -cardX;
        if (selRT != null)
        {
            selRT.DOAnchorPos(new Vector2(0f, selRT.anchoredPosition.y), moveToCenterDuration).SetEase(Ease.OutCubic);
        }
        if (selIndex < eternalItemGroups.Count)
        {
            foreach (var itemGO in eternalItemGroups[selIndex])
            {
                if (itemGO == null) continue;
                var irt = itemGO.GetComponent<RectTransform>();
                if (irt != null)
                {
                    irt.DOAnchorPos(new Vector2(irt.anchoredPosition.x + deltaX, irt.anchoredPosition.y), moveToCenterDuration)
                       .SetEase(Ease.OutCubic);
                }
            }
        }

        // Esperar a que terminen animaciones
        float wait = Mathf.Max(fadeOtherDuration, selectScaleDuration * 2f, moveToCenterDuration) + 0.05f;
        yield return new WaitForSeconds(wait);

        // Asegurar que el seleccionado quede con alpha/color completos (corrección de 50% alpha)
        if (selImg != null)
        {
            selImg.DOKill();
            Color forceFull = Color.white; forceFull.a = 1f;
            selImg.color = forceFull;
            var cr = selImg.canvasRenderer; if (cr != null) cr.SetAlpha(1f);
        }
        var selCG = selectedGO.GetComponent<CanvasGroup>();
        if (selCG != null)
        {
            selCG.DOKill();
            selCG.alpha = 1f; selCG.interactable = false; selCG.blocksRaycasts = false;
        }
        // También asegurar alpha de sus objetos eternos
        if (selIndex < eternalItemGroups.Count)
        {
            foreach (var itemGO in eternalItemGroups[selIndex])
            {
                if (itemGO == null) continue;
                var icg = itemGO.GetComponent<CanvasGroup>();
                if (icg != null)
                {
                    icg.DOKill(); icg.alpha = 1f; icg.interactable = false; icg.blocksRaycasts = false;
                }
                var iimg = itemGO.GetComponent<Image>();
                if (iimg != null)
                {
                    iimg.DOKill();
                    var c2 = iimg.color; c2.a = 1f; iimg.color = c2;
                    var cr2 = iimg.canvasRenderer; if (cr2 != null) cr2.SetAlpha(1f);
                }
            }
        }

        isAnimatingSelection = false;
        isSelectionComplete = true;
    }
    
    private void ClearCards()
    {
        foreach (GameObject card in characterCards)
        {
            if (card != null)
                Destroy(card);
        }
        characterCards.Clear();
    }
    
    private void ClearEternalItems()
    {
        foreach (GameObject item in eternalItemCards)
        {
            if (item != null)
                Destroy(item);
        }
        eternalItemCards.Clear();
    }
    
    private void ApplyCharacterSelections()
    {
        foreach (var kvp in playerSelections)
        {
            int playerId = kvp.Key;
            CharacterDataSO charData = kvp.Value;
            
            PlayerData player = players.Find(p => p.playerId == playerId);
            if (player != null)
            {
                // Aplicar tipo de personaje
                player.character = charData.characterType;
                
                // Aplicar stats iniciales
                player.health = charData.startingHealth;
                player.maxHealth = charData.startingHealth;
                player.coins = charData.startingCoins;
                player.attackDamage = charData.startingAttack;
                
                // Agregar objetos eternos
                if (charData.eternalItems != null)
                {
                    foreach (CardDataSO itemSO in charData.eternalItems)
                    {
                        if (itemSO != null)
                        {
                            CardData itemData = itemSO.ToCardData();
                            itemData.isEternal = true;
                            
                            // Los items de tipo Treasure pueden ser activos o pasivos
                            if (itemData.cardType == CardType.Treasure)
                            {
                                if (itemData.isPassive)
                                {
                                    player.passiveItems.Add(itemData);
                                }
                                else
                                {
                                    player.activeItems.Add(itemData);
                                }
                            }
                        }
                    }
                }
                
                Debug.Log($"[CharacterSelection] {player.playerName} ahora es {charData.characterName} (HP:{player.health}, Coins:{player.coins}, ATK:{player.attackDamage})");

                // Asignar icono del personaje al panel del jugador si existe
                Sprite icon = (charData != null) ? charData.CharacterIcon : null;
                bool iconAssigned = false;
                if (UIRegistry.Instance != null && UIRegistry.Instance.TryGetPlayerStats(playerId, out var statsUIById) && statsUIById != null)
                {
                    statsUIById.SetCharacterIcon(icon);
                    iconAssigned = true;
                }
                else if (UIRegistry.Instance != null && UIRegistry.Instance.TryGetPlayerStats(currentPlayerIndex, out var statsUIByIndex) && statsUIByIndex != null)
                {
                    // Fallback por índice de turno actual (por si playerId/index no coinciden en escena)
                    statsUIByIndex.SetCharacterIcon(icon);
                    iconAssigned = true;
                }
                else
                {
                    // Fallback final: buscar cualquier PlayerStatsUI cuyo playerIndex coincida con playerId
                    var allStats = FindObjectsOfType<PlayerStatsUI>(includeInactive: true);
                    foreach (var stats in allStats)
                    {
                        if (stats != null && stats.playerIndex == player.playerId)
                        {
                            stats.SetCharacterIcon(icon);
                            iconAssigned = true;
                            break;
                        }
                    }
                }

                if (!iconAssigned)
                {
                    Debug.LogWarning($"[CharacterSelection] No se pudo asignar el icono del personaje a Player {player.playerId}. Verifica que PlayerStatsUI esté registrado en UIRegistry con playerIndex={player.playerId}.");
                }
                if (icon == null)
                {
                    Debug.LogWarning($"[CharacterSelection] CharacterIcon no asignado en '{charData?.characterName}'. Asigna un Sprite en el SO del personaje para mostrar el icono en el panel.");
                }
            }
        }

        // Actualizar board displays si está presente
        var boardManager = FindObjectOfType<BoardUIManager>();
        if (boardManager != null)
        {
            boardManager.TrySetup();
            Debug.Log("[CharacterSelection] BoardUIManager actualizado con las selecciones de personajes.");
        }
    }
}

// Utilidades parciales para ajustar RectTransforms
partial class CharacterSelectionUI
{
    private readonly List<TurnTimerUI> turnTimersToRestore = new List<TurnTimerUI>();

    private void EnsureStretchFull(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    private void SetAllPlayerStatsVisible(bool visible)
    {
        var allStats = FindObjectsOfType<PlayerStatsUI>(includeInactive: true);
        foreach (var stats in allStats)
        {
            if (stats == null) continue;
            stats.gameObject.SetActive(visible);
        }
    }

    private void PopAllPlayerUI()
    {
        var allStats = FindObjectsOfType<PlayerStatsUI>(includeInactive: true);
        float delay = 0f;
        foreach (var stats in allStats)
        {
            if (stats == null || !stats.gameObject.activeInHierarchy) continue;
            var rt = stats.GetComponent<RectTransform>();
            if (rt == null) continue;
            rt.DOKill();
            // Conservar escala original y evitar overshoot
            Vector3 baseScale = rt.localScale;
            rt.localScale = baseScale * 0.95f;
            rt.DOScale(baseScale, 0.28f).SetEase(Ease.OutQuad).SetDelay(delay);
            delay += 0.05f;
        }
    }

    private void LogTextState(TMP_Text text, string name)
    {
        if (text == null)
        {
            Debug.LogWarning($"[CharacterSelectionUI] {name} no asignado");
            return;
        }
        var rt = text.rectTransform;
        Debug.Log($"[CharacterSelectionUI] {name}: activeSelf={text.gameObject.activeSelf}, activeInHierarchy={text.gameObject.activeInHierarchy}, pos={rt.anchoredPosition}, anchors=({rt.anchorMin}->{rt.anchorMax}), pivot={rt.pivot}");
    }

    private void EnsureTextRendering(TMP_Text text)
    {
        if (text == null) return;
        // Habilitar componente y alfa
        text.enabled = true;
        var c = text.color; c.a = 1f; text.color = c;
        var cr = text.canvasRenderer; if (cr != null) cr.SetAlpha(1f);
        // Evitar que bloquee raycasts si está delante
        text.raycastTarget = false;
        // Empujar al frente dentro de su contenedor para asegurar visibilidad
        text.transform.SetAsLastSibling();

        // Log de alpha efectiva por CanvasGroups padres
        float effective = GetEffectiveCanvasGroupAlpha(text.transform);
        if (effective < 1f)
        {
            Debug.LogWarning($"[CharacterSelectionUI] {text.name} alpha efectiva por CanvasGroup={effective}. Puede estar atenuado por padres.");
        }
    }

    private float GetEffectiveCanvasGroupAlpha(Transform t)
    {
        float a = 1f;
        var groups = t.GetComponentsInParent<CanvasGroup>(true);
        foreach (var g in groups)
        {
            if (g != null && g.enabled)
            {
                a *= g.alpha;
            }
        }
        return a;
    }

    private void SetSelectionTimerVisible(bool visible)
    {
        // Visibilidad del TMP_Text de fallback
        if (timerText != null && timerText.gameObject.activeSelf != visible)
            timerText.gameObject.SetActive(visible);
        // Visibilidad del TurnTimerUI dedicado a selección
        if (selectionTurnTimerUI != null && selectionTurnTimerUI.gameObject.activeSelf != visible)
            selectionTurnTimerUI.gameObject.SetActive(visible);
    }

    private void DisableOtherTurnTimers()
    {
        turnTimersToRestore.Clear();
        var timers = FindObjectsOfType<TurnTimerUI>(includeInactive: true);
        foreach (var t in timers)
        {
            if (t == null) continue;
            if (selectionTurnTimerUI != null && t == selectionTurnTimerUI) continue; // mantener el de selección
            // Si está activo, desactivarlo y recordarlo para restaurar luego
            if (t.gameObject.activeSelf)
            {
                t.gameObject.SetActive(false);
                turnTimersToRestore.Add(t);
                Debug.Log($"[CharacterSelectionUI] Ocultando TurnTimerUI: {t.gameObject.name}");
            }
        }
    }

    private void RestoreOtherTurnTimers()
    {
        foreach (var t in turnTimersToRestore)
        {
            if (t == null) continue;
            if (!t.gameObject.activeSelf)
            {
                t.gameObject.SetActive(true);
                Debug.Log($"[CharacterSelectionUI] Restaurando TurnTimerUI: {t.gameObject.name}");
            }
        }
        turnTimersToRestore.Clear();
    }
}

/// <summary>
/// Componente para cartas de personaje individuales
/// </summary>
public partial class CharacterCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public CharacterDataSO characterData;
    public CharacterSelectionUI selectionUI;
    
    private Button button;
    private RectTransform rt;
    private Tween hoverTween;
    private Vector3 baseScale = Vector3.one;
    
    void Awake()
    {
        rt = GetComponent<RectTransform>();
        if (rt != null) baseScale = rt.localScale;
        button = GetComponent<Button>();
        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
        }
        
        button.onClick.AddListener(OnClick);
    }
    
    void OnDisable()
    {
        if (hoverTween != null && hoverTween.IsActive()) hoverTween.Kill();
        if (rt != null) rt.localScale = baseScale;
    }
    
    void OnClick()
    {
        if (selectionUI != null && characterData != null)
        {
            selectionUI.OnCharacterClicked(this);
        }
    }
}

// Hover effect handlers
public partial class CharacterCard
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (rt == null) return;
        if (hoverTween != null && hoverTween.IsActive()) hoverTween.Kill();
        hoverTween = rt.DOScale(baseScale * 1.08f, 0.12f).SetEase(Ease.OutQuad);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (rt == null) return;
        if (hoverTween != null && hoverTween.IsActive()) hoverTween.Kill();
        hoverTween = rt.DOScale(baseScale, 0.12f).SetEase(Ease.OutQuad);
    }
}
