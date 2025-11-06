using UnityEngine;
using TMPro;
using DG.Tweening;
using System;

/// <summary>
/// Muestra un banner centrado con el texto "Turno de <Jugador>"
/// con un pequeño efecto de zoom y desvanecimiento al inicio del turno.
/// 
/// Colócalo en un objeto dentro del Canvas con:
/// - CanvasGroup (para fade)
/// - TextMeshProUGUI (texto centrado)
/// </summary>
[DisallowMultipleComponent]
public class TurnAnnouncerUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI turnText;         // Texto centrado
    [SerializeField] private CanvasGroup canvasGroup;           // Para fade in/out
    [SerializeField] private RectTransform container;           // RectTransform del banner (para escala)

    [Header("Appearance")]
    [SerializeField] private string messageFormat = "Turno de {0}";
    [SerializeField] private float fadeInDuration = 0.25f;
    [SerializeField] private float punchDuration = 0.25f;
    [SerializeField] private float holdDuration = 0.9f;
    [SerializeField] private float fadeOutDuration = 0.45f;
    [SerializeField] private float startScale = 0.85f;          // Escala al aparecer
    [SerializeField] private float punchScale = 1.12f;          // Pico de escala (pequeño zoom)
    [SerializeField] private float fadeOutScale = 1.15f;         // Escala final al desvanecer (zoom out)

    [Header("Behavior")]
    [SerializeField] private bool playOnGameStart = true;       // Reproduce al arrancar juego (primer turno)

    private Sequence currentSeq;
    private bool subscribed = false;
    private GameManager cachedGM = null;
    
    // Eventos para notificar inicio y fin de la animación
    public event Action OnAnimationStarted;
    public event Action OnAnimationComplete;

    private void Reset()
    {
        // Intentar autocompletar referencias en el editor
        container = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (turnText == null)
            turnText = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Awake()
    {
        Debug.Log("[TurnAnnouncerUI] Awake llamado");
        if (container == null) container = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        // Estado inicial oculto
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            Debug.Log("[TurnAnnouncerUI] CanvasGroup configurado (alpha=0)");
        }
        if (container != null)
        {
            container.localScale = Vector3.one;
            Debug.Log("[TurnAnnouncerUI] Container configurado (scale=1)");
        }
    }

    private void OnEnable()
    {
        Debug.Log("[TurnAnnouncerUI] OnEnable llamado");
        TrySubscribe();
    }

    private void Start()
    {
        Debug.Log("[TurnAnnouncerUI] Start llamado");
        Debug.Log($"[TurnAnnouncerUI] turnText asignado: {turnText != null}");
        Debug.Log($"[TurnAnnouncerUI] canvasGroup asignado: {canvasGroup != null}");
        Debug.Log($"[TurnAnnouncerUI] container asignado: {container != null}");
        
        // Intentar suscribirse si aún no fue posible en OnEnable (por orden de ejecución)
        TrySubscribe();

        // Anunciar el primer turno en cuanto el GameManager esté listo
        if (playOnGameStart)
        {
            Debug.Log("[TurnAnnouncerUI] playOnGameStart = true, iniciando AnnounceWhenReady...");
            StartCoroutine(AnnounceWhenReady());
        }
    }

    private void OnDisable()
    {
        TryUnsubscribe();
        KillSequence();
    }

    private void Update()
    {
        // Cubrir el caso en que GameManager.Instance se inicializa después
        // o cambia entre escenas
        if (!subscribed || cachedGM != GameManager.Instance)
        {
            TryUnsubscribe();
            TrySubscribe();
        }
    }

    private void HandlePlayerTurnChanged(int playerIndex)
    {
        Debug.Log($"[TurnAnnouncerUI] ===== HandlePlayerTurnChanged RECIBIDO: playerIndex = {playerIndex} =====");
        var gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[TurnAnnouncerUI] GameManager.Instance es NULL en HandlePlayerTurnChanged!");
            return;
        }

        var player = gm.GetPlayer(playerIndex);
        if (player == null)
        {
            Debug.LogWarning($"[TurnAnnouncerUI] No se encontró jugador en índice {playerIndex}");
            return;
        }

        Debug.Log($"[TurnAnnouncerUI] Mostrando anuncio para: {player.playerName}");
        string msg = string.Format(messageFormat, player.playerName);
        PlayMessage(msg);
    }

    /// <summary>
    /// Reproduce la animación del banner con el mensaje indicado.
    /// </summary>
    public void PlayMessage(string message)
    {
        Debug.Log($"[TurnAnnouncerUI] PlayMessage llamado: '{message}'");
        
        if (turnText == null || canvasGroup == null || container == null)
        {
            Debug.LogError("[TurnAnnouncerUI] Faltan referencias! turnText, canvasGroup o container son NULL. Asigna en el Inspector.");
            Debug.LogError($"[TurnAnnouncerUI] turnText: {turnText != null}, canvasGroup: {canvasGroup != null}, container: {container != null}");
            return;
        }

        turnText.text = message;
        Debug.Log($"[TurnAnnouncerUI] Texto configurado: '{turnText.text}'");

        KillSequence();

        // Estado inicial antes de animar
        canvasGroup.alpha = 0f;
        container.localScale = Vector3.one * startScale;
        Debug.Log($"[TurnAnnouncerUI] Estado inicial: alpha=0, scale={startScale}");

        // Secuencia con DOTween
        currentSeq = DOTween.Sequence();
        currentSeq.SetUpdate(true); // asegurar que corre aunque Time.timeScale cambie

        // Notificar inicio
        Debug.Log("[TurnAnnouncerUI] OnAnimationStarted invocado");
        OnAnimationStarted?.Invoke();

        // Fade in + pequeño zoom (overshoot)
        currentSeq.Append(canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad));
        currentSeq.Join(container.DOScale(punchScale, punchDuration).SetEase(Ease.OutBack));

        // Suavizar a escala 1
        currentSeq.Append(container.DOScale(1f, 0.18f).SetEase(Ease.OutQuad));

        // Mantener visible
        currentSeq.AppendInterval(holdDuration);

        // Fade out + zoom out simultáneo
        currentSeq.Append(canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad));
        currentSeq.Join(container.DOScale(fadeOutScale, fadeOutDuration).SetEase(Ease.InQuad));
        
        // Callback al terminar la secuencia completa
        currentSeq.OnComplete(() =>
        {
            Debug.Log("[TurnAnnouncerUI] Animación completada, OnAnimationComplete invocado");
            OnAnimationComplete?.Invoke();
        });

        Debug.Log("[TurnAnnouncerUI] Iniciando secuencia DOTween...");
        currentSeq.Play();
    }

    /// <summary>
    /// Muestra un mensaje persistente (no se oculta automáticamente). Llamar a HideMessage() para cerrarlo.
    /// Útil para flujos de selección (p.ej., "Descarta una carta").
    /// </summary>
    public void ShowPersistentMessage(string message)
    {
        if (turnText == null || canvasGroup == null || container == null)
        {
            Debug.LogError("[TurnAnnouncerUI] Faltan referencias para ShowPersistentMessage");
            return;
        }

        KillSequence();
        turnText.text = message;
        // Preparar estado visible
        canvasGroup.DOKill();
        container.DOKill();
        container.localScale = Vector3.one; // sin punch ni auto-animación
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad).SetUpdate(true);
        // Notificar como "iniciado" para pausar temporizador, etc.
        OnAnimationStarted?.Invoke();
    }

    /// <summary>
    /// Oculta el mensaje persistente (si está visible).
    /// </summary>
    public void HideMessage()
    {
        if (canvasGroup == null || container == null) return;
        KillSequence();
        canvasGroup.DOKill();
        container.DOKill();
        canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad).SetUpdate(true)
            .OnComplete(() =>
            {
                OnAnimationComplete?.Invoke();
            });
    }

    private void KillSequence()
    {
        if (currentSeq != null && currentSeq.IsActive())
        {
            currentSeq.Kill();
            currentSeq = null;
        }
    }

    private void TrySubscribe()
    {
        if (subscribed)
        {
            return;
        }
        var gm = GameManager.Instance;
        if (gm == null)
        {
            // GameManager aún no existe (orden de inicialización). Reintentar más tarde sin advertencias.
            return;
        }
        gm.OnPlayerTurnChanged += HandlePlayerTurnChanged;
        cachedGM = gm;
        subscribed = true;
    }

    private void TryUnsubscribe()
    {
        if (!subscribed) return;
        if (cachedGM != null)
        {
            cachedGM.OnPlayerTurnChanged -= HandlePlayerTurnChanged;
        }
        subscribed = false;
        cachedGM = null;
    }

    private System.Collections.IEnumerator AnnounceWhenReady()
    {
        Debug.Log("[TurnAnnouncerUI] AnnounceWhenReady iniciado...");
        // Esperar hasta que GameManager indique que están listos los anuncios (tras setup/selección)
        while (true)
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                Debug.Log("[TurnAnnouncerUI] GameManager encontrado");
                // No anunciar hasta que el GameManager indique que el setup terminó
                if (gm.IsReadyForTurnAnnouncements)
                {
                    var p = gm.GetCurrentPlayer();
                    if (p != null)
                    {
                        // El evento OnPlayerTurnChanged disparará el mensaje; salir sin forzar PlayMessage para evitar duplicado
                        Debug.Log("[TurnAnnouncerUI] Setup listo; esperando evento de turno para anunciar");
                        yield break;
                    }
                    else
                    {
                        Debug.LogWarning("[TurnAnnouncerUI] GetCurrentPlayer() devolvió NULL tras setup; esperando...");
                    }
                }
                // Aún no listo para anunciar (probablemente en selección de personajes)
            }
            else
            {
                Debug.LogWarning("[TurnAnnouncerUI] GameManager.Instance es NULL, esperando...");
            }
            yield return null;
        }
    }
}
