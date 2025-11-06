using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Controla el flujo de selección de objetivos.
/// Resalta objetivos válidos y notifica cuando el jugador elige uno.
/// Soporta tanto objetivos UI (Targetable) como del mundo (WorldTargetable).
/// </summary>
public class TargetingManager : MonoBehaviour
{
    public static TargetingManager Instance { get; private set; }

    [Header("Targeting Settings")]
    [Tooltip("Permitir cancelar con Escape o click derecho")]
    public bool allowCancel = true;
    [Tooltip("Mensaje por defecto mostrado durante la selección")] public string defaultPrompt = "Elige un objetivo";

    [Header("Overlay Visual")]
    [Tooltip("Color del overlay al entrar en modo targeting (alpha ~0.5 recomendado)")]
    public Color overlayColor = new Color(0f, 0f, 0f, 0.5f);
    private RectTransform overlayRect;
    private UnityEngine.UI.Image overlayImage;
    private TextMeshProUGUI overlayPromptText;
    [Header("Prompt (TMP)")]
    [Tooltip("Fuente del prompt (TextMeshPro Font Asset)")] public TMP_FontAsset promptFont;
    [Tooltip("Color del texto de prompt")] public Color promptColor = new Color(1f, 1f, 1f, 0.95f);
    [Tooltip("Tamaño del texto de prompt")] public int promptFontSize = 36;
    [Tooltip("Alineación del prompt")] public TextAlignmentOptions promptAlignment = TextAlignmentOptions.Center;
    [Tooltip("Posición anclada del prompt dentro del overlay")] public Vector2 promptAnchoredPosition = new Vector2(0f, -20f);
    [Tooltip("Tamaño del rectángulo del prompt")] public Vector2 promptSize = new Vector2(800f, 100f);

    private Action<TargetSelection> onTargetChosen;
    private HashSet<TargetType> allowedTypes = new HashSet<TargetType>();
    private bool isActive = false;

    // Listas separadas para UI y World targets
    private readonly List<Targetable> registeredUITargets = new List<Targetable>();
    private readonly List<WorldTargetable> registeredWorldTargets = new List<WorldTargetable>();

    public bool IsTargeting => isActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        // Permitir cancelar con ESC o click derecho
        if (isActive && allowCancel)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                CancelTargeting();
            }
        }
    }

    // === REGISTRO DE TARGETS UI ===
    public void Register(Targetable t)
    {
        if (t != null && !registeredUITargets.Contains(t))
        {
            registeredUITargets.Add(t);
        }
    }

    public void Unregister(Targetable t)
    {
        registeredUITargets.Remove(t);
    }

    // === REGISTRO DE TARGETS MUNDO ===
    public void Register(WorldTargetable t)
    {
        if (t != null && !registeredWorldTargets.Contains(t))
        {
            registeredWorldTargets.Add(t);
        }
    }

    public void Unregister(WorldTargetable t)
    {
        registeredWorldTargets.Remove(t);
    }

    /// <summary>
    /// Inicia modo selección con tipos permitidos (sin prompt custom)
    /// </summary>
    public void BeginTargeting(IEnumerable<TargetType> allowed, Action<TargetSelection> callback)
    {
        BeginTargeting(allowed, null, callback);
    }

    /// <summary>
    /// Inicia modo selección con tipos permitidos y un mensaje de prompt opcional
    /// </summary>
    public void BeginTargeting(IEnumerable<TargetType> allowed, string prompt, Action<TargetSelection> callback)
    {
        allowedTypes = new HashSet<TargetType>(allowed);
        onTargetChosen = callback;
        isActive = true;

        // Ocultar cualquier preview abierta inmediatamente
        if (CardPreviewManager.Instance != null)
        {
            CardPreviewManager.Instance.HideImmediate();
        }
        else
        {
            // fallback por si no tenemos singleton
            var anyPrev = GameObject.FindObjectOfType<CardPreviewManager>();
            if (anyPrev != null) anyPrev.HideImmediate();
        }
        // Ocultar también el preview de UI de cartas (lado izquierdo) si está abierto
        if (CardPreviewUI.Instance != null)
        {
            CardPreviewUI.Instance.HidePreview();
        }

        // Crear/mostrar overlay de oscurecimiento para el foco de objetivos
        EnsureOverlay();
        if (overlayImage != null)
        {
            overlayImage.color = overlayColor;
            overlayImage.raycastTarget = false; // no bloquear clicks a objetivos
            overlayImage.enabled = true;
            overlayRect.gameObject.SetActive(true);
            // Asegurar que quede detrás de futuros elementos en el mismo canvas
            overlayRect.SetAsFirstSibling();
        }
        // Prompt
        if (overlayPromptText != null)
        {
            overlayPromptText.text = string.IsNullOrEmpty(prompt) ? defaultPrompt : prompt;
            if (promptFont != null) overlayPromptText.font = promptFont;
            overlayPromptText.color = promptColor;
            overlayPromptText.fontSize = promptFontSize;
            overlayPromptText.alignment = promptAlignment;
            // Reaplicar tamaño/posición por si cambiaron en inspector
            var tr = overlayPromptText.rectTransform;
            tr.sizeDelta = promptSize;
            tr.anchoredPosition = promptAnchoredPosition;
            overlayPromptText.enabled = true;
        }

        // Si no hay registros (por timing), intentar detectar en escena
        if (registeredUITargets.Count == 0)
        {
            var uiTargets = FindObjectsOfType<Targetable>(includeInactive: false);
            foreach (var t in uiTargets)
            {
                if (!registeredUITargets.Contains(t)) registeredUITargets.Add(t);
            }
        }
        if (registeredWorldTargets.Count == 0)
        {
            var worldTargets = FindObjectsOfType<WorldTargetable>(includeInactive: false);
            foreach (var t in worldTargets)
            {
                if (!registeredWorldTargets.Contains(t)) registeredWorldTargets.Add(t);
            }
        }

        // Resaltar objetivos válidos (UI)
        foreach (var t in registeredUITargets)
        {
            bool can = allowedTypes.Contains(t.targetType);
            t.Highlight(can);
            t.SetInteractable(can);
        }

        // Resaltar objetivos válidos (Mundo)
        foreach (var t in registeredWorldTargets)
        {
            bool can = allowedTypes.Contains(t.targetType);
            t.Highlight(can);
            t.SetInteractable(can);
        }

        Debug.Log($"[TargetingManager] Selección iniciada. Tipos permitidos: {string.Join(", ", allowed)}");
    }

    /// <summary>
    /// Cancela la selección actual
    /// </summary>
    public void CancelTargeting()
    {
        isActive = false;
        onTargetChosen = null;
        allowedTypes.Clear();

        // Limpiar UI targets
        foreach (var t in registeredUITargets)
        {
            t.Highlight(false);
            t.SetInteractable(false);
        }

        // Limpiar World targets
        foreach (var t in registeredWorldTargets)
        {
            t.Highlight(false);
            t.SetInteractable(false);
        }

        // Ocultar overlay y prompt
        if (overlayRect != null)
        {
            overlayRect.gameObject.SetActive(false);
        }
        if (overlayPromptText != null)
        {
            overlayPromptText.enabled = false;
        }

        // Asegurar limpiar resaltes residuales
        foreach (var t in registeredUITargets)
        {
            t.Highlight(false);
        }
        foreach (var t in registeredWorldTargets)
        {
            t.Highlight(false);
        }

        Debug.Log("[TargetingManager] Selección cancelada");
    }

    /// <summary>
    /// Verifica si un tipo de objetivo está permitido actualmente
    /// </summary>
    public bool IsTargetTypeAllowed(TargetType type)
    {
        return isActive && allowedTypes.Contains(type);
    }

    // === CLICK EN TARGET UI ===
    public void TargetClicked(Targetable t)
    {
        if (!isActive) return;
        if (!allowedTypes.Contains(t.targetType)) return;

        var selection = t.BuildSelection();
        CompleteTargeting(selection);
    }

    // === CLICK EN TARGET MUNDO ===
    public void TargetClicked(WorldTargetable t)
    {
        if (!isActive) return;
        if (!allowedTypes.Contains(t.targetType)) return;

        var selection = t.BuildSelection();
        CompleteTargeting(selection);
    }

    /// <summary>
    /// Completa el proceso de targeting y ejecuta el callback
    /// </summary>
    private void CompleteTargeting(TargetSelection selection)
    {
        Debug.Log($"[TargetingManager] Objetivo seleccionado: {selection.targetType}");

        // Guardar callback antes de limpiar
        var callback = onTargetChosen;

        // Limpiar estado visual
        CancelTargeting();

        // Invocar callback
        callback?.Invoke(selection);
    }

    private void EnsureOverlay()
    {
        if (overlayRect != null) return;

        // Intentar usar la capa de animación para garantizar screen-space superior
        RectTransform parent = null;
        if (GameManager.Instance != null)
        {
            parent = GameManager.Instance.GetAnimationLayerRect();
        }
        if (parent == null)
        {
            var anyCanvas = FindObjectOfType<Canvas>();
            parent = anyCanvas != null ? anyCanvas.transform as RectTransform : null;
        }
        if (parent == null) return; // sin canvas, no creamos overlay

        var go = new GameObject("TargetingOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        overlayRect = go.GetComponent<RectTransform>();
        overlayImage = go.GetComponent<UnityEngine.UI.Image>();
        overlayRect.SetParent(parent, worldPositionStays: false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.pivot = new Vector2(0.5f, 0.5f);
        overlayRect.anchoredPosition = Vector2.zero;
        overlayRect.sizeDelta = Vector2.zero;
        overlayImage.enabled = false;
        overlayRect.gameObject.SetActive(false);

        // Crear texto de prompt como hijo, anclado arriba centro
        var textGO = new GameObject("TargetingPrompt", typeof(RectTransform));
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.SetParent(overlayRect, worldPositionStays: false);
        textRect.anchorMin = new Vector2(0.5f, 1f);
        textRect.anchorMax = new Vector2(0.5f, 1f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.anchoredPosition = promptAnchoredPosition;
        textRect.sizeDelta = promptSize;
        overlayPromptText = textGO.AddComponent<TextMeshProUGUI>();
        overlayPromptText.text = defaultPrompt;
        if (promptFont != null) overlayPromptText.font = promptFont;
        overlayPromptText.alignment = promptAlignment;
        overlayPromptText.fontSize = promptFontSize;
        overlayPromptText.color = promptColor;
        overlayPromptText.enableWordWrapping = false;
        overlayPromptText.enabled = false;
    }
}
