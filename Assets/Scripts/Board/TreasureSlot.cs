using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// Componente de un slot individual de tesoro en la tienda (world-space, usa SpriteRenderer).
/// Similar a MonsterSlot: se coloca en un GameObject preexistente en la escena.
/// </summary>
public class TreasureSlot : MonoBehaviour
{
    [Header("Visual (World Space)")]
    [Tooltip("SpriteRenderer donde se muestra el sprite del tesoro revelado")]
    public SpriteRenderer treasureRenderer;
    [Tooltip("Sprite para mostrar cuando el slot está vacío (placeholder)")]
    public Sprite emptySlotSprite;
    [Tooltip("Opcional: SpriteRenderer base del slot (marco). Si se asigna, el ajuste usará su tamaño real para encajar la carta dentro")]
    public SpriteRenderer baseSlotRenderer;

    [Header("Sizing")]
    [Tooltip("Ajustar el sprite para caber dentro de este tamaño (en unidades de mundo)")]
    public bool autoFitSprite = true;
    public Vector2 maxWorldSize = new Vector2(1.2f, 1.7f);
    [Tooltip("Porcentaje de padding interior al encajar la carta (0.05 = 5% por lado)")]
    [Range(0f, 0.3f)]
    public float fitPaddingPercent = 0.05f;

    [Header("Hover Animation")]
    public bool hoverScaleEnabled = true;
    public float hoverScaleFactor = 1.08f;
    public float hoverTween = 0.15f;
    
    [Header("State")]
    private CardData currentTreasure;
    private GameObject cardObject; // CardUI instanciado si se usa prefab
    private int slotIndex = -1;
    private BoxCollider2D clickCollider;
    private Vector3 baseScale = Vector3.one;
    private Vector3 fittedScale = Vector3.one; // escala después del ajuste de tamaño

    public System.Action<int> OnClicked;

    private void Awake()
    {
        // Añadir collider para detectar clicks
        clickCollider = GetComponent<BoxCollider2D>();
        if (clickCollider == null)
        {
            clickCollider = gameObject.AddComponent<BoxCollider2D>();
            clickCollider.isTrigger = false;
        }
        
        // Guardar la escala base del SLOT (GameObject padre), no del renderer
        baseScale = transform.localScale;
        
        if (treasureRenderer != null)
        {
            // La escala base del renderer debe ser (1,1,1) para que el ajuste funcione correctamente
            treasureRenderer.transform.localScale = Vector3.one;
        }

        // Dimensionar collider al tamaño del marco o al máximo configurado
        UpdateColliderFromVisual();
    }

    public void SetIndex(int index)
    {
        slotIndex = index;
    }

    public int GetIndex() => slotIndex;

    /// <summary>
    /// Asigna un tesoro revelado a este slot
    /// </summary>
    public void SetTreasure(CardData treasure, GameObject cardGO = null)
    {
        currentTreasure = treasure;
        
        // Limpiar objeto anterior si existe
        if (cardObject != null && cardObject != cardGO)
        {
            Destroy(cardObject);
        }
        
        cardObject = cardGO;
        
        // Actualizar visuales: si hay baseSlotRenderer, úsalo para el placeholder y deja treasureRenderer solo para la carta
        if (treasure != null && treasure.frontSprite != null)
        {
            // Mostrar carta en renderer de tesoro
            if (treasureRenderer != null)
            {
                treasureRenderer.sprite = treasure.frontSprite;
                treasureRenderer.color = Color.white; // sin oscurecer
                // Poner por encima del marco si existe
                if (baseSlotRenderer != null)
                {
                    treasureRenderer.sortingLayerID = baseSlotRenderer.sortingLayerID;
                    treasureRenderer.sortingOrder = baseSlotRenderer.sortingOrder + 1;
                }
                // Ajuste de tamaño
                if (autoFitSprite) FitSpriteToBounds();
            }
            // Si hay marco y venía con placeholder, mantenlo (no es necesario cambiarlo)
        }
        else
        {
            // No hay carta → mostrar placeholder de forma robusta
            bool usedBase = false;
            if (baseSlotRenderer != null)
            {
                if (emptySlotSprite != null)
                {
                    baseSlotRenderer.sprite = emptySlotSprite;
                    // Mantener el tinte del marco según Inspector
                    usedBase = true;
                }
                else
                {
                    Debug.LogWarning($"[TreasureSlot] emptySlotSprite no asignado en slot {slotIndex}, usando treasureRenderer como fallback");
                }
            }

            if (treasureRenderer != null)
            {
                if (usedBase)
                {
                    // Si el marco ya muestra el placeholder, ocultamos la carta
                    treasureRenderer.sprite = null;
                }
                else
                {
                    // Fallback: mostrar placeholder en el renderer de la carta
                    treasureRenderer.sprite = emptySlotSprite;
                }
                treasureRenderer.transform.localScale = Vector3.one;
                fittedScale = Vector3.one;
            }
        }
        
        // Si se pasó un CardUI, configurarlo (opcional)
        if (cardGO != null)
        {
            var cardUI = cardGO.GetComponent<CardUI>();
            if (cardUI != null && treasure != null)
            {
                Sprite front = treasure.frontSprite;
                if (front == null && !string.IsNullOrEmpty(treasure.frontSpritePath))
                {
                    front = Resources.Load<Sprite>(treasure.frontSpritePath);
                }
                cardUI.SetCardData(treasure, front, null);
            }
        }

        // Actualizar collider tras cualquier cambio visual
        UpdateColliderFromVisual();
    }

    public CardData GetTreasure() => currentTreasure;

    public bool HasTreasure() => currentTreasure != null;

    public void ClearTreasure()
    {
        currentTreasure = null;
        if (cardObject != null)
        {
            Destroy(cardObject);
            cardObject = null;
        }
        // Mostrar placeholder en el marco si existe, ocultar carta
        if (baseSlotRenderer != null)
        {
            baseSlotRenderer.sprite = emptySlotSprite;
        }
        if (treasureRenderer != null)
        {
            treasureRenderer.sprite = null;
            treasureRenderer.transform.localScale = Vector3.one;
            fittedScale = Vector3.one;
        }
        UpdateColliderFromVisual();
    }

    private void FitSpriteToBounds()
    {
        if (treasureRenderer == null || treasureRenderer.sprite == null) return;

        // 1) Determinar el tamaño objetivo disponible en UNIDADES DE MUNDO
        Vector2 targetWorldSize;
        if (baseSlotRenderer != null && baseSlotRenderer.sprite != null)
        {
            // Tamaño del marco en mundo = (rect/ppu) * escala mundial del renderer base
            var baseSprite = baseSlotRenderer.sprite;
            var basePPU = Mathf.Max(1f, baseSprite.pixelsPerUnit);
            var baseRect = baseSprite.rect;
            var baseLocalSize = new Vector2(baseRect.width / basePPU, baseRect.height / basePPU);
            var baseLossy = baseSlotRenderer.transform.lossyScale;
            targetWorldSize = new Vector2(baseLocalSize.x * Mathf.Abs(baseLossy.x), baseLocalSize.y * Mathf.Abs(baseLossy.y));
        }
        else
        {
            // Usar el valor de referencia configurado en el inspector
            targetWorldSize = maxWorldSize;
        }

        // Aplicar padding interior opcional (por lado). Ej: 0.05 = 5% por cada lado → 10% menos del tamaño total
        if (fitPaddingPercent > 0f)
        {
            float clampPad = Mathf.Clamp01(fitPaddingPercent);
            float scale = Mathf.Max(0.0f, 1f - (clampPad * 2f));
            targetWorldSize *= scale;
        }

        // 2) Tamaño del sprite de la carta en mundo a escala local = 1 del renderer
        var sprite = treasureRenderer.sprite;
        float ppu = Mathf.Max(1f, sprite.pixelsPerUnit);
        var rect = sprite.rect;
        Vector2 cardLocalWorldSize = new Vector2(rect.width / ppu, rect.height / ppu); // sin escala de padres ni del renderer

        // 3) Considerar la escala mundial del padre (sin la del renderer, que será lo que ajustemos)
        //    Como hemos puesto treasureRenderer.transform.localScale = (Vector3.one) en Awake, su lossyScale ahora equivale al de los padres
        var parentWorldScale = treasureRenderer.transform.lossyScale; // incluye padres, local=1 → solo padres
        Vector2 cardWorldSizeAtLocal1 = new Vector2(
            cardLocalWorldSize.x * Mathf.Abs(parentWorldScale.x),
            cardLocalWorldSize.y * Mathf.Abs(parentWorldScale.y)
        );

        if (cardWorldSizeAtLocal1.x <= 0f || cardWorldSizeAtLocal1.y <= 0f) return;

        // 4) Escala local necesaria del renderer para encajar dentro del target
        float sx = targetWorldSize.x / cardWorldSizeAtLocal1.x;
        float sy = targetWorldSize.y / cardWorldSizeAtLocal1.y;
        float s = Mathf.Min(sx, sy);

        // 5) Aplicar esa escala (permitimos crecer o decrecer para que encaje exactamente)
        fittedScale = new Vector3(s, s, 1f);
        treasureRenderer.transform.localScale = fittedScale;
    }

    private void UpdateColliderFromVisual()
    {
        if (clickCollider == null) return;

        Vector2 targetWorldSize;
        if (baseSlotRenderer != null && baseSlotRenderer.sprite != null)
        {
            // Usar tamaño del marco (bounds en mundo)
            var b = baseSlotRenderer.bounds; // en espacio mundo
            targetWorldSize = new Vector2(b.size.x, b.size.y);
        }
        else if (treasureRenderer != null && treasureRenderer.sprite != null)
        {
            var b = treasureRenderer.bounds;
            targetWorldSize = new Vector2(b.size.x, b.size.y);
        }
        else
        {
            // Fallback: usar maxWorldSize directo
            targetWorldSize = maxWorldSize;
        }

        // Convertir a espacio local del GameObject donde está el collider
        var lossy = transform.lossyScale;
        Vector2 localSize = new Vector2(
            targetWorldSize.x / Mathf.Max(0.0001f, Mathf.Abs(lossy.x)),
            targetWorldSize.y / Mathf.Max(0.0001f, Mathf.Abs(lossy.y))
        );

        clickCollider.offset = Vector2.zero;
        clickCollider.size = localSize;
    }

    private void OnMouseDown()
    {
        Debug.Log($"[TreasureSlot] OnMouseDown - HasTreasure: {HasTreasure()}, slotIndex: {slotIndex}");
        if (!HasTreasure())
        {
            // Si no hay tesoro, ocultar cualquier preview abierta
            if (CardPreviewUI.Instance != null) CardPreviewUI.Instance.HidePreview();
            return;
        }
        Debug.Log($"[TreasureSlot] Click en slot {slotIndex}. Llamando a OnClicked o fallback a preview.");
        if (OnClicked != null)
        {
            OnClicked.Invoke(slotIndex);
        }
        else if (CardPreviewUI.Instance != null)
        {
            // Fallback: abrir preview directamente si no hay suscriptores
            CardPreviewUI.Instance.ShowShopTreasure(currentTreasure, slotIndex);
        }
    }

    private void OnMouseEnter()
    {
        // Solo animación de hover (no mostrar preview)
        if (hoverScaleEnabled && treasureRenderer != null)
        {
            var sr = treasureRenderer;
            sr.transform.DOKill();
            // Escalar desde la escala ajustada actual
            sr.transform.DOScale(fittedScale * hoverScaleFactor, hoverTween).SetEase(Ease.OutSine);
        }
    }

    private void OnMouseExit()
    {
        // Revertir animación de hover a la escala ajustada
        if (treasureRenderer != null)
        {
            var sr = treasureRenderer;
            sr.transform.DOKill();
            sr.transform.DOScale(fittedScale, hoverTween).SetEase(Ease.OutSine);
        }
    }
}
