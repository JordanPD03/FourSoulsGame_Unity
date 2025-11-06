using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Configuración de la carta")]
    public Image cardImage;               // Referencia a la imagen de la carta (UI)
    public SpriteRenderer spriteRenderer; // Referencia al SpriteRenderer (World Space)
    public Sprite frontSprite;            // Imagen frontal de la carta
    public Sprite backSprite;             // Imagen del dorso (opcional)
    public bool isFaceUp = true;          // Estado: visible o volteada

    private CardData cardData;            // Datos de la carta (del GameManager)
    private RectTransform rectTransform;  // Para animaciones o movimiento
    private Vector3 originalPosition;     // Guarda la posición base
    private PlayerHandUI hand;            // Referencia al controlador de la mano
    private CardHover cardHover;          // Referencia al componente de hover
    private bool isSelected = false;      // Estado de selección
    
    [Header("Interacción")]
    [Tooltip("Tiempo máximo entre clicks para considerarlo doble click")]
    public float doubleClickThreshold = 0.25f;
    private float lastClickTime = -999f;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        hand = GetComponentInParent<PlayerHandUI>();
        cardHover = GetComponent<CardHover>();
        if (cardImage == null)
            cardImage = GetComponent<Image>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            // Si no está en este objeto, buscar en hijos
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }
        
        // Buscar SpriteRenderer si aún no está asignado
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }
        
        // Debug.Log($"[CardUI] Awake en {gameObject.name}: spriteRenderer={spriteRenderer?.name}, cardImage={cardImage?.name}");
    }

    void Start()
    {
        // Solo para cartas UI (con RectTransform)
        if (rectTransform != null)
        {
            originalPosition = rectTransform.anchoredPosition;
        }
        UpdateCardVisual();
    }

    public void SetCard(Sprite sprite, bool faceUp = true)
    {
        frontSprite = sprite;
        isFaceUp = faceUp;
        UpdateCardVisual();
    }

    /// <summary>
    /// Configura la carta desde CardData (nuevo sistema)
    /// </summary>
    public void SetCardData(CardData data, Sprite frontSpr = null, Sprite backSpr = null)
    {
        cardData = data;
        
        if (frontSpr != null)
        {
            frontSprite = frontSpr;
        }
        else if (!string.IsNullOrEmpty(data.frontSpritePath))
        {
            frontSprite = Resources.Load<Sprite>(data.frontSpritePath);
            if (frontSprite == null)
            {
                Debug.LogWarning($"[CardUI] No se pudo cargar sprite: {data.frontSpritePath}");
            }
        }
        else if (data.frontSprite != null)
        {
            // Fallback: usar referencia directa del ScriptableObject
            frontSprite = data.frontSprite;
        }
        
        if (backSpr != null)
        {
            backSprite = backSpr;
        }
        else if (!string.IsNullOrEmpty(data.backSpritePath))
        {
            backSprite = Resources.Load<Sprite>(data.backSpritePath);
        }
        else if (data.backSprite != null)
        {
            backSprite = data.backSprite;
        }
        
        isFaceUp = data.isFaceUp;
        
        // Forzar actualización visual
        if (cardImage != null)
        {
            cardImage.sprite = isFaceUp ? frontSprite : backSprite;
        }
        // Actualizar SpriteRenderer si existe (para cartas en world space, como monstruos)
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = isFaceUp ? frontSprite : backSprite;
        }
        else { }
    }

    /// <summary>
    /// Obtiene los datos de la carta
    /// </summary>
    public CardData GetCardData()
    {
        return cardData;
    }

    private void UpdateCardVisual()
    {
        if (cardImage != null)
            cardImage.sprite = isFaceUp ? frontSprite : backSprite;
        if (spriteRenderer != null)
            spriteRenderer.sprite = isFaceUp ? frontSprite : backSprite;
    }

    

    // --- EVENTOS DE INTERFAZ ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Si deseas que el PlayerHandUI reaccione:
        hand?.CardHovered(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // La animación se maneja en CardHover
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Detectar doble click para jugar cartas de Loot rápidamente
        float now = Time.unscaledTime;
        bool isDouble = (now - lastClickTime) <= doubleClickThreshold;
        lastClickTime = now;

        // Si doble click y es Loot, intentar jugarla inmediatamente (si es legal)
        if (isDouble && cardData != null && cardData.cardType == CardType.Loot && GameManager.Instance != null)
        {
            var gm = GameManager.Instance;
            // No interferir si estamos en selección de descarte (muerte) u otra selección
            if (!gm.IsAwaitingLootDiscardSelection() && !gm.IsAwaitingItemDiscardSelection())
            {
                var player = gm.GetCurrentPlayer();
                if (player != null)
                {
                    gm.RequestPlayCard(player, cardData);
                    // Cerrar cualquier preview para no tapar el overlay/targeting
                    if (CardPreviewUI.Instance != null)
                    {
                        CardPreviewUI.Instance.HidePreview();
                    }
                    return;
                }
            }
        }

        // Comportamiento normal: seleccionar y notificar
        Select();
        hand?.CardClicked(this);
    }

    /// <summary>
    /// Selecciona esta carta y muestra su preview
    /// </summary>
    public void Select()
    {
        if (isSelected) return;
        
        isSelected = true;
        
        // Mostrar en el CardPreviewUI
        if (CardPreviewUI.Instance != null)
        {
            CardPreviewUI.Instance.ShowCard(this);
        }
        
        // Mantener el hover individual activo
        if (cardHover != null)
        {
            cardHover.SetSelected(true);
        }
    }

    /// <summary>
    /// Deselecciona esta carta
    /// </summary>
    public void Deselect()
    {
        if (!isSelected) return;
        
        isSelected = false;
        
        // Desactivar el estado de selección en CardHover
        if (cardHover != null)
        {
            cardHover.SetSelected(false);
        }
    }

    /// <summary>
    /// Verifica si la carta está seleccionada
    /// </summary>
    public bool IsSelected => isSelected;
}
