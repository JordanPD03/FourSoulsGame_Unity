using UnityEngine;
using DG.Tweening;

/// <summary>
/// Componente para objetos del mundo (con SpriteRenderer) que pueden ser objetivo de cartas.
/// Usa Collider2D y OnMouseDown en lugar del sistema de eventos UI.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class WorldTargetable : MonoBehaviour
{
    [Header("Target Info")]
    public TargetType targetType;
    public int playerIndex = -1; // Para jugadores en el mundo (si aplica)
    
    [Header("Monster Info")]
    public CardData monsterCardUI; // Referencia al CardData del monstruo (si es un slot de monstruo)

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private bool isInteractable = true;
    private bool isHighlighted = false;
    private bool isRegistered = false;

    private Color originalColor;
    private Vector3 baseScale; // escala base a la que volver en hover/highlight off

    [Header("Highlight Settings")]
    public Color highlightColor = new Color(1f, 1f, 0.5f, 1f); // Amarillo claro
    public float highlightScaleMultiplier = 1.1f;

    [Header("Hover Settings")]
    public bool enableHoverEffect = true;
    public float hoverScaleMultiplier = 1.05f;
    public float hoverAnimationDuration = 0.2f;
    private bool isHovering = false;

    [Header("Targeting Mode Animation")]
    [Tooltip("Animar con pulso cuando estás en modo targeting")]
    public bool enableTargetingPulse = true;
    public float targetingPulseScale = 1.08f;
    public float targetingPulseDuration = 0.6f;
    private Tween targetingPulseTween;

    // Double click detection
    [Header("Click Settings")]
    public float doubleClickThreshold = 0.25f;
    private float lastClickTime = -999f;

    private void Awake()
    {
        // Buscar SpriteRenderer en este GO o en hijos
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        originalColor = spriteRenderer.color;
        
        // Capturar la escala base SOLO al inicio (no cambiarla después)
        baseScale = transform.localScale;

        // Auto-añadir collider si no existe
        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        // Ajustar tamaño/offset del collider al sprite
        FitColliderToSprite();
    }    private void OnEnable()
    {
        TryRegister();
    }

    private void OnDisable()
    {
        if (isRegistered && TargetingManager.Instance != null)
        {
            TargetingManager.Instance.Unregister(this);
        }
        isRegistered = false;
    }

    private void OnValidate()
    {
        // En editor, intentar ajustar el collider si hay cambios
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        FitColliderToSprite();
    }

    private void Update()
    {
        // Registrar tardíamente si el TargetingManager aparece después
        if (!isRegistered && TargetingManager.Instance != null)
        {
            TryRegister();
        }
    }

    private void OnMouseDown()
    {
        // Si estamos en modo targeting, procesar como objetivo
        if (TargetingManager.Instance != null && TargetingManager.Instance.IsTargeting)
        {
            // Verificar permiso por tipo (más robusto que depender del estado visual de highlight)
            if (!TargetingManager.Instance.IsTargetTypeAllowed(this.targetType)) return;
            TargetingManager.Instance.TargetClicked(this);
            return;
        }

        // Si no estamos en targeting, y es un monstruo, mostrar preview
        if (targetType == TargetType.Monster)
        {
            // Si el GameManager está esperando elegir un slot para colocar un overlay desde el mazo, confirmar aquí
            var gmSel = GameManager.Instance;
            if (gmSel != null && gmSel.IsAwaitingDeckOverlayPlacement())
            {
                MonsterSlot selSlot = GetComponentInParent<MonsterSlot>();
                if (selSlot != null)
                {
                    gmSel.ConfirmDeckOverlayPlacement(selSlot);
                    return;
                }
            }

            // Buscar el MonsterSlot asociado a este objeto
            MonsterSlot slot = GetComponentInParent<MonsterSlot>();
            if (slot != null && slot.CurrentMonster != null && CardPreviewUI.Instance != null)
            {
                // Detección de doble clic para atacar/continuar combate
                float now = Time.unscaledTime;
                bool isDouble = (now - lastClickTime) <= doubleClickThreshold;
                lastClickTime = now;

                var gm = GameManager.Instance;
                if (gm != null)
                {
                    var player = gm.GetCurrentPlayer();
                    if (isDouble)
                    {
                        // Si ya estamos en combate con este slot, solicitar siguiente tirada
                        if (gm.IsInCombatSlot(slot))
                        {
                            gm.RequestCombatRollNext(slot);
                            return;
                        }
                        // Si no, intentar iniciar combate rápidamente (si es legal)
                        if (player != null && gm.CanPerformAction(player, "Attack"))
                        {
                            bool started = gm.BeginCombat(player, slot);
                            if (started)
                            {
                                // Primera doble pulsación: iniciar combate y lanzar inmediatamente la primera tirada
                                gm.RequestCombatRollNext(slot);
                                return;
                            }
                        }
                    }
                }

                // Fallback: abrir preview si no fue doble clic o no se pudo iniciar/continuar combate
                CardPreviewUI.Instance.ShowMonster(slot);
            }
        }
    }

    private void OnMouseEnter()
    {
        if (!enableHoverEffect) return;
        
        bool inTargeting = (TargetingManager.Instance != null && TargetingManager.Instance.IsTargeting);
        
        // Solo hacer hover si es un monstruo y no estamos en modo targeting
        if (targetType == TargetType.Monster && !inTargeting)
        {
            isHovering = true;
            transform.DOKill();
            // NO modificar baseScale, solo aplicar el hover sobre ella
            transform.DOScale(baseScale * hoverScaleMultiplier, hoverAnimationDuration).SetEase(Ease.OutQuad);
        }
    }

    private void OnMouseExit()
    {
        if (!enableHoverEffect) return;
        
        if (isHovering)
        {
            isHovering = false;
            transform.DOKill();
            
            // Volver SIEMPRE a baseScale original (sin modificarla)
            transform.DOScale(baseScale, hoverAnimationDuration).SetEase(Ease.OutQuad);
        }
    }

    /// <summary>
    /// Resalta el objeto (cambia color y escala)
    /// </summary>
    public void Highlight(bool highlight)
    {
        isHighlighted = highlight;

        // Cancelar animaciones previas
        transform.DOKill();
        if (targetingPulseTween != null && targetingPulseTween.IsActive())
        {
            targetingPulseTween.Kill();
            targetingPulseTween = null;
        }

        if (highlight)
        {
            spriteRenderer.color = highlightColor;
            // NO modificar baseScale, aplicar highlight sobre la escala base original
            transform.DOScale(baseScale * highlightScaleMultiplier, 0.2f).SetEase(Ease.OutQuad);
            
            // Añadir pulso si está habilitado
            if (enableTargetingPulse)
            {
                targetingPulseTween = transform.DOScale(baseScale * highlightScaleMultiplier * targetingPulseScale, targetingPulseDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }
        else
        {
            spriteRenderer.color = originalColor;
            // Volver SIEMPRE a baseScale original
            transform.DOScale(baseScale, 0.2f).SetEase(Ease.OutQuad);
        }
    }

    /// <summary>
    /// Controla si el objeto puede ser clickeado
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        // Mantener siempre el collider activo para permitir preview fuera de targeting.
        // Guardamos el estado por si en el futuro se requiere lógica adicional.
        isInteractable = interactable;
    }

    /// <summary>
    /// Construye la selección de objetivo para este objeto
    /// </summary>
    public TargetSelection BuildSelection()
    {
        var selection = new TargetSelection
        {
            targetType = this.targetType
        };

        switch (targetType)
        {
            case TargetType.Player:
                if (playerIndex >= 0 && GameManager.Instance != null)
                {
                    selection.targetPlayer = GameManager.Instance.GetPlayer(playerIndex);
                }
                break;

            case TargetType.Monster:
                selection.targetMonsterCard = monsterCardUI;
                break;

            case TargetType.DiscardPile:
                // No necesita datos adicionales
                break;
        }

        return selection;
    }

    /// <summary>
    /// Obtiene el tipo de objetivo
    /// </summary>
    public TargetType GetTargetType()
    {
        return targetType;
    }

    private void TryRegister()
    {
        if (TargetingManager.Instance == null) return;
        TargetingManager.Instance.Register(this);
        isRegistered = true;
    }

    private void FitColliderToSprite()
    {
        if (spriteRenderer == null || boxCollider == null) return;

        // bounds en espacio de mundo
        var bounds = spriteRenderer.bounds;
        var sizeWorld = bounds.size;
        var centerWorld = bounds.center;

        // Convertir a espacio local del collider (este transform)
        Vector3 lossy = transform.lossyScale;
        float sx = Mathf.Approximately(lossy.x, 0f) ? 1f : lossy.x;
        float sy = Mathf.Approximately(lossy.y, 0f) ? 1f : lossy.y;
        Vector2 sizeLocal = new Vector2(sizeWorld.x / sx, sizeWorld.y / sy);
        Vector2 centerLocal = transform.InverseTransformPoint(centerWorld);

        boxCollider.size = sizeLocal;
        boxCollider.offset = centerLocal;
    }
}
