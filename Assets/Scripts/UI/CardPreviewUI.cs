using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.EventSystems;
using System.Linq;

/// <summary>
/// Muestra una vista ampliada de la carta seleccionada en el lado izquierdo de la pantalla
/// </summary>
public partial class CardPreviewUI : MonoBehaviour
{
    public static CardPreviewUI Instance { get; private set; }

    [Header("Referencias")]
    public Image previewImage;              // Imagen que muestra la carta ampliada
    public RectTransform previewContainer;  // Contenedor de la preview
    public Button useButton;                // Botón para usar/jugar la carta seleccionada
    public Button attackButton;             // Botón para atacar monstruo
    public Button cancelButton;             // Botón para cerrar/cancelar
    [Header("Button UI (Optional)")]
    [Tooltip("Texto del botón de usar/descartar (opcional)")]
    public TextMeshProUGUI useButtonLabel;
    [Tooltip("Fondo del botón de usar/descartar (opcional)")]
    public Image useButtonBackground;
    [Header("Materials")]
    [Tooltip("Material para mostrar la preview en escala de grises (opcional)")]
    public Material grayscaleMaterial;

    private Material _defaultPreviewMaterial;

    [Header("Configuración")]
    public Vector2 previewPosition = new Vector2(-600f, 0f); // Posición en pantalla (izquierda)
    public Vector2 previewSize = new Vector2(400f, 600f);    // Tamaño de la preview
    public float animationDuration = 0.3f;
    public Ease animationEase = Ease.OutBack;

    private CardUI currentSelectedCard;
    private MonsterSlot currentSelectedMonster;
    private bool isShowing = false;
    private Color _useButtonDefaultColor;

    // Modo de confirmación de descarte (por muerte u otros efectos)
    private bool discardSelectionMode = false;

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Configurar el contenedor
        if (previewContainer != null)
        {
            previewContainer.anchoredPosition = previewPosition;
            previewContainer.sizeDelta = previewSize;
            previewContainer.localScale = Vector3.zero; // Empezar oculto
        }

        if (useButton != null)
        {
            useButton.onClick.RemoveAllListeners();
            useButton.onClick.AddListener(OnUseClicked);
            // Autodetectar label si no está asignado
            if (useButtonLabel == null)
            {
                useButtonLabel = useButton.GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }
        if (attackButton != null)
        {
            attackButton.onClick.RemoveAllListeners();
            attackButton.onClick.AddListener(OnAttackClicked);
        }
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(HidePreview);
        }

        if (previewImage != null)
        {
            _defaultPreviewMaterial = previewImage.material;
        }

        // Ocultar botones inicialmente para evitar estados inconsistentes en escena
        if (useButton != null) useButton.gameObject.SetActive(false);
        if (attackButton != null) attackButton.gameObject.SetActive(false);
        if (cancelButton != null) cancelButton.gameObject.SetActive(false);
    }

    void Update()
    {
        // Detectar tecla ESC para cerrar la preview
        if (Input.GetKeyDown(KeyCode.Escape) && isShowing)
        {
            HidePreview();
        }

        // Ocultar al hacer click en un área sin UI (tablero libre) ni objetivo del mundo
        if (isShowing && Input.GetMouseButtonDown(0))
        {
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            bool overWorldTarget = IsPointerOverWorldTargetable();
            if (!overUI && !overWorldTarget)
            {
                HidePreview();
            }
        }

        // Si estamos mostrando una carta de mano y cambia el modo de selección, refrescar el botón Usar/Descartar
        if (isShowing && currentSelectedCard != null && useButton != null)
        {
            RefreshUseButtonForCurrentCard();
        }
    }

    private bool IsPointerOverWorldTargetable()
    {
        var cam = Camera.main;
        if (cam == null) return false;
        var mouse = Input.mousePosition;
        var world = cam.ScreenToWorldPoint(mouse);
        // 2D check
        var hits2D = Physics2D.OverlapPointAll((Vector2)world);
        if (hits2D != null)
        {
            for (int i = 0; i < hits2D.Length; i++)
            {
                if (hits2D[i] == null) continue;
                if (hits2D[i].GetComponentInParent<WorldTargetable>() != null) return true;
            }
        }
        // 3D check
        Ray ray = cam.ScreenPointToRay(mouse);
        if (Physics.Raycast(ray, out var hit3D, 1000f))
        {
            if (hit3D.collider != null && hit3D.collider.GetComponentInParent<WorldTargetable>() != null) return true;
        }
        return false;
    }

    private void RefreshUseButtonForCurrentCard()
    {
        var gm = GameManager.Instance;
        var data = currentSelectedCard != null ? currentSelectedCard.GetCardData() : null;
        if (gm == null || data == null) return;

        if (gm.IsAwaitingLootDiscardSelection())
        {
            bool valid = gm.IsValidLootDiscardCandidate(data);
            useButton.gameObject.SetActive(valid);
            useButton.interactable = valid;
            discardSelectionMode = valid;
            if (useButtonLabel != null) useButtonLabel.text = valid ? "Descartar" : "";
            if (useButtonBackground != null)
            {
                if (_useButtonDefaultColor == default(Color)) _useButtonDefaultColor = useButtonBackground.color;
                useButtonBackground.color = valid ? new Color(0.9f, 0.3f, 0.3f, 1f) : _useButtonDefaultColor;
            }
        }
        else if (gm.IsAwaitingItemDiscardSelection())
        {
            bool valid = gm.IsValidItemDiscardCandidate(data);
            useButton.gameObject.SetActive(valid);
            useButton.interactable = valid;
            discardSelectionMode = valid;
            if (useButtonLabel != null) useButtonLabel.text = valid ? "Descartar" : "";
            if (useButtonBackground != null)
            {
                if (_useButtonDefaultColor == default(Color)) _useButtonDefaultColor = useButtonBackground.color;
                useButtonBackground.color = valid ? new Color(0.9f, 0.3f, 0.3f, 1f) : _useButtonDefaultColor;
            }
        }
        else
        {
            var player = gm.GetCurrentPlayer();
            bool show = (player != null && gm.CanPerformAction(player, "PlayCard") && player.hand.Contains(data));
            useButton.gameObject.SetActive(show);
            useButton.interactable = show;
            discardSelectionMode = false;
            if (useButtonLabel != null) useButtonLabel.text = show ? "Usar" : "";
            if (useButtonBackground != null && _useButtonDefaultColor != default(Color)) useButtonBackground.color = _useButtonDefaultColor;
        }
    }

    /// <summary>
    /// Muestra la carta seleccionada en la preview
    /// </summary>
    public void ShowCard(CardUI card)
    {
        if (card == null || previewImage == null) return;

        // Si ya hay una carta seleccionada, deseleccionarla primero
        if (currentSelectedCard != null && currentSelectedCard != card)
        {
            currentSelectedCard.Deselect();
        }

        // Asegurar que no estamos en modo monstruo
        currentSelectedMonster = null;

        currentSelectedCard = card;
        isShowing = true;

        // Restaurar material default (por si venimos de una preview en gris)
        if (previewImage != null && _defaultPreviewMaterial != null)
        {
            previewImage.material = _defaultPreviewMaterial;
        }

        // Actualizar la imagen
        previewImage.sprite = card.frontSprite;

        // Animar la aparición
        previewContainer.DOKill();
        previewContainer.DOScale(Vector3.one, animationDuration).SetEase(animationEase);

        // Botón contextual (Usar o Descartar durante selección de muerte)
        if (useButton != null)
        {
            bool show = false;
            bool interact = false;
            var gm = GameManager.Instance;
            var data = card.GetCardData();
            if (gm != null && data != null)
            {
                if (gm.IsAwaitingLootDiscardSelection())
                {
                    // En selección de descarte: mostrar solo si la carta es Loot válida del jugador objetivo
                    show = gm.IsValidLootDiscardCandidate(data);
                    interact = show;
                    discardSelectionMode = show;
                    if (useButtonLabel != null) useButtonLabel.text = "Descartar";
                    if (useButtonBackground != null)
                    {
                        if (_useButtonDefaultColor == default(Color)) _useButtonDefaultColor = useButtonBackground.color;
                        useButtonBackground.color = new Color(0.9f, 0.3f, 0.3f, 1f);
                    }
                }
                else if (gm.IsAwaitingItemDiscardSelection())
                {
                    // En selección de descarte de objeto: mostrar solo si es item válido controlado
                    show = gm.IsValidItemDiscardCandidate(data);
                    interact = show;
                    discardSelectionMode = show;
                    if (useButtonLabel != null) useButtonLabel.text = "Descartar";
                    if (useButtonBackground != null)
                    {
                        if (_useButtonDefaultColor == default(Color)) _useButtonDefaultColor = useButtonBackground.color;
                        useButtonBackground.color = new Color(0.9f, 0.3f, 0.3f, 1f);
                    }
                }
                else
                {
                    var player = gm.GetCurrentPlayer();
                    show = (player != null && gm.CanPerformAction(player, "PlayCard") && player.hand.Contains(data));
                    interact = show;
                    discardSelectionMode = false;
                    if (useButtonLabel != null) useButtonLabel.text = "Usar";
                    if (useButtonBackground != null && _useButtonDefaultColor != default(Color)) useButtonBackground.color = _useButtonDefaultColor;
                }
            }
            useButton.gameObject.SetActive(show);
            useButton.interactable = interact;
        }
        // En modo carta, el botón de atacar no debe mostrarse
        if (attackButton != null)
        {
            attackButton.gameObject.SetActive(false);
        }
        if (cancelButton != null)
        {
            cancelButton.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Oculta la preview y deselecciona todas las cartas
    /// </summary>
    public void HidePreview()
    {
        if (!isShowing) return;

        isShowing = false;

        // Reset modo tienda
        shopMode = false;
        currentShopSlotIndex = -1;
        currentShopCard = null;

        // Deseleccionar la carta actual
        if (currentSelectedCard != null)
        {
            currentSelectedCard.Deselect();
            currentSelectedCard = null;
        }

        // Limpiar el monstruo actual
        currentSelectedMonster = null;

        // Animar la desaparición (restaurar material al terminar para que no recupere color durante el cierre)
        previewContainer.DOKill();
        previewContainer.DOScale(Vector3.zero, animationDuration).SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                // Solo resetear si no se abrió otra preview durante la animación
                if (!isShowing && previewImage != null && _defaultPreviewMaterial != null)
                {
                    previewImage.material = _defaultPreviewMaterial;
                }
            });

        if (useButton != null)
        {
            useButton.gameObject.SetActive(false);
        }
        discardSelectionMode = false;
        if (useButtonBackground != null && _useButtonDefaultColor != default(Color)) useButtonBackground.color = _useButtonDefaultColor;
        if (attackButton != null)
        {
            attackButton.gameObject.SetActive(false);
        }
        if (cancelButton != null)
        {
            cancelButton.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Muestra una carta arbitraria (por ejemplo, tope del descarte). Puede forzar escala de grises.
    /// </summary>
    public void ShowCardData(CardData data, bool grayscale = false)
    {
        if (data == null || previewImage == null) return;

        // Si hay una carta de la mano seleccionada, deseleccionarla primero
        if (currentSelectedCard != null)
        {
            currentSelectedCard.Deselect();
            currentSelectedCard = null;
        }

        currentSelectedMonster = null;
        isShowing = true;

        // Obtener sprite frontal
        Sprite front = data.frontSprite;
        if (front == null && !string.IsNullOrEmpty(data.frontSpritePath))
        {
            front = Resources.Load<Sprite>(data.frontSpritePath);
        }
        if (front != null)
        {
            previewImage.sprite = front;
        }

        // Material en escala de grises opcional
        if (grayscale && grayscaleMaterial != null)
        {
            previewImage.material = grayscaleMaterial;
        }
        else if (_defaultPreviewMaterial != null)
        {
            previewImage.material = _defaultPreviewMaterial;
        }

        // Animación de entrada
        previewContainer.DOKill();
        previewContainer.DOScale(Vector3.one, animationDuration).SetEase(animationEase);

        // Para cartas arbitrarias (como descarte): solo Cancelar
        if (useButton != null) useButton.gameObject.SetActive(false);
        if (attackButton != null) attackButton.gameObject.SetActive(false);
        if (cancelButton != null) cancelButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// Verifica si una carta está seleccionada actualmente
    /// </summary>
    public bool IsCardSelected(CardUI card)
    {
        return currentSelectedCard == card;
    }

    /// <summary>
    /// Muestra un monstruo en la preview con botones de ataque
    /// </summary>
    public void ShowMonster(MonsterSlot monsterSlot)
    {
        if (monsterSlot == null || monsterSlot.CurrentMonster == null || previewImage == null) return;

        // Si ya hay una carta seleccionada, deseleccionarla
        if (currentSelectedCard != null)
        {
            currentSelectedCard.Deselect();
            currentSelectedCard = null;
        }

        // Restaurar material default (por si venimos de una preview en gris)
        if (previewImage != null && _defaultPreviewMaterial != null)
        {
            previewImage.material = _defaultPreviewMaterial;
        }

    currentSelectedMonster = monsterSlot;
        isShowing = true;

        // Actualizar la imagen del monstruo
        if (monsterSlot.CurrentMonster.frontSprite != null)
        {
            previewImage.sprite = monsterSlot.CurrentMonster.frontSprite;
        }

        // Animar la aparición
        previewContainer.DOKill();
        previewContainer.DOScale(Vector3.one, animationDuration).SetEase(animationEase);

        // Mostrar botón de Atacar y Cancelar
        if (useButton != null)
        {
            useButton.gameObject.SetActive(false);
        }
        if (attackButton != null)
        {
            bool show = true;
            bool interact = false;
            var gm = GameManager.Instance;
            if (gm != null)
            {
                var player = gm.GetCurrentPlayer();
                // Si ya hay combate con OTRO monstruo, ocultar el botón
                var activeCombatSlot = gm.GetCombatSlot();
                if (activeCombatSlot != null && activeCombatSlot != monsterSlot)
                {
                    show = false;
                }
                else
                {
                    // Si no hay combate, permitir iniciar
                    if (activeCombatSlot == null)
                    {
                        interact = (player != null && gm.CanPerformAction(player, "Attack"));
                    }
                    else
                    {
                        // Misma criatura: permitir pedir siguiente tirada
                        interact = true; // si se está rodando, OnAttackClicked se encargará de ignorar
                    }
                }
            }
            attackButton.gameObject.SetActive(show);
            attackButton.interactable = interact;
        }
        if (cancelButton != null)
        {
            cancelButton.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Indica si la preview actualmente muestra a este monstruo/slot.
    /// </summary>
    public bool IsShowingMonster(MonsterSlot slot)
    {
        return isShowing && currentSelectedMonster == slot;
    }
}

// Handlers
public partial class CardPreviewUI
{
    // Modo Tienda
    private bool shopMode = false;
    private int currentShopSlotIndex = -1;
    private CardData currentShopCard = null;

    public void ShowShopTreasure(CardData data, int slotIndex)
    {
        if (data == null || previewImage == null) return;

        // Reset selecciones previas
        if (currentSelectedCard != null)
        {
            currentSelectedCard.Deselect();
            currentSelectedCard = null;
        }
        currentSelectedMonster = null;

        // Configurar imagen
        Sprite front = data.frontSprite;
        if (front == null && !string.IsNullOrEmpty(data.frontSpritePath))
        {
            front = Resources.Load<Sprite>(data.frontSpritePath);
        }
        if (front != null)
        {
            previewImage.sprite = front;
        }

        // Estado
        isShowing = true;
        shopMode = true;
        currentShopSlotIndex = slotIndex;
        currentShopCard = data;

        // Asegurar activación y visibilidad del UI
        if (!gameObject.activeInHierarchy) gameObject.SetActive(true);
        if (previewContainer != null)
        {
            var canvas = previewContainer.GetComponentInParent<Canvas>(includeInactive: true);
            if (canvas != null && !canvas.enabled) canvas.enabled = true;
            var cg = previewContainer.GetComponentInParent<CanvasGroup>(includeInactive: true);
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
            previewContainer.gameObject.SetActive(true);
            previewContainer.SetAsLastSibling();
        }
        if (previewImage != null) previewImage.enabled = true;

        // Animación de entrada
        previewContainer.DOKill();
        previewContainer.localScale = Vector3.zero;
        previewContainer.DOScale(Vector3.one, animationDuration).SetEase(animationEase);
        Debug.Log($"[CardPreviewUI] ShowShopTreasure visible. Slot {slotIndex}, Card {data.cardName}");

        // Configurar botones: Comprar / Cancelar
        if (useButton != null)
        {
            var gm = GameManager.Instance;
            var player = gm != null ? gm.GetCurrentPlayer() : null;
            bool canBuy = (gm != null && player != null && gm.CanPerformAction(player, "Buy") && player.coins >= 10);
            useButton.gameObject.SetActive(true);
            useButton.interactable = canBuy;
            if (useButtonLabel != null) useButtonLabel.text = "Comprar (10)";
            if (useButtonBackground != null && _useButtonDefaultColor != default(Color)) useButtonBackground.color = _useButtonDefaultColor;
        }
        if (attackButton != null) attackButton.gameObject.SetActive(false);
        if (cancelButton != null) cancelButton.gameObject.SetActive(true);
    }

    private void OnUseClicked()
    {
        // Si estamos en modo tienda, el botón usa compra
        if (shopMode)
        {
            if (GameManager.Instance == null) { HidePreview(); return; }
            var gm = GameManager.Instance;
            var player = gm.GetCurrentPlayer();
            if (player == null) { HidePreview(); return; }

            if (gm.TryBuyFromShopSlot(player, currentShopSlotIndex, out var reason))
            {
                HidePreview();
            }
            else
            {
                Debug.LogWarning($"[CardPreviewUI] No se pudo comprar: {reason}");
                // Mantener la preview abierta para feedback visual
            }
            return;
        }

        if (currentSelectedCard == null) return;
        if (GameManager.Instance == null) return;

        var gm2 = GameManager.Instance;
        var player2 = gm2.GetCurrentPlayer();
        var data2 = currentSelectedCard.GetCardData();
        if (data2 == null) return;

        // Si estamos en modo de selección de descarte, confirmar descarte
        if (gm2.IsAwaitingLootDiscardSelection())
        {
            if (gm2.IsValidLootDiscardCandidate(data2))
            {
                gm2.ConfirmLootDiscardSelection(data2);
                HidePreview();
            }
            return;
        }
        if (gm2.IsAwaitingItemDiscardSelection())
        {
            if (gm2.IsValidItemDiscardCandidate(data2))
            {
                gm2.ConfirmItemDiscardSelection(data2);
                HidePreview();
            }
            return;
        }

        if (player2 == null) return;
        gm2.RequestPlayCard(player2, data2);
        // Al usar la carta (ej. Bomba), cerrar la preview inmediatamente para no tapar el overlay/targeting
        HidePreview();
    }

    private void OnAttackClicked()
    {
        if (currentSelectedMonster == null || currentSelectedMonster.CurrentMonster == null) return;
        if (GameManager.Instance == null) return;

        var gm = GameManager.Instance;
        var player = gm.GetCurrentPlayer();
        if (player == null) return;

        // Si ya estamos en combate con este mismo slot, tratar el botón como "continuar" (pedir tirada)
        if (gm.IsInCombatSlot(currentSelectedMonster))
        {
            gm.RequestCombatRollNext(currentSelectedMonster);
            HidePreview();
            return;
        }

        // Delegar el combate al GameManager (maneja lock y múltiples tiradas)
        bool started = gm.BeginCombat(player, currentSelectedMonster);
        if (started)
        {
            // Atacar inmediatamente en el primer click desde la preview
            gm.RequestCombatRollNext(currentSelectedMonster);
        }

        // Ocultar la preview después de iniciar el combate
        HidePreview();
    }

    private void StartCombat(PlayerData attacker, MonsterSlot monsterSlot)
    {
        var monster = monsterSlot.CurrentMonster;
        
        // Lanzar el dado de combate (1d6)
        GameManager.Instance.RollDice(6, (rollResult) =>
        {
            Debug.Log($"[Combat] {attacker.playerName} rolled {rollResult} vs Monster's {monster.diceRequirement}+ requirement");

            // Si el resultado del dado es >= al requisito, el jugador daña al monstruo
            if (rollResult >= monster.diceRequirement)
            {
                Debug.Log($"[Combat] Hit! Monster takes {attacker.attackDamage} damage");
                
                // Animar el daño al monstruo
                monsterSlot.PlayTakeDamageAnimation();
                
                // Esperar a que termine la animación antes de aplicar el daño (evitar destruir el objeto durante tweens)
                DOVirtual.DelayedCall(0.4f, () =>
                {
                    MonsterSlotManager.Instance.DamageMonster(monsterSlot, attacker.attackDamage);
                });
            }
            else
            {
                // Si falla, el jugador recibe daño del monstruo
                Debug.Log($"[Combat] Miss! Player takes {monster.attackDamage} damage");
                
                // Animar el ataque del monstruo
                monsterSlot.PlayAttackAnimation();
                
                // Aplicar daño al jugador después de la animación
                DOVirtual.DelayedCall(0.3f, () =>
                {
                    GameManager.Instance.ChangePlayerHealth(attacker, -monster.attackDamage);
                });
            }

            // Procesar trigger de combate (si existe) después de todas las animaciones
            DOVirtual.DelayedCall(0.7f, () =>
            {
                if (monsterSlot.CurrentMonster != null) // Verificar que no fue derrotado
                {
                    MonsterSlotManager.Instance.ProcessCombatRoll(monsterSlot, attacker, rollResult);
                }
            });
        });
    }
}
