using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Animación de Hover Individual")]
    public float extraHoverLiftY = 30f;   // levantamiento adicional solo para esta carta
    public float hoverScale = 1.1f;       // cuánto aumenta el tamaño
    public float duration = 0.25f;        // velocidad de la animación
    public Ease easeType = Ease.OutQuad;  // tipo de interpolación

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Vector2 positionBeforeHover;  // Guardar posición justo antes del hover
    private bool isHoveringCard = false;  // Track del estado de hover
    private CardPositionTracker positionTracker;
    private bool isSelected = false;      // Track del estado de selección
    private Outline outline;              // Referencia al componente Outline
    private Color originalOutlineColor;   // Color original del outline
    private Vector2 originalOutlineDistance; // Distancia/grosor original del outline
    private int originalSiblingIndex;     // Orden de apilado original para restaurar
    
    [Header("Selección")]
    public Color selectedOutlineColor = Color.yellow; // Color del outline cuando está seleccionada
    public Vector2 selectedOutlineDistance = new Vector2(4f, -4f); // Grosor del outline cuando está seleccionada

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
        positionTracker = GetComponent<CardPositionTracker>();
        outline = GetComponent<Outline>();
        
        // Si no existe, agregarlo
        if (positionTracker == null)
        {
            positionTracker = gameObject.AddComponent<CardPositionTracker>();
        }
        
        // Guardar valores originales del outline
        if (outline != null)
        {
            originalOutlineColor = outline.effectColor;
            originalOutlineDistance = outline.effectDistance;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isHoveringCard) return; // Evitar entradas múltiples
        isHoveringCard = true;
        
        // Guardar índice de apilado para restaurarlo al salir
        originalSiblingIndex = rectTransform.GetSiblingIndex();
        
        // Usar la posición con hover base del tracker (calculada, no la actual que puede estar en animación)
        if (positionTracker != null && positionTracker.baseHoverPosition != Vector2.zero)
        {
            positionBeforeHover = positionTracker.baseHoverPosition;
        }
        else
        {
            // Fallback: usar posición actual
            positionBeforeHover = rectTransform.anchoredPosition;
        }
        
        // Cancelar cualquier animación anterior
        rectTransform.DOKill();

        // Levantar EXTRA desde la posición con hover base
        float targetY = positionBeforeHover.y + extraHoverLiftY;
        rectTransform.DOAnchorPosY(targetY, duration).SetEase(easeType);
        rectTransform.DOScale(originalScale * hoverScale, duration).SetEase(easeType);
        
        // Traer al frente solo mientras está en hover
        rectTransform.SetAsLastSibling();
    }

    // Método público para forzar hover (usado por PlayerHandUI)
    public void ForceHover()
    {
        // Si ya está en hover, primero resetear el flag
        if (isHoveringCard)
        {
            isHoveringCard = false;
        }

        // Solo aplicar el hover individual si la mano del jugador está en estado de hover
        PlayerHandUI playerHand = GetComponentInParent<PlayerHandUI>();
        if (playerHand != null && !playerHand.IsHovering)
        {
            return; // Evita que una invocación retrasada vuelva a levantar la carta fuera del área
        }

        // Crear un PointerEventData dummy y aplicar
        PointerEventData dummyData = new PointerEventData(EventSystem.current);
        OnPointerEnter(dummyData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Restaurar orden de apilado al salir del hover
        rectTransform.SetSiblingIndex(originalSiblingIndex);

        // Si la carta está seleccionada, no bajar posición/escala, solo restaurar z-order
        if (isSelected) return;

        if (!isHoveringCard) return; // Solo salir si estábamos en hover
        isHoveringCard = false;
        
        // Regresar a la posición con hover base (no individual)
        rectTransform.DOKill();

        rectTransform.DOAnchorPos(positionBeforeHover, duration).SetEase(Ease.InQuad);
        rectTransform.DOScale(originalScale, duration).SetEase(Ease.InQuad);
    }

    // Método para resetear forzadamente (útil cuando PlayerHandUI reorganiza)
    public void ResetHover()
    {
        // Siempre limpiar el hover individual (aunque esté seleccionada)
        if (isHoveringCard)
        {
            isHoveringCard = false;
            rectTransform.DOKill();
            rectTransform.DOScale(originalScale, duration).SetEase(Ease.InQuad);
        }
    }

    /// <summary>
    /// Establece el estado de selección de la carta
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        if (selected)
        {
            // Cambiar color y grosor del outline
            if (outline != null)
            {
                outline.effectColor = selectedOutlineColor;
                outline.effectDistance = selectedOutlineDistance;
            }
            
            // Mantener el hover individual activo SOLO si la mano está en hover
            if (!isHoveringCard)
            {
                PlayerHandUI playerHand = GetComponentInParent<PlayerHandUI>();
                if (playerHand != null && playerHand.IsHovering)
                {
                    // Forzar hover si no está activo
                    PointerEventData dummyData = new PointerEventData(EventSystem.current);
                    OnPointerEnter(dummyData);
                }
            }
        }
        else
        {
            // Restaurar color y grosor original del outline
            if (outline != null)
            {
                outline.effectColor = originalOutlineColor;
                outline.effectDistance = originalOutlineDistance;
            }
            
            // Al deseleccionar, bajar la carta
            if (isHoveringCard)
            {
                isHoveringCard = false;
                rectTransform.DOKill();
                
                // Verificar si la mano está en estado de hover
                PlayerHandUI playerHand = GetComponentInParent<PlayerHandUI>();
                Vector2 targetPos;
                
                if (playerHand != null && positionTracker != null)
                {
                    // Si la mano NO está en hover, usar posición base
                    // Si la mano SÍ está en hover, usar posición con hover base
                    targetPos = positionTracker.baseHoverPosition;
                }
                else
                {
                    // Fallback
                    targetPos = positionBeforeHover;
                }
                
                rectTransform.DOAnchorPos(targetPos, duration).SetEase(Ease.InQuad);
                rectTransform.DOScale(originalScale, duration).SetEase(Ease.InQuad);
            }
        }
    }
}
