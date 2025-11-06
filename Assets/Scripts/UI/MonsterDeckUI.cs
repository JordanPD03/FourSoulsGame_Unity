using UnityEngine;
using DG.Tweening;

/// <summary>
/// Representa el mazo de Monstruos en el tablero. Permite hacer click para atacar el tope y colocar un overlay.
/// </summary>
public class MonsterDeckUI : MonoBehaviour
{
    [Header("Visuals")]
    [Tooltip("SpriteRenderer del dorso del mazo")] public SpriteRenderer deckRenderer;
    
    [Header("Interaction")]
    [Tooltip("Añadir un BoxCollider2D si no existe para recibir clicks")] public bool addColliderIfMissing = true;
    
    [Header("Hover")]
    [Tooltip("Efecto de hover al pasar el mouse")] public bool enableHover = true;
    [Range(1.0f, 1.2f)] public float hoverScale = 1.05f;
    [Tooltip("Duración del tween de hover")] public float hoverTweenDuration = 0.1f;

    private Vector3 baseScale = Vector3.one;
    private bool isHovering = false;

    private void Awake()
    {
        if (deckRenderer == null)
        {
            deckRenderer = GetComponent<SpriteRenderer>();
            if (deckRenderer == null)
            {
                deckRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }
        
        if (deckRenderer != null)
        {
            baseScale = deckRenderer.transform.localScale;
        }

        EnsureCollider();
    }

    private void EnsureCollider()
    {
        if (!addColliderIfMissing) return;
        var col = GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
        }
        
        // Ajustar collider al sprite
        if (deckRenderer != null && deckRenderer.sprite != null)
        {
            Vector2 worldSize = deckRenderer.bounds.size;
            Vector3 lossy = transform.lossyScale;
            float sx = Mathf.Approximately(lossy.x, 0f) ? 1f : Mathf.Abs(lossy.x);
            float sy = Mathf.Approximately(lossy.y, 0f) ? 1f : Mathf.Abs(lossy.y);
            col.size = new Vector2(worldSize.x / sx, worldSize.y / sy);
            col.offset = Vector2.zero;
        }
    }

    private void OnMouseDown()
    {
        // Ignorar si estamos en targeting
        if (TargetingManager.Instance != null && TargetingManager.Instance.IsTargeting)
        {
            return;
        }

        // Intentar iniciar ataque al tope del mazo
        var gm = GameManager.Instance;
        if (gm == null) return;

        var player = gm.GetCurrentPlayer();
        if (player == null)
        {
            Debug.LogWarning("[MonsterDeckUI] No hay jugador activo.");
            return;
        }

        if (!gm.CanPerformAction(player, "Attack"))
        {
            Debug.LogWarning($"[MonsterDeckUI] No puedes atacar durante la fase {gm.GetCurrentPhase()} o no tienes ataques disponibles.");
            return;
        }

        Debug.Log("[MonsterDeckUI] Click en mazo de Monstruos: iniciando ataque al tope.");
        gm.BeginAttackDeckTop(player);
    }

    private void OnMouseEnter()
    {
        if (!enableHover) return;
        if (TargetingManager.Instance != null && TargetingManager.Instance.IsTargeting) return;
        if (deckRenderer == null) return;

        isHovering = true;
        deckRenderer.transform.DOKill();
        deckRenderer.transform.DOScale(baseScale * hoverScale, hoverTweenDuration).SetEase(Ease.OutQuad);
    }

    private void OnMouseExit()
    {
        if (!enableHover) return;
        if (deckRenderer == null) return;

        if (isHovering)
        {
            isHovering = false;
            deckRenderer.transform.DOKill();
            deckRenderer.transform.DOScale(baseScale, hoverTweenDuration).SetEase(Ease.InQuad);
        }
    }
}
