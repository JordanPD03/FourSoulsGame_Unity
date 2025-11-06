using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DrawCardController : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerHandUI playerHand;          // Mano del jugador (contenedor)
    public GameObject cardPrefab;            // Prefab de la carta UI
    public RectTransform animationLayer;     // Capa UI para la animación (puede ser el Canvas root)

    [Header("Dorso de carta")]
    public Sprite cardBackSprite;            // Dorso para animación

    [Header("Animación de robo")]
    public Vector2 animCardSize = new Vector2(220, 308);  // Tamaño manual por defecto (más pequeño)
    public bool usePrefabSizeForAnim = true;              // Usar tamaño del prefab escalado
    [Range(0.1f, 1.0f)]
    public float prefabSizeAnimScale = 0.4f;              // Escala del tamaño del prefab para la animación
    public float slideDuration = 0.6f;
    public Ease slideEase = Ease.InQuad;
    public float appearScale = 0.9f; // efecto pequeño al aparecer en la mano
    public float appearDuration = 0.2f;

    private bool isDrawing = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryDrawCard();
        }
    }

    public void TryDrawCard()
    {
        if (isDrawing) return;
        if (playerHand == null || cardPrefab == null)
        {
            Debug.LogWarning("DrawCardController: falta asignar playerHand o cardPrefab.");
            return;
        }

        // Verificar que el GameManager existe
        if (GameManager.Instance == null)
        {
            Debug.LogError("DrawCardController: GameManager no encontrado en la escena!");
            return;
        }

        // Validar que sea la fase de robo
        if (GameManager.Instance.GetCurrentPhase() != GamePhase.Draw)
        {
            Debug.LogWarning($"DrawCardController: No puedes robar cartas durante la fase {GameManager.Instance.GetCurrentPhase()}!");
            return;
        }

        // Robar carta del mazo usando el GameManager
        PlayerData currentPlayer = GameManager.Instance.GetCurrentPlayer();
        CardData drawnCard = GameManager.Instance.DrawCard(DeckType.Loot, currentPlayer);

        if (drawnCard == null)
        {
            Debug.Log("DrawCardController: no hay más cartas en el deck.");
            return;
        }

        // Iniciar animación con la carta robada
        StartCoroutine(DrawCardAnimationRoutine(drawnCard));
        
        // Cambiar a fase de acción después de robar
        GameManager.Instance.ChangePhase(GamePhase.Action);
    }

    private IEnumerator DrawCardAnimationRoutine(CardData cardData)
    {
        isDrawing = true;

        // Asegurar capa de animación
        RectTransform layer = animationLayer;
        if (layer == null)
        {
            Canvas canvas = playerHand.GetComponentInParent<Canvas>();
            if (canvas != null)
                layer = canvas.transform as RectTransform;
        }
        if (layer == null)
        {
            Debug.LogWarning("DrawCardController: no se encontró una capa para animación (Canvas).");
            isDrawing = false;
            yield break;
        }

        // Determinar tamaño de carta para la animación
        Vector2 size = animCardSize;
        RectTransform prefabRect = cardPrefab.GetComponent<RectTransform>();
        if (usePrefabSizeForAnim && prefabRect != null && prefabRect.sizeDelta != Vector2.zero)
        {
            size = prefabRect.sizeDelta * prefabSizeAnimScale;
        }

        // Crear objeto temporal para animación
        GameObject temp = new GameObject("DrawAnimCard", typeof(RectTransform), typeof(Image));
        RectTransform tempRect = temp.GetComponent<RectTransform>();
        tempRect.SetParent(layer, worldPositionStays: false);
        tempRect.anchorMin = new Vector2(0.5f, 0.5f);
        tempRect.anchorMax = new Vector2(0.5f, 0.5f);
        tempRect.pivot = new Vector2(0.5f, 0.5f);
        tempRect.sizeDelta = size;
        tempRect.anchoredPosition = Vector2.zero; // centro de la mesa

        Image tempImg = temp.GetComponent<Image>();
        // Usar el dorso en la animación
        tempImg.sprite = cardBackSprite;
        tempImg.raycastTarget = false;

        // Calcular objetivo fuera de cámara por abajo
        float targetY = -(layer.rect.height * 0.5f + size.y);
        yield return tempRect.DOAnchorPosY(targetY, slideDuration).SetEase(slideEase).WaitForCompletion();

        Destroy(temp);

        // Cargar sprite desde Resources si es necesario
        Sprite cardFrontSprite = null;
        if (!string.IsNullOrEmpty(cardData.frontSpritePath))
        {
            cardFrontSprite = Resources.Load<Sprite>(cardData.frontSpritePath);
        }

        // Instanciar la carta real en la mano del jugador
        GameObject cardGO = Instantiate(cardPrefab, playerHand.transform);

        // Configurar la carta con CardData
        CardUI cardUI = cardGO.GetComponent<CardUI>();
        if (cardUI != null)
        {
            cardUI.SetCardData(cardData, cardFrontSprite, cardBackSprite);
        }
        else
        {
            Debug.LogWarning("DrawCardController: El prefab no tiene componente CardUI!");
        }

        // Añadir a la mano y recolocar
        RectTransform newCardRect = cardGO.GetComponent<RectTransform>();
        if (newCardRect != null)
        {
            // efecto de aparición sutil
            Vector3 originalScale = newCardRect.localScale;
            newCardRect.localScale = originalScale * appearScale;

            playerHand.AddCardToHand(newCardRect);

            // pequeño pop-in
            newCardRect.DOScale(originalScale, appearDuration).SetEase(Ease.OutQuad);
        }

        isDrawing = false;
    }
}
