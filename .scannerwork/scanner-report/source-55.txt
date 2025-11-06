using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class PlayerHandUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Configuración del abanico")]
    public float fanRadius = 300f; // (reservado si luego usamos arco real)
    [Tooltip("Ángulo máximo total del abanico (para pocas cartas)")]
    public float maxAngleRange = 60f;
    [Tooltip("Ángulo mínimo total del abanico (para muchas cartas)")]
    public float minAngleRange = 10f;
    [Tooltip("Espaciado máximo entre cartas (para pocas cartas)")]
    public float maxCardSpacing = 120f;
    [Tooltip("Espaciado mínimo entre cartas (para muchas cartas)")]
    public float minCardSpacing = 35f;

    [Header("Animación de Hover Base")]
    public float baseHoverLiftY = 30f; // cuánto se levantan todas las cartas al hacer hover en la mano
    public float hoverDuration = 0.3f;
    
    private List<RectTransform> cards = new List<RectTransform>();
    private Dictionary<RectTransform, Vector2> basePositions = new Dictionary<RectTransform, Vector2>(); // Posiciones originales sin hover
    private bool isHovering = false;
    private Canvas canvas;
    private Camera canvasCamera;
    private int boundPlayerId = -1; // Jugador cuya mano mostramos actualmente (simulación)

    // Exponer el estado de hover para otras clases (solo lectura)
    public bool IsHovering => isHovering;

    void Start()
    {
        // Obtener referencias al Canvas
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            canvasCamera = canvas.worldCamera;
        }
        
        // Suscribirse a eventos del GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCardDrawn += HandleCardDrawn;
            GameManager.Instance.OnCardPlayed += HandleCardPlayed;
            GameManager.Instance.OnCardDiscarded += HandleCardDiscarded;
        }
        
        // Registrar todas las cartas hijas (cada carta debe tener un RectTransform)
        foreach (Transform child in transform)
        {
            if (child.TryGetComponent(out RectTransform rect))
            {
                cards.Add(rect);
                
                // Asegurarse de que cada carta tenga el tracker
                if (rect.GetComponent<CardPositionTracker>() == null)
                {
                    rect.gameObject.AddComponent<CardPositionTracker>();
                }
            }
        }

        ArrangeCards();
    }

    void OnDestroy()
    {
        // Desuscribirse de eventos al destruir
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCardDrawn -= HandleCardDrawn;
            GameManager.Instance.OnCardPlayed -= HandleCardPlayed;
            GameManager.Instance.OnCardDiscarded -= HandleCardDiscarded;
        }
    }

    public void BindToPlayer(PlayerData player)
    {
        boundPlayerId = (player != null) ? player.playerId : -1;
    }

    private bool IsBoundPlayer(PlayerData player)
    {
        return player != null && boundPlayerId >= 0 && player.playerId == boundPlayerId;
    }

    /// <summary>
    /// Elimina todos los elementos visuales de la mano y limpia estructuras internas.
    /// </summary>
    public void ResetHandVisual()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        cards.Clear();
        basePositions.Clear();
        isHovering = false;
    }

    /// <summary>
    /// Maneja el evento cuando se roba una carta (ya está en la mano del jugador)
    /// </summary>
    private void HandleCardDrawn(PlayerData player, CardData cardData)
    {
        // Este evento se dispara DESPUÉS de que DrawCardController añade la carta visual
        // Solo necesitamos reorganizar si es necesario
        Debug.Log($"[PlayerHandUI] {player.playerName} drew {cardData.cardName}");
    }

    private void HandleCardPlayed(PlayerData player, CardData cardData)
    {
        // Solo si corresponde al jugador actualmente bindeado a esta mano
        if (!IsBoundPlayer(player)) return;
        TryRemoveCardFromHandUI(cardData);
    }

    private void HandleCardDiscarded(PlayerData player, CardData cardData)
    {
        // En caso de descartes forzosos (o por si OnCardPlayed no llegó), solo si corresponde al jugador bindeado
        if (!IsBoundPlayer(player)) return;
        TryRemoveCardFromHandUI(cardData);
    }

    private void TryRemoveCardFromHandUI(CardData cardData)
    {
        if (cardData == null) return;

        RectTransform toRemove = null;
        CardUI uiComp = null;
        foreach (var rect in cards)
        {
            uiComp = rect != null ? rect.GetComponent<CardUI>() : null;
            if (uiComp != null && uiComp.GetCardData() == cardData)
            {
                toRemove = rect;
                break;
            }
        }

        if (toRemove == null) return;

        // Si es una carta de Loot usada, animar hacia la pila de descarte usando el sprite de la carta
        if (cardData.cardType == CardType.Loot && GameManager.Instance != null)
        {
            try
            {
                // Calcular centro en pantalla de la carta
                if (canvas == null) canvas = GetComponentInParent<Canvas>();
                if (canvas != null && canvasCamera == null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    canvasCamera = canvas.worldCamera;
                }
                Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(canvasCamera != null ? canvasCamera : Camera.main, toRemove.position);
                GameManager.Instance.AnimateCardToDiscard(cardData, screenPos);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[PlayerHandUI] No se pudo iniciar animación a descarte: {ex.Message}");
            }
        }

        // Limpieza de estado visual/hover
        var hover = toRemove.GetComponent<CardHover>();
        if (hover != null) hover.ResetHover();
        toRemove.DOKill();

        // Animación de salida breve
        Vector3 endScale = toRemove.localScale * 0.8f;
        float dur = 0.15f;
        toRemove.DOScale(endScale, dur).SetEase(Ease.InQuad).OnComplete(() =>
        {
            // Eliminar de estructuras y destruir
            cards.Remove(toRemove);
            basePositions.Remove(toRemove);
            if (toRemove != null) Destroy(toRemove.gameObject);
            ArrangeCards();
        });
    }

    private float ComputeAngleRange(int count)
    {
        if (count <= 1) return 0f;
        // A más cartas, menor ángulo total
        float t = Mathf.InverseLerp(2f, 12f, Mathf.Clamp(count, 2, 12));
        return Mathf.Lerp(maxAngleRange, minAngleRange, t);
    }

    private float ComputeCardSpacing(int count)
    {
        if (count <= 1) return maxCardSpacing;
        // A más cartas, menor espaciado
        float t = Mathf.InverseLerp(2f, 12f, Mathf.Clamp(count, 2, 12));
        return Mathf.Lerp(maxCardSpacing, minCardSpacing, t);
    }

    /// <summary>
    /// Agrega una nueva carta (RectTransform) a la mano y actualiza el abanico.
    /// Asegura que tenga el CardPositionTracker.
    /// </summary>
    public void AddCardToHand(RectTransform newCard)
    {
        if (newCard == null) return;
        if (!cards.Contains(newCard))
        {
            cards.Add(newCard);
        }

        // Asegurar tracker de posiciones
        if (newCard.GetComponent<CardPositionTracker>() == null)
        {
            newCard.gameObject.AddComponent<CardPositionTracker>();
        }

        ArrangeCards();
    }

    public void ArrangeCards()
    {
        if (cards.Count == 0) return;

        float mid = (cards.Count - 1) / 2f;

        float dynamicAngleRange = ComputeAngleRange(cards.Count);
        float dynamicSpacing = ComputeCardSpacing(cards.Count);
        float angleStep = (cards.Count > 1) ? (dynamicAngleRange / (cards.Count - 1)) : 0f;

        for (int i = 0; i < cards.Count; i++)
        {
            RectTransform card = cards[i];

            // Calcula posición horizontal (desplazamiento centrado)
            float xOffset = (i - mid) * dynamicSpacing;

            // Calcula rotación en forma de abanico
            float angle = -(i - mid) * angleStep;

            // Calcular y guardar la posición base (sin hover)
            Vector2 basePos = new Vector2(xOffset, Mathf.Abs(i - mid) * -5f);
            basePositions[card] = basePos;

            // Actualizar el tracker de posiciones
            CardPositionTracker tracker = card.GetComponent<CardPositionTracker>();
            if (tracker != null)
            {
                tracker.basePosition = basePos;
                tracker.baseHoverPosition = new Vector2(basePos.x, basePos.y + baseHoverLiftY);
            }

            // Cancelar animaciones previas
            card.DOKill();

            // Aplicar posición y rotación
            card.anchoredPosition = basePos;
            card.localRotation = Quaternion.Euler(0, 0, angle);
        }

        // Asegurar orden de apilado: izquierda debajo, derecha encima
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].SetSiblingIndex(i);
        }
    }
    public void CardHovered(CardUI card)
    {
        // Por ejemplo, puedes levantar la carta o mostrar su detalle
        Debug.Log($"Carta {card.name} fue resaltada.");
    }

    public void CardClicked(CardUI card)
    {
        // Mostrar carta ampliada en el centro, o ejecutar su acción
        Debug.Log($"Carta {card.name} fue seleccionada.");

        // Notificar al GameManager por si estamos en selección de descarte por muerte
        if (GameManager.Instance != null && card != null)
        {
            var data = card.GetCardData();
            if (data != null)
            {
                GameManager.Instance.NotifyHandCardClicked(data);
            }
        }
    }

    // Cuando el mouse entra en el área de la mano, levantar todas las cartas
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isHovering) return;
        isHovering = true;
        
        foreach (RectTransform card in cards)
        {
            // Cancelar animaciones previas
            card.DOKill();
            
            // Levantar cada carta desde su posición base
            if (basePositions.TryGetValue(card, out Vector2 basePos))
            {
                Vector2 hoverPos = new Vector2(basePos.x, basePos.y + baseHoverLiftY);
                card.DOAnchorPos(hoverPos, hoverDuration).SetEase(Ease.OutQuad);
                
                // Actualizar la posición de hover base en el tracker
                CardPositionTracker tracker = card.GetComponent<CardPositionTracker>();
                if (tracker != null)
                {
                    tracker.baseHoverPosition = hoverPos;
                }
                
                // Si la carta está seleccionada, aplicar el hover individual adicional
                CardUI cardUI = card.GetComponent<CardUI>();
                if (cardUI != null && cardUI.IsSelected)
                {
                    CardHover cardHover = card.GetComponent<CardHover>();
                    if (cardHover != null)
                    {
                        // Reactivar el hover individual inmediatamente (sin delay)
                        // Usamos Invoke con tiempo 0 para que se ejecute después de que termine el frame actual
                        cardHover.Invoke("ForceHover", hoverDuration);
                    }
                }
            }
        }
        
        // Asegurar orden de apilado antes de activar hover individual
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].SetSiblingIndex(i);
        }
        
        // Detectar si el cursor ya está sobre una carta y activar su hover individual
        Invoke(nameof(ActivateCardUnderCursor), 0.01f);
    }

    private void ActivateCardUnderCursor()
    {
        // Buscar la carta bajo el cursor
        foreach (RectTransform card in cards)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(card, Input.mousePosition, canvasCamera))
            {
                CardHover cardHover = card.GetComponent<CardHover>();
                if (cardHover != null)
                {
                    // Forzar hover en esta carta
                    cardHover.ForceHover();
                    break;
                }
            }
        }
    }

    // Cuando el mouse sale del área de la mano, bajar todas las cartas
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isHovering) return;
        isHovering = false;
        
        // Cancelar cualquier invocación pendiente
        CancelInvoke(nameof(ActivateCardUnderCursor));
        
        // NO ocultar el preview - se mantiene hasta presionar ESC o seleccionar otra carta
        // CardPreviewUI.Instance.HidePreview(); <- ELIMINADO
        
        foreach (RectTransform card in cards)
        {
            // Resetear hover individual si existe (excepto cartas seleccionadas)
            CardHover cardHover = card.GetComponent<CardHover>();
            if (cardHover != null)
            {
                // Cancelar cualquier invocación pendiente de ForceHover para evitar re-levantar tras salir
                cardHover.CancelInvoke("ForceHover");
                cardHover.ResetHover();
            }
            
            // Cancelar animaciones previas
            card.DOKill();
            
            // Volver a la posición base (sin hover)
            if (basePositions.TryGetValue(card, out Vector2 basePos))
            {
                card.DOAnchorPos(basePos, hoverDuration).SetEase(Ease.InQuad);
            }
        }

        // Restablecer orden de apilado base tras salir del hover de la mano
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].SetSiblingIndex(i);
        }
    }
}
