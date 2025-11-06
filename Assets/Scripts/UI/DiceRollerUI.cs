using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI para lanzar un dado en una ventana centrada con animación.
/// - Puede usar sprites de caras (1..N) o modo numérico de respaldo.
/// - Muestra una superposición que bloquea la interacción temporalmente.
/// - Devuelve el resultado vía callback al finalizar.
/// </summary>
public class DiceRollerUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup overlayGroup;   // Panel de fondo semitransparente
    [SerializeField] private RectTransform windowRoot;   // Ventana centrada
    [SerializeField] private Image diceImage;            // Imagen del dado

    [Header("Sprites de caras")]
    [Tooltip("Lista de sprites para las caras del dado. El índice 0 representa la cara 1.")]
    [SerializeField] private List<Sprite> diceFaceSprites = new List<Sprite>();

    [Header("Animación")]
    [SerializeField] private float overlayFadeDuration = 0.15f;
    [SerializeField] private float windowPopScale = 1.06f;
    [SerializeField] private float windowPopDuration = 0.18f;
    [SerializeField] private float rollDuration = 1.0f;   // duración total del "agitado"
    [SerializeField] private float faceShuffleInterval = 0.06f; // tiempo entre cambios de cara
    [SerializeField] private float resultHoldDuration = 0.8f;   // cuánto tiempo se muestra antes de ocultar

    private bool isRolling = false;
    public bool IsOverlayVisible => overlayGroup != null && overlayGroup.alpha > 0.99f;

    private void Reset()
    {
        // Intentar autocompletar referencias al crear el componente
        overlayGroup = GetComponentInChildren<CanvasGroup>();
        if (overlayGroup != null)
        {
            overlayGroup.alpha = 0;
            overlayGroup.blocksRaycasts = false;
            overlayGroup.interactable = false;
        }
    }

    private void Awake()
    {
        if (overlayGroup != null)
        {
            overlayGroup.alpha = 0f;
            overlayGroup.blocksRaycasts = false;
            overlayGroup.interactable = false;
        }
        if (windowRoot != null)
        {
            windowRoot.localScale = Vector3.one;
            windowRoot.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Lanza un dado de "sides" caras y muestra una animación.
    /// Invoca el callback con el resultado (1..sides) al finalizar.
    /// </summary>
    public void RollDice(int sides = 6, Action<int> onComplete = null)
    {
        if (isRolling)
            return;
        if (sides < 2)
            sides = 2;

        StartCoroutine(RollRoutine(sides, onComplete));
    }

    private IEnumerator RollRoutine(int sides, Action<int> onComplete)
    {
        isRolling = true;

        // Preparar UI
        if (diceImage != null)
        {
            diceImage.enabled = true;
            // Establecer sprite inicial si hay disponibles
            if (diceFaceSprites != null && diceFaceSprites.Count > 0)
                diceImage.sprite = diceFaceSprites[0];
        }

        if (windowRoot != null) windowRoot.gameObject.SetActive(true);

        if (overlayGroup != null)
        {
            overlayGroup.DOKill();
            overlayGroup.blocksRaycasts = true;
            overlayGroup.interactable = true;
            yield return overlayGroup.DOFade(1f, overlayFadeDuration).WaitForCompletion();
        }

        if (windowRoot != null)
        {
            windowRoot.DOKill();
            windowRoot.localScale = Vector3.one;
            yield return windowRoot.DOPunchScale(Vector3.one * (windowPopScale - 1f), windowPopDuration, 10, 0.8f)
                                   .SetUpdate(true)
                                   .WaitForCompletion();
        }

        // Animación de "agitar" y permutar caras rápidamente
        float t = 0f;
        float nextShuffle = 0f;
        System.Random rng = new System.Random();

        // Si no hay sprites suficientes o sides>sprites, usaremos texto numérico
        bool useNumeric = diceFaceSprites == null || diceFaceSprites.Count < Mathf.Min(6, sides);

        // Pequeño shake
        if (windowRoot != null)
        {
            windowRoot.DOShakeRotation(rollDuration, new Vector3(0, 0, 10f), vibrato: 20, randomness: 90f, fadeOut: true);
        }

        while (t < rollDuration)
        {
            t += Time.unscaledDeltaTime; // UI animaciones en tiempo no escalado
            nextShuffle -= Time.unscaledDeltaTime;
            if (nextShuffle <= 0f)
            {
                int face = rng.Next(1, sides + 1);
                ApplyFace(face, useNumeric);
                nextShuffle = faceShuffleInterval;
            }
            yield return null;
        }

        // Resultado final
        int result = UnityEngine.Random.Range(1, sides + 1);
        ApplyFace(result, useNumeric);

        // Pop del resultado
        if (diceImage != null && diceImage.enabled)
            diceImage.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 10, 0.9f);

        yield return new WaitForSecondsRealtime(resultHoldDuration);

        // Ocultar
        if (overlayGroup != null)
        {
            overlayGroup.DOKill();
            yield return overlayGroup.DOFade(0f, overlayFadeDuration).WaitForCompletion();
            overlayGroup.blocksRaycasts = false;
            overlayGroup.interactable = false;
        }
        if (windowRoot != null)
        {
            windowRoot.gameObject.SetActive(false);
        }

        isRolling = false;
        onComplete?.Invoke(result);
    }

    private void ApplyFace(int value, bool useNumeric)
    {
        if (!useNumeric && diceImage != null && diceFaceSprites != null && diceFaceSprites.Count >= value)
        {
            // Mostrar sprite de cara
            diceImage.enabled = true;
            diceImage.sprite = diceFaceSprites[value - 1]; // índice 0 => cara 1
        }
        else
        {
            // Modo sin sprites: mostrar sprite por defecto o último disponible
            if (diceImage != null)
            {
                diceImage.enabled = true;
                // Si hay al menos un sprite, usar el primero como placeholder
                if (diceFaceSprites != null && diceFaceSprites.Count > 0)
                {
                    diceImage.sprite = diceFaceSprites[0];
                }
            }
        }
    }

    /// <summary>
    /// Muestra u oculta el overlay de fondo con la misma animación usada al lanzar el dado.
    /// Puede usarse por otros flujos (p.ej., selección de descarte) para unificar estilo visual.
    /// </summary>
    public void SetOverlay(bool on)
    {
        if (overlayGroup == null) return;
        overlayGroup.DOKill();
        overlayGroup.blocksRaycasts = on;
        overlayGroup.interactable = on;
        float target = on ? 1f : 0f;
        overlayGroup.DOFade(target, overlayFadeDuration).SetUpdate(true);
        if (!on && windowRoot != null)
        {
            // Asegurar que la ventana del dado no quede activa accidentalmente si solo queríamos el overlay
            windowRoot.gameObject.SetActive(false);
        }
    }
}
