using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Muestra la carta superior de una pila de descarte (por defecto, Loot) en el tablero/Canvas.
/// Puede renderizar en un Image (UI) o en un SpriteRenderer (mundo). Usa el primero que esté asignado.
/// </summary>
public class DiscardPileUI : MonoBehaviour
{
    [Header("Role")]
    [Tooltip("Marca si este componente representa una pila de descarte (no el mazo de robar)")]
    public bool isDiscardPile = true;
    [Header("Pile Settings")]
    [Tooltip("Tipo de pila de descarte a mostrar")] public CardType pileType = CardType.Loot;

    [Header("Render Targets (UI/Mundo)")]
    [Tooltip("Si se usa UI Canvas, asigna un Image")] public Image uiImage;
    [Tooltip("SpriteRenderer donde dibujar la carta superior (mundo)")] public SpriteRenderer worldRenderer;

    [Header("Background (Mundo opcional)")]
    [Tooltip("Renderer de fondo (p.ej. el 'Square' negro semitransparente)")] public SpriteRenderer backgroundRenderer;
    [Tooltip("Crear automáticamente un hijo 'TopCard' si no hay renderer asignado")] public bool autoCreateTopRenderer = true;
    [Tooltip("Offset de orden para que la carta quede por encima del fondo")] public int sortingOrderOffset = 1;

    [Header("Visuals")]
    [Tooltip("Sprite a mostrar cuando la pila está vacía (opcional)")] public Sprite emptySprite;
    [Tooltip("Mostrar dorso si hay al menos una carta pero no hay sprite frontal")] public Sprite fallbackBackSprite;

    [Header("Fitting")]
    [Tooltip("Ajustar la carta superior al rectángulo del fondo (mundo)")] public bool fitToBackground = true;
    [Tooltip("Factor de relleno dentro del fondo (1 = cubrir exacto, <1 = con margen)")] [Range(0.5f, 1.0f)] public float fitPadding = 0.92f;
    [Tooltip("Mantener proporción (recomendado)")] public bool preserveAspect = true;

    [Header("Sorting Overrides (opcional)")]
    [Tooltip("Forzar capa/orden de render para que la pila quede por debajo de la UI")] public bool overrideSorting = false;
    [Tooltip("Nombre de la Sorting Layer a usar (p.ej. 'Board' o 'Default')")] public string overrideSortingLayerName = "Default";
    [Tooltip("Orden base dentro de la capa (TopCard usará este + offset)")] public int overrideSortingOrderBase = 0;
    [Tooltip("Aplicar también al fondo (Square)")] public bool applyOverrideToBackground = false;

    [Header("Interaction")]
    [Tooltip("Permitir abrir un preview al hacer click en la pila")] public bool enableClickPreview = true;
    [Tooltip("Añadir un BoxCollider2D al objeto de la pila si no existe para recibir clicks")] public bool addColliderIfMissing = true;

    [Header("Hover")]
    [Tooltip("Efecto de hover al pasar el mouse sobre la pila")] public bool enableHover = true;
    [Range(1.0f, 1.2f)] public float hoverScale = 1.05f;
    [Tooltip("Duración del tween de hover")] public float hoverTweenDuration = 0.08f;

    private bool subscribed = false;
    private Vector3 baseTopCardScale = Vector3.one;

    private void Awake()
    {
        EnsureWorldRenderer();
        EnsureColliderForClick();
    }

    private void Start()
    {
        TrySubscribe();
        // Registrar en UIRegistry solamente si es pila de descarte
        if (UIRegistry.Instance != null && isDiscardPile)
        {
            UIRegistry.Instance.RegisterDiscardPile(pileType, this);
        }
    }

    private void OnEnable()
    {
        TrySubscribe();
        RefreshTop();
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCardDiscarded -= HandleCardDiscarded;
        }
        if (UIRegistry.Instance != null && isDiscardPile)
        {
            UIRegistry.Instance.UnregisterDiscardPile(pileType, this);
        }
    }

    private void OnValidate()
    {
        // En el editor, intentar preparar el renderer si falta
        if (!Application.isPlaying)
        {
            EnsureWorldRenderer();
        }
        else
        {
            // Mantener actualizado el registro si cambia el tipo/flag en runtime
            if (UIRegistry.Instance != null)
            {
                if (isDiscardPile)
                    UIRegistry.Instance.RegisterDiscardPile(pileType, this);
                else
                    UIRegistry.Instance.UnregisterDiscardPile(pileType, this);
            }
        }
    }

    private void EnsureWorldRenderer()
    {
        // 1) Asegurar background si no está asignado
        if (backgroundRenderer == null)
        {
            var renderers = GetComponentsInChildren<SpriteRenderer>(true);
            if (renderers != null && renderers.Length > 0)
            {
                if (renderers.Length == 1)
                {
                    backgroundRenderer = renderers[0];
                }
                else
                {
                    // Intentar por nombre
                    foreach (var r in renderers)
                    {
                        if (r != null && r.name.ToLower().Contains("square"))
                        {
                            backgroundRenderer = r;
                            break;
                        }
                    }
                    // Si no encontramos por nombre, elegir el de menor sortingOrder como fondo
                    if (backgroundRenderer == null)
                    {
                        SpriteRenderer min = renderers[0];
                        foreach (var r in renderers)
                        {
                            if (r.sortingOrder < min.sortingOrder) min = r;
                        }
                        backgroundRenderer = min;
                    }
                }
            }
        }

        // 2) Asegurar worldRenderer (TopCard)
        if (worldRenderer == null)
        {
            // Intentar localizar un hijo llamado "TopCard"
            var topChild = transform.Find("TopCard");
            if (topChild != null)
            {
                worldRenderer = topChild.GetComponent<SpriteRenderer>();
            }
        }

        if (worldRenderer == null && backgroundRenderer != null && autoCreateTopRenderer)
        {
            var go = new GameObject("TopCard");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            worldRenderer = go.AddComponent<SpriteRenderer>();

            // Asignar sorting según overrides o copiar del fondo
            ApplySortingLayers();
        }

        // 3) Si aún no hay renderer, elegir cualquiera que no sea el fondo como top
        if (worldRenderer == null)
        {
            var renderers = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var r in renderers)
            {
                if (backgroundRenderer != null && r == backgroundRenderer) continue;
                worldRenderer = r;
                break;
            }
        }
    }

    private void TrySubscribe()
    {
        if (GameManager.Instance == null) return;
        if (subscribed) return;
        GameManager.Instance.OnCardDiscarded -= HandleCardDiscarded; // evitar dobles
        GameManager.Instance.OnCardDiscarded += HandleCardDiscarded;
        subscribed = true;
    }

    private void Update()
    {
        // Si el GameManager aparece tarde, intentar suscribirnos
        if (!subscribed && GameManager.Instance != null)
        {
            TrySubscribe();
            // Refrescar por si se perdieron eventos anteriores
            RefreshTop();
        }
    }

    private void HandleCardDiscarded(PlayerData player, CardData card)
    {
        if (card == null) return;
        if (card.cardType != pileType) return;
        // Si el GameManager está suprimiendo el refresco visual (hay una animación en curso), no actualizar aún
        if (GameManager.Instance != null && GameManager.Instance.ShouldSuppressDiscardVisuals(pileType))
        {
            return;
        }
        // Actualizar directamente con la carta recién descartada
        SetSpriteForCard(card);
    }

    /// <summary>
    /// Fuerza actualizar la vista con la carta superior actual de la pila
    /// </summary>
    public void RefreshTop()
    {
        if (GameManager.Instance == null)
        {
            SetSprite(emptySprite);
            return;
        }
        var top = GameManager.Instance.GetTopDiscard(pileType);
        if (top != null)
        {
            SetSpriteForCard(top);
        }
        else
        {
            SetSprite(emptySprite);
        }
    }

    private void SetSpriteForCard(CardData card)
    {
        Sprite front = null;
        // Priorizar referencia directa
        if (card.frontSprite != null)
        {
            front = card.frontSprite;
        }
        // Si no hay referencia directa, intentar por path
        if (front == null && !string.IsNullOrEmpty(card.frontSpritePath))
        {
            front = Resources.Load<Sprite>(card.frontSpritePath);
        }

        if (front != null)
        {
            SetSprite(front);
            FitTopCardToBackground();
            CaptureBaseTopScale();
        }
        else
        {
            // Fallback: dorso o vacío
            SetSprite(fallbackBackSprite != null ? fallbackBackSprite : emptySprite);
            // No ajustar si no hay carta real
        }
    }

    private void SetSprite(Sprite sprite)
    {
        if (uiImage != null)
        {
            uiImage.sprite = sprite;
            uiImage.enabled = sprite != null;
        }
        if (worldRenderer != null)
        {
            // Aplicar política de sorting en cada actualización de sprite
            ApplySortingLayers();

            worldRenderer.sprite = sprite;
            // No tocar el fondo: solo deshabilitar el top si no hay sprite
            worldRenderer.enabled = sprite != null;
            if (sprite == null)
            {
                ResetHoverScale();
            }
        }
    }

    private void ApplySortingLayers()
    {
        if (worldRenderer == null) return;

        if (overrideSorting)
        {
            // Forzar capa/orden conocidos para mantenernos por debajo de la UI
            worldRenderer.sortingLayerName = overrideSortingLayerName;
            worldRenderer.sortingOrder = overrideSortingOrderBase + sortingOrderOffset;
            if (applyOverrideToBackground && backgroundRenderer != null)
            {
                backgroundRenderer.sortingLayerName = overrideSortingLayerName;
                backgroundRenderer.sortingOrder = overrideSortingOrderBase;
            }
        }
        else if (backgroundRenderer != null)
        {
            // Copiar del fondo y elevar el orden
            worldRenderer.sortingLayerID = backgroundRenderer.sortingLayerID;
            worldRenderer.sortingLayerName = backgroundRenderer.sortingLayerName;
            worldRenderer.sortingOrder = backgroundRenderer.sortingOrder + sortingOrderOffset;
        }
    }

    private void EnsureColliderForClick()
    {
        if (!enableClickPreview || !addColliderIfMissing) return;
        // Usar un collider en el objeto raíz para capturar OnMouseDown fácilmente
        var col = GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
        }
        // Ajustar el collider al tamaño del fondo
        if (backgroundRenderer != null)
        {
            // bounds.size está en mundo. Convertimos a espacio local del padre
            Vector2 worldSize = backgroundRenderer.bounds.size;
            Vector3 lossy = transform.lossyScale;
            float sx = Mathf.Approximately(lossy.x, 0f) ? 1f : Mathf.Abs(lossy.x);
            float sy = Mathf.Approximately(lossy.y, 0f) ? 1f : Mathf.Abs(lossy.y);
            col.size = new Vector2(worldSize.x / sx, worldSize.y / sy);
            col.offset = Vector2.zero;
        }
    }

    private void OnMouseDown()
    {
        if (!enableClickPreview) return;
        if (TargetingManager.Instance != null && TargetingManager.Instance.IsTargeting)
        {
            // No abrir preview durante targeting
            return;
        }
        if (GameManager.Instance == null || CardPreviewUI.Instance == null) return;
        var top = GameManager.Instance.GetTopDiscard(pileType);
        if (top == null) return;
        CardPreviewUI.Instance.ShowCardData(top, grayscale: true);
    }

    private void OnMouseEnter()
    {
        if (!enableHover) return;
        if (TargetingManager.Instance != null && TargetingManager.Instance.IsTargeting) return;
        if (worldRenderer == null || !worldRenderer.enabled || worldRenderer.sprite == null) return;
        worldRenderer.transform.DOKill();
        worldRenderer.transform.DOScale(baseTopCardScale * hoverScale, hoverTweenDuration).SetEase(Ease.OutQuad);
    }

    private void OnMouseExit()
    {
        if (!enableHover) return;
        if (worldRenderer == null) return;
        worldRenderer.transform.DOKill();
        worldRenderer.transform.DOScale(baseTopCardScale, hoverTweenDuration).SetEase(Ease.InQuad);
    }

    private void CaptureBaseTopScale()
    {
        if (worldRenderer != null)
        {
            baseTopCardScale = worldRenderer.transform.localScale;
        }
    }

    private void ResetHoverScale()
    {
        if (worldRenderer != null)
        {
            worldRenderer.transform.DOKill();
            worldRenderer.transform.localScale = baseTopCardScale;
        }
    }

    private void FitTopCardToBackground()
    {
        if (!fitToBackground) return;
        if (worldRenderer == null || backgroundRenderer == null) return;
        if (worldRenderer.sprite == null || backgroundRenderer.sprite == null) return;

        // Tamaños en unidades de mundo (sin escala del objeto)
        Vector2 bgSize = backgroundRenderer.bounds.size; // ya incluye escala del fondo
        Vector2 cardSize = worldRenderer.sprite.bounds.size; // tamaño del sprite sin escala

        if (bgSize.x <= 0f || bgSize.y <= 0f) return;
        if (cardSize.x <= 0f || cardSize.y <= 0f) return;

        // Tamaño actual del TopCard en mundo = cardSize * lossyScale
        Vector3 lossy = worldRenderer.transform.lossyScale;
        float currentW = cardSize.x * Mathf.Abs(lossy.x);
        float currentH = cardSize.y * Mathf.Abs(lossy.y);
        if (currentW <= 0f || currentH <= 0f) return;

        // Objetivo con padding
        float targetW = bgSize.x * fitPadding;
        float targetH = bgSize.y * fitPadding;

        float factorX = targetW / currentW;
        float factorY = targetH / currentH;
        float factor = preserveAspect ? Mathf.Min(factorX, factorY) : 1f;

        Vector3 local = worldRenderer.transform.localScale;
        if (preserveAspect)
        {
            worldRenderer.transform.localScale = local * factor;
        }
        else
        {
            worldRenderer.transform.localScale = new Vector3(local.x * factorX, local.y * factorY, local.z);
        }
    }
}
