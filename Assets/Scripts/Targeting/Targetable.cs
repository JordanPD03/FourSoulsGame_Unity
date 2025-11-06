using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Zona o elemento seleccionable como objetivo
/// </summary>
[RequireComponent(typeof(Image))]
public class Targetable : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public TargetType targetType = TargetType.Player;

    [Header("Datos del objetivo (opcionales segun tipo)")]
    public int playerIndex = -1;        // Para objetivos de jugador
    public CardUI monsterCardUI;        // Para objetivos de monstruo (si usamos una carta UI)

    private Image img;
    private Color baseColor;
    private bool interactable = false;
    private bool isRegistered = false;

    [Header("Highlight Animation (UI)")]
    public Color highlightColor = new Color(1f, 1f, 0.5f, 1f); // Amarillo claro similar a monstruos
    public bool enableTargetingPulse = true;
    public float highlightScaleMultiplier = 1.08f;
    public float targetingPulseScale = 1.12f;
    public float targetingPulseDuration = 0.6f;
    private Vector3 baseScale = Vector3.one;
    private Tween pulseTween;

    [Header("Outline de énfasis (opcional)")]
    public bool useOutlineDuringTargeting = true;
    public Color outlineColor = new Color(1f, 0.92f, 0.016f, 0.95f); // amarillo dorado
    public Vector2 outlineDistance = new Vector2(2f, -2f);
    private Outline outline;

    private void Awake()
    {
        img = GetComponent<Image>();
        baseColor = img != null ? img.color : Color.white;
        baseScale = transform.localScale;
        outline = GetComponent<Outline>();
    }

    private void OnEnable()
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
        // Kill tweens and restore scale
        transform.DOKill();
        if (pulseTween != null && pulseTween.IsActive()) pulseTween.Kill();
        transform.localScale = baseScale;
        // Disable outline when not active
        if (outline != null)
        {
            outline.enabled = false;
        }
    }

    private void Update()
    {
        // Registrar tardíamente si el TargetingManager aparece después
        if (!isRegistered)
        {
            TryRegister();
        }
    }

    private void TryRegister()
    {
        if (TargetingManager.Instance != null && !isRegistered)
        {
            TargetingManager.Instance.Register(this);
            isRegistered = true;
            // Debug.Log($"[Targetable] Registrado: {name} tipo={targetType}");
        }
    }

    public void Highlight(bool on)
    {
        if (img == null) return;
        if (on)
        {
            // Tint amarillento manteniendo alpha alto
            var tinted = highlightColor;
            tinted.a = 0.9f;
            img.color = tinted;
        }
        else
        {
            img.color = baseColor;
        }

        // Pulse similar a WorldTargetable (para consistencia de enemigos y UI)
        transform.DOKill();
        if (pulseTween != null && pulseTween.IsActive())
        {
            pulseTween.Kill();
            pulseTween = null;
        }
        if (on)
        {
            // subir a escala resaltada y comenzar pulso
            transform.localScale = baseScale * highlightScaleMultiplier;
            if (enableTargetingPulse)
            {
                pulseTween = transform.DOScale(baseScale * targetingPulseScale, targetingPulseDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
            // Outline de énfasis
            if (useOutlineDuringTargeting)
            {
                if (outline == null) outline = gameObject.AddComponent<Outline>();
                outline.effectColor = outlineColor;
                outline.effectDistance = outlineDistance;
                outline.enabled = true;
            }
        }
        else
        {
            // volver a la escala base
            transform.DOScale(baseScale, 0.2f).SetEase(Ease.OutQuad);
            if (outline != null) outline.enabled = false;
        }
    }

    public void SetInteractable(bool can)
    {
        interactable = can;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!interactable) return;
        if (TargetingManager.Instance == null) return;
        if (!TargetingManager.Instance.IsTargeting) return;
        if (!TargetingManager.Instance.IsTargetTypeAllowed(targetType)) return;

        // Validación adicional para Player
        if (targetType == TargetType.Player && playerIndex < 0)
        {
            Debug.LogWarning($"[Targetable] playerIndex no asignado en {name}. Configura el índice del jugador.");
            return;
        }

        TargetingManager.Instance.TargetClicked(this);
    }

    public TargetSelection BuildSelection()
    {
        switch (targetType)
        {
            case TargetType.Player:
                if (playerIndex >= 0 && GameManager.Instance != null)
                {
                    var p = GameManager.Instance.GetPlayer(playerIndex);
                    if (p != null) return TargetSelection.ForPlayer(p);
                }
                Debug.LogWarning($"[Targetable] No se pudo construir TargetSelection para Player en {name}. playerIndex={playerIndex}");
                return new TargetSelection { targetType = TargetType.None };
            case TargetType.Monster:
                if (monsterCardUI != null)
                {
                    return TargetSelection.ForMonster(monsterCardUI.GetCardData());
                }
                Debug.LogWarning($"[Targetable] monsterCardUI no asignado en {name} para target Monster");
                return new TargetSelection { targetType = TargetType.None };
            case TargetType.DiscardPile:
                return TargetSelection.ForDiscardPile();
        }
        return new TargetSelection { targetType = targetType };
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Highlight visual mínimo cuando targeting activo y permitido
        if (TargetingManager.Instance != null && TargetingManager.Instance.IsTargeting && TargetingManager.Instance.IsTargetTypeAllowed(targetType))
        {
            Highlight(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Quitar highlight si no estamos en targeting
        if (TargetingManager.Instance == null || !TargetingManager.Instance.IsTargeting)
        {
            Highlight(false);
        }
    }
}
