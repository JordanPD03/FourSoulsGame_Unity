using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// Componente que, al pasar el mouse (o puntero) por encima, muestra una vista previa de la carta en la mesa.
/// Soporta CardData, CardDataSO o un Sprite directo (prioridad en ese orden).
/// </summary>
[DisallowMultipleComponent]
public class CardPreviewTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Datos de Carta (opcional)")]
    public CardData cardData;
    public CardDataSO cardDataSO;
    public Sprite spriteOverride;

    [Header("Comportamiento")]
    [Tooltip("Mostrar al pasar el cursor por encima")]
    public bool showOnHover = true;
    [Tooltip("Mostrar al hacer click")]
    public bool showOnClick = true;
    [Tooltip("Offset en unidades de mundo respecto a este objeto")]
    public Vector3 worldOffset = new Vector3(0.12f, 0.12f, 0f);

    [Header("Hover Animation")]
    public bool hoverScaleEnabled = true;
    public float hoverScaleFactor = 1.08f;
    public float hoverTween = 0.15f;

    private bool _isShown;
    private Vector3 _baseScale;

    private void Awake()
    {
        _baseScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverScaleEnabled)
        {
            transform.DOKill();
            transform.DOScale(_baseScale * hoverScaleFactor, hoverTween).SetEase(Ease.OutSine);
        }
        if (!showOnHover) return;
        // No mostrar preview mientras esté activo el targeting
        if (TargetingManager.Instance != null && TargetingManager.Instance.IsTargeting) return;
        Show();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverScaleEnabled)
        {
            transform.DOKill();
            transform.DOScale(_baseScale, hoverTween).SetEase(Ease.OutSine);
        }
        if (!showOnHover) return;
        Hide();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!showOnClick) return;
        if (TargetingManager.Instance != null && TargetingManager.Instance.IsTargeting) return;
        Show();
    }

    public void Show()
    {
        // No mostrar preview mientras esté activo el targeting
        if (TargetingManager.Instance != null && TargetingManager.Instance.IsTargeting) return;
        if (CardPreviewManager.Instance == null) return;
        if (cardData != null)
        {
            CardPreviewManager.Instance.Show(cardData, transform, worldOffset);
            _isShown = true;
            return;
        }
        if (cardDataSO != null)
        {
            CardPreviewManager.Instance.Show(cardDataSO, transform, worldOffset);
            _isShown = true;
            return;
        }
        if (spriteOverride != null)
        {
            CardPreviewManager.Instance.Show(spriteOverride, transform, worldOffset);
            _isShown = true;
            return;
        }
    }

    public void Hide()
    {
        if (!_isShown) return;
        if (CardPreviewManager.Instance == null) return;
        CardPreviewManager.Instance.Hide();
        _isShown = false;
    }

    private void OnDisable()
    {
        // Asegurar que se oculte si el objeto se desactiva
        if (_isShown && CardPreviewManager.Instance != null)
        {
            CardPreviewManager.Instance.Hide();
            _isShown = false;
        }
        if (hoverScaleEnabled)
        {
            transform.DOKill();
            transform.localScale = _baseScale;
        }
    }
}
