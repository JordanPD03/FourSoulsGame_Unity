using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Muestra un temporizador de turno (mm:ss) y un icono de reloj.
/// Se sincroniza con el GameManager (eventos OnTurnTimerUpdated y cambio de turno).
/// </summary>
[DisallowMultipleComponent]
public class TurnTimerUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Image clockImage;

    [Header("Appearance")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = new Color(1f, 0.35f, 0.35f); // rojo suave
    [SerializeField] private int warningThresholdSeconds = 30; // Cambiar color cuando quede poco tiempo

    [Header("Behavior")]
    [SerializeField] private bool hideWhenNoTimer = false;

    [Header("External Mode (Optional)")]
    [Tooltip("Cuando está activo, este componente ignora el GameManager y usa las actualizaciones externas (p.ej. selección de personajes)")]
    [SerializeField] private bool useExternalMode = false;

    private float lastRemaining = 0f;
    private float lastTotal = 0f;

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        Subscribe();
        RefreshInitialState();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.OnTurnTimerUpdated += HandleTimerUpdated;
        gm.OnPlayerTurnChanged += HandlePlayerTurnChanged;
    }

    private void Unsubscribe()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.OnTurnTimerUpdated -= HandleTimerUpdated;
        gm.OnPlayerTurnChanged -= HandlePlayerTurnChanged;
    }

    private void RefreshInitialState()
    {
        if (timerText == null) return;
        // Si el GM ya arrancó, fuerza una actualización
        var gm = GameManager.Instance;
        if (gm != null)
        {
            // No tenemos un getter público del tiempo, así que el primer Update llegará pronto
            // Dejamos un placeholder por ahora
            timerText.text = "05:00"; // por defecto 5 minutos
            timerText.color = normalColor;
        }
    }

    private void HandlePlayerTurnChanged(int playerIndex)
    {
        if (useExternalMode) return; // en modo externo no reaccionamos a cambios de turno
        // Nuevo turno: resetear color
        if (timerText != null)
            timerText.color = normalColor;
    }

    private void HandleTimerUpdated(float remaining, float total)
    {
        if (useExternalMode) return; // en modo externo ignoramos eventos del GM
        lastRemaining = remaining;
        lastTotal = total;

        if (timerText == null)
            return;

        // Mostrar u ocultar según configuración
        if (hideWhenNoTimer)
        {
            bool active = total > 0f;
            if (timerText.gameObject.activeSelf != active)
                timerText.gameObject.SetActive(active);
            if (clockImage != null && clockImage.gameObject.activeSelf != active)
                clockImage.gameObject.SetActive(active);
        }

        // Formatear mm:ss
        int seconds = Mathf.CeilToInt(remaining);
        int mm = Mathf.Max(0, seconds / 60);
        int ss = Mathf.Max(0, seconds % 60);
        timerText.text = $"{mm:00}:{ss:00}";

        // Cambiar color si queda poco tiempo
        if (remaining <= warningThresholdSeconds)
            timerText.color = warningColor;
        else
            timerText.color = normalColor;
    }

    // ===== External Mode API =====
    public void SetExternalMode(bool enabled)
    {
        useExternalMode = enabled;
        if (enabled)
        {
            // Asegurar que el timer esté visible en modo externo
            if (timerText != null && !timerText.gameObject.activeSelf)
                timerText.gameObject.SetActive(true);
            if (clockImage != null && !clockImage.gameObject.activeSelf)
                clockImage.gameObject.SetActive(true);
        }
        else
        {
            // Al salir, resetear color
            if (timerText != null) timerText.color = normalColor;
        }
    }

    public void ExternalUpdate(float remaining, float total)
    {
        if (!useExternalMode || timerText == null) return;

        lastRemaining = remaining;
        lastTotal = total;

        // Mostrar/ocultar según configuración
        if (hideWhenNoTimer)
        {
            bool active = total > 0f;
            if (timerText.gameObject.activeSelf != active)
                timerText.gameObject.SetActive(active);
            if (clockImage != null && clockImage.gameObject.activeSelf != active)
                clockImage.gameObject.SetActive(active);
        }

        // Formatear mm:ss
        int seconds = Mathf.CeilToInt(Mathf.Max(0f, remaining));
        int mm = Mathf.Max(0, seconds / 60);
        int ss = Mathf.Max(0, seconds % 60);
        timerText.text = $"{mm:00}:{ss:00}";

        // Cambiar color si queda poco tiempo
        if (remaining <= warningThresholdSeconds)
            timerText.color = warningColor;
        else
            timerText.color = normalColor;
    }
}
