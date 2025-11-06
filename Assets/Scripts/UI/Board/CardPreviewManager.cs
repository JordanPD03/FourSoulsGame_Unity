using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;

/// <summary>
/// Muestra una vista previa (en World Space) de una carta sobre la mesa.
/// Está pensado para usarse desde cualquier UI de tablero (PlayerBoardDisplay, etc.).
/// 
/// Uso típico:
/// - CardPreviewManager.Instance.Show(cardData|cardSO|sprite, followTransform, optionalWorldOffset)
/// - CardPreviewManager.Instance.Hide()
/// 
/// La vista previa se instancia bajo el Canvas ancestro del followTransform para asegurar orden y escala correctos.
/// </summary>
public class CardPreviewManager : MonoBehaviour
{
    public static CardPreviewManager Instance { get; private set; }

    [Header("Apariencia")]
    [Tooltip("Tamaño (en unidades de Canvas) de la carta de preview")] 
    public Vector2 previewSize = new Vector2(280f, 390f);
    [Tooltip("Offset en unidades de mundo respecto al objetivo (si no se especifica otro al mostrar)")]
    public Vector3 defaultWorldOffset = new Vector3(0.12f, 0.12f, 0f);
    [Tooltip("Escala inicial de aparición (1 = tamaño final)")]
    public float popStartScale = 0.9f;
    [Tooltip("Duración de tween de aparición/desaparición")]
    public float tweenDuration = 0.15f;

    private RectTransform _previewRect;
    private Image _previewImage;
    private Canvas _currentCanvas;
    private bool _isHiding;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Persistente opcional (descomentar si se desea entre escenas)
        // DontDestroyOnLoad(gameObject);
    }

    public void Show(CardData data, Transform followTarget, Vector3? worldOffset = null)
    {
        if (data == null) return;
        var sprite = data.isFaceUp ? (data.frontSprite != null ? data.frontSprite : data.sourceScriptableObject != null ? data.sourceScriptableObject.frontSprite : null) : (data.backSprite != null ? data.backSprite : data.sourceScriptableObject != null ? data.sourceScriptableObject.backSprite : null);
        Show(sprite, followTarget, worldOffset);
    }

    public void Show(CardDataSO so, Transform followTarget, Vector3? worldOffset = null)
    {
        if (so == null) return;
        Show(so.frontSprite, followTarget, worldOffset);
    }

    public void Show(Sprite sprite, Transform followTarget, Vector3? worldOffset = null)
    {
        if (sprite == null || followTarget == null) return;

        Canvas canvas = followTarget.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[CardPreviewManager] No se encontró Canvas ancestro para la vista previa; cancelado.");
            return;
        }

        EnsurePreview(canvas.transform);

        _previewImage.sprite = sprite;
        _previewImage.enabled = true;
        _previewImage.preserveAspect = true;
        _previewImage.raycastTarget = false; // no bloquear eventos
        _previewRect.sizeDelta = previewSize;

        // Posicionar en mundo cerca del objetivo
        var offset = worldOffset.HasValue ? worldOffset.Value : defaultWorldOffset;
        _previewRect.position = followTarget.position + offset;
        _previewRect.rotation = followTarget.rotation; // respetar orientación del canvas del jugador

        // Animación pop
        _previewRect.DOKill();
        _previewImage.DOKill();
        var finalScale = Vector3.one;
        _previewRect.localScale = Vector3.one * Mathf.Max(0.01f, popStartScale);
        _previewImage.color = new Color(1f, 1f, 1f, 0f);
        _previewRect.DOScale(finalScale, tweenDuration).SetEase(Ease.OutCubic);
        _previewImage.DOFade(1f, tweenDuration).SetEase(Ease.OutCubic);
    }

    public void Hide()
    {
        if (_previewRect == null || _previewImage == null) return;
        _previewRect.DOKill();
        _previewImage.DOKill();
        // Usar una secuencia para evitar destruir mientras otro tween sigue activo
        var seq = DOTween.Sequence();
        _isHiding = true;
        seq.Join(_previewRect.DOScale(_previewRect.localScale * 0.95f, tweenDuration).SetEase(Ease.OutCubic));
        seq.Join(_previewImage.DOFade(0f, tweenDuration).SetEase(Ease.OutCubic));
        seq.OnComplete(() =>
        {
            if (_previewRect != null)
            {
                Destroy(_previewRect.gameObject);
            }
            _previewRect = null;
            _previewImage = null;
            _currentCanvas = null;
            _isHiding = false;
        });
    }

    /// <summary>
    /// Oculta inmediatamente sin tween (útil al entrar en modo targeting o al hacer click en vacío).
    /// </summary>
    public void HideImmediate()
    {
        if (_previewRect == null || _previewImage == null) return;
        _previewRect.DOKill();
        _previewImage.DOKill();
        if (_previewRect != null)
        {
            Destroy(_previewRect.gameObject);
        }
        _previewRect = null;
        _previewImage = null;
        _currentCanvas = null;
        _isHiding = false;
    }

    private void OnDisable()
    {
        // Asegurar limpieza de tweens y objeto temporal
        if (_previewRect != null) _previewRect.DOKill();
        if (_previewImage != null) _previewImage.DOKill();
    }

    private void OnDestroy()
    {
        if (_previewRect != null) _previewRect.DOKill();
        if (_previewImage != null) _previewImage.DOKill();
        if (_previewRect != null)
        {
            Destroy(_previewRect.gameObject);
        }
        _previewRect = null;
        _previewImage = null;
        _currentCanvas = null;
    }

    private void EnsurePreview(Transform parent)
    {
        // Si cambia el canvas, destruimos la previa anterior para re-crear bajo el nuevo
        if (_currentCanvas != null && parent != null && _currentCanvas.transform != parent)
        {
            if (_previewRect != null)
            {
                Destroy(_previewRect.gameObject);
            }
            _previewRect = null;
            _previewImage = null;
            _currentCanvas = null;
        }

        if (_previewRect != null) return;

        var go = new GameObject("CardPreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _previewRect = go.GetComponent<RectTransform>();
        _previewImage = go.GetComponent<Image>();
        _previewRect.SetParent(parent, worldPositionStays: false);
        _previewRect.anchorMin = new Vector2(0.5f, 0.5f);
        _previewRect.anchorMax = new Vector2(0.5f, 0.5f);
        _previewRect.pivot = new Vector2(0.5f, 0.5f);
        _previewRect.localScale = Vector3.one;
        _previewRect.anchoredPosition3D = Vector3.zero;

        _currentCanvas = parent.GetComponentInParent<Canvas>();
    }

    private void Update()
    {
        // Si hay una preview visible y el usuario hace click en un área sin UI, ocultar inmediatamente.
        if (_previewRect != null && !_isHiding)
        {
            if (Input.GetMouseButtonDown(0))
            {
                bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                if (!overUI)
                {
                    HideImmediate();
                }
            }
        }
    }
}
