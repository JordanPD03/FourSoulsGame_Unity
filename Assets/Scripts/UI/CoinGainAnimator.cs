using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Anima monedas "volando" hacia el icono de monedas del jugador que las gana.
/// Crea imágenes temporales que se mueven rápidamente en secuencia y al llegar pulsan el icono destino.
/// </summary>
public class CoinGainAnimator : MonoBehaviour
{
    public static CoinGainAnimator Instance { get; private set; }

    [Header("Layer de Animación (Canvas)")]
    [Tooltip("Capa/RectTransform bajo la cual se instancian las monedas voladoras. Si está vacío, intentará usar GameManager.animationLayer o el Canvas principal.")]
    public RectTransform animationLayer;

    [Header("Apariencia de Moneda")]
    [Tooltip("Tamaño de cada moneda visual en UI.")]
    public Vector2 coinSize = new Vector2(28f, 28f);
    [Tooltip("Color multiplicador opcional para las monedas.")]
    public Color coinTint = Color.white;

    [Header("Tiempo y Secuencia")]
    [Tooltip("Duración del vuelo de cada moneda (segundos)")]
    public float flyDuration = 0.25f;
    [Tooltip("Retraso entre cada moneda para la secuencia (segundos)")]
    public float spawnInterval = 0.06f;
    [Tooltip("Máximo de monedas visuales a mostrar (para cantidades grandes)")]
    public int maxVisualCoins = 8;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private RectTransform ResolveAnimationLayer()
    {
        if (animationLayer != null) return animationLayer;
        if (GameManager.Instance != null)
        {
            var layer = GameManager.Instance.GetAnimationLayerRect();
            if (layer != null) return layer;
        }
        // fallback: primer Canvas en la escena
        var canvas = FindObjectOfType<Canvas>();
        return canvas != null ? canvas.transform as RectTransform : null;
    }

    /// <summary>
    /// Dispara la animación de monedas hacia el icono de monedas del jugador.
    /// </summary>
    /// <param name="player">Jugador que recibe las monedas</param>
    /// <param name="amount">Cantidad ganada</param>
    /// <param name="startScreenPos">Posición inicial en pantalla (opcional). Si es null, parte del centro del layer.</param>
    public void PlayCoinGain(PlayerData player, int amount, Vector2? startScreenPos = null)
    {
        if (player == null || amount <= 0) return;
        if (UIRegistry.Instance == null) return;

        RectTransform layer = ResolveAnimationLayer();
        if (layer == null) return;

        // Determinar destino: primero intentar Board (world-space), luego HUD legacy
        RectTransform targetRect = null;
        Sprite coinSprite = null;
        PlayerBoardDisplay board = null;
        PlayerStatsUI stats = null;

        if (UIRegistry.Instance.TryGetPlayerBoard(player.playerId, out board) && board != null)
        {
            targetRect = board.GetCoinsTargetRect();
            coinSprite = board.GetCoinSprite();
        }
        else if (UIRegistry.Instance.TryGetPlayerStats(player.playerId, out stats) && stats != null && stats.coinsIcon != null)
        {
            targetRect = stats.coinsIcon.rectTransform;
            coinSprite = stats.coinsIcon.sprite;
        }

        if (targetRect == null) return;

        // Convertir target a posición local del layer
        Vector2 targetLocal;
        if (!TryGetIconCenterInLayer(targetRect, layer, out targetLocal))
            return;

        // Posición inicial (pantalla → layer local)
        Vector2 startLocal;
        if (startScreenPos.HasValue)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, startScreenPos.Value, layer.GetComponentInParent<Canvas>()?.worldCamera, out startLocal))
                startLocal = Vector2.zero;
        }
        else
        {
            startLocal = Vector2.zero; // centro del layer
        }

        int visuals = Mathf.Clamp(amount, 1, Mathf.Max(1, maxVisualCoins));
        if (board != null)
            StartCoroutine(PlaySequenceBoard(board, layer, startLocal, targetLocal, visuals, coinSprite));
        else if (stats != null)
            StartCoroutine(PlaySequenceStats(stats, layer, startLocal, targetLocal, visuals, coinSprite));
    }

    private IEnumerator PlaySequenceStats(PlayerStatsUI stats, RectTransform layer, Vector2 startLocal, Vector2 targetLocal, int visuals, Sprite coinSprite)
    {
        // Evitar doble pulso por evento: que PlayerStatsUI no pulse en HandleCoinsChanged inmediatamente
        stats.SuppressNextCoinsPulse();

        for (int i = 0; i < visuals; i++)
        {
            bool isLast = i == visuals - 1;
            SpawnAndFlyOneCommon(layer, startLocal, targetLocal, coinSprite, onArrive: isLast ? (Action)stats.PulseCoinsIcon : null);
            if (spawnInterval > 0f)
                yield return new WaitForSeconds(spawnInterval);
        }
    }

    private IEnumerator PlaySequenceBoard(PlayerBoardDisplay board, RectTransform layer, Vector2 startLocal, Vector2 targetLocal, int visuals, Sprite coinSprite)
    {
        for (int i = 0; i < visuals; i++)
        {
            bool isLast = i == visuals - 1;
            SpawnAndFlyOneCommon(layer, startLocal, targetLocal, coinSprite, onArrive: isLast ? (Action)board.PulseCoins : null);
            if (spawnInterval > 0f)
                yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnAndFlyOneCommon(RectTransform layer, Vector2 startLocal, Vector2 targetLocal, Sprite coinSprite, Action onArrive)
    {
        var go = new GameObject("FlyingCoin", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(layer, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = coinSize;
        rt.anchoredPosition = startLocal + (Vector2)UnityEngine.Random.insideUnitCircle * 14f;

        var img = go.GetComponent<Image>();
        // Usar el sprite del icono de monedas si está asignado para coherencia visual
        img.sprite = coinSprite;
        img.color = coinTint;
        img.raycastTarget = false;

        var cg = go.GetComponent<CanvasGroup>();
        cg.alpha = 0f;

        // Pequeño fade-in y vuelo
        Sequence seq = DOTween.Sequence();
        seq.Append(cg.DOFade(1f, 0.08f));
        seq.Join(rt.DOScale(1.0f, 0.08f).From(0.6f));
        seq.Append(rt.DOAnchorPos(targetLocal, flyDuration).SetEase(Ease.OutQuad));
        // Al llegar, pequeño pop + fade out y destruir
        seq.AppendCallback(() =>
        {
            onArrive?.Invoke();
        });
        seq.Append(rt.DOPunchScale(Vector3.one * 0.12f, 0.12f, 8, 0.8f));
        seq.Join(cg.DOFade(0f, 0.12f).SetDelay(0.02f));
        seq.OnComplete(() =>
        {
            if (go != null) Destroy(go);
        });
    }

    private bool TryGetIconCenterInLayer(RectTransform icon, RectTransform layer, out Vector2 local)
    {
        local = Vector2.zero;
        if (icon == null || layer == null) return false;

        var canvas = layer.GetComponentInParent<Canvas>();
        Camera cam = canvas != null ? canvas.worldCamera : null;
        Vector3 world = icon.TransformPoint(icon.rect.center);
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, world);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, cam, out local);
    }
}
