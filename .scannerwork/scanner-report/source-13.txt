using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Gestiona la TIENDA de tesoros en el tablero: N cartas reveladas que los jugadores pueden comprar por 10 monedas.
/// Similar al sistema de monstruos: usa slots precolocados en la escena.
/// Todos los slots permanecen visibles; solo se rellenan tantos como haya en la tienda.
/// </summary>
public class TreasureShopManager : MonoBehaviour
{
    public static TreasureShopManager Instance { get; private set; }

    [Header("Shop Slots (Precolocados en escena)")]
    [Tooltip("Lista de TreasureSlot en la escena (máximo 4). Se muestran todos; se rellenan según la tienda.")]
    [SerializeField] private List<TreasureSlot> treasureSlots = new List<TreasureSlot>();
    
    [Header("Configuration")]
    [Tooltip("Slots de tienda iniciales (cantidad de cartas reveladas) - visualmente se muestran todos los slots")]
    [SerializeField] private int initialSlotCount = 2;
    
    [Header("Prefab para instanciar cartas (Opcional)")]
    [Tooltip("Prefab de carta para mostrar los tesoros revelados (debe tener CardUI). Opcional si usas solo treasureImage.")]
    [SerializeField] private GameObject treasureCardPrefab;

    [Header("Deck Buy UI (Opcional)")]
    [Tooltip("Botón para comprar del mazo de tesoros")]
    [SerializeField] private Button buyFromDeckButton;
    [Tooltip("Texto de feedback (opcional)")]
    [SerializeField] private TMP_Text feedbackText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Indexar los slots
        for (int i = 0; i < treasureSlots.Count; i++)
        {
            if (treasureSlots[i] != null)
            {
                treasureSlots[i].SetIndex(i);
                // Suscribirse a clicks de cada slot
                treasureSlots[i].OnClicked += TryBuySlot;
                // Asegurar visibles en escena (no ocultamos slots por tamaño de tienda)
                treasureSlots[i].gameObject.SetActive(true);
                Debug.Log($"[TreasureShopManager] Slot {i} indexado y suscrito a OnClicked");
            }
        }

        // No desactivamos slots: los mostramos todos y se rellenan según GameManager

        // Suscribir a eventos
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnShopUpdated += HandleShopUpdated;
        }

        if (buyFromDeckButton != null)
        {
            buyFromDeckButton.onClick.AddListener(OnBuyFromDeckClicked);
        }

        // Refrescar estado inicial
        RefreshFromGameState();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnShopUpdated -= HandleShopUpdated;
        }
        if (buyFromDeckButton != null)
        {
            buyFromDeckButton.onClick.RemoveListener(OnBuyFromDeckClicked);
        }
    }

    // Nota: ya no activamos/desactivamos slots; todos permanecen visibles

    private void HandleShopUpdated(List<CardData> revealed)
    {
        // Actualizar todos los slots: rellenar los primeros N con las cartas reveladas y vaciar el resto
        for (int i = 0; i < treasureSlots.Count; i++)
        {
            var slot = treasureSlots[i];
            if (slot == null) continue;

            CardData card = (revealed != null && i < revealed.Count) ? revealed[i] : null;

            if (card != null)
            {
                if (treasureCardPrefab != null)
                {
                    // Crear objeto visual de carta si se desea usar un prefab
                    var cardGO = Instantiate(treasureCardPrefab, slot.transform);
                    var cardUI = cardGO.GetComponent<CardUI>();
                    if (cardUI != null)
                    {
                        Sprite front = card.frontSprite;
                        if (front == null && !string.IsNullOrEmpty(card.frontSpritePath))
                        {
                            front = Resources.Load<Sprite>(card.frontSpritePath);
                        }
                        cardUI.SetCardData(card, front, null);
                    }
                    slot.SetTreasure(card, cardGO);
                }
                else
                {
                    // Solo sprite del slot
                    slot.SetTreasure(card, null);
                }
            }
            else
            {
                // Sin carta para este índice → mostrar placeholder vacío
                slot.ClearTreasure();
            }
        }
    }

    private void RefreshFromGameState()
    {
        if (GameManager.Instance == null) return;
        var revealed = GameManager.Instance.GetShopCards();
        HandleShopUpdated(revealed);
    }

    private void OnBuyFromDeckClicked()
    {
        if (GameManager.Instance == null) return;
        var player = GameManager.Instance.GetCurrentPlayer();
        if (player == null) return;
        if (GameManager.Instance.TryBuyFromTreasureDeck(player, out var bought, out var reason))
        {
            ShowFeedback($"Compraste del mazo: {bought.cardName}");
        }
        else
        {
            ShowFeedback(reason ?? "No se pudo comprar del mazo");
        }
    }

    private void ShowFeedback(string msg)
    {
        if (feedbackText != null && !string.IsNullOrEmpty(msg))
        {
            feedbackText.text = msg;
        }
    }

    public void TryBuySlot(int index)
    {
        Debug.Log($"[TreasureShopManager] TryBuySlot llamado para índice {index}");
        if (GameManager.Instance == null) return;
        if (index < 0 || index >= treasureSlots.Count) return;
        var slot = treasureSlots[index];
        if (slot == null || !slot.HasTreasure()) return;

        var card = slot.GetTreasure();
        Debug.Log($"[TreasureShopManager] Abriendo preview para carta: {card.cardName}");
        // Abrir preview en modo Tienda con botones Comprar/Cancelar
        if (CardPreviewUI.Instance != null)
        {
            CardPreviewUI.Instance.ShowShopTreasure(card, index);
        }
    }

    /// <summary>
    /// Expande la tienda añadiendo más slots activos (hasta el máximo de 4)
    /// </summary>
    public bool ExpandShop(int newSlotCount)
    {
        // Visualmente no cambiamos activación; refrescamos desde el estado del GameManager
        RefreshFromGameState();
        return true;
    }

    public int GetActiveSlotCount()
    {
        // El recuento activo ahora es igual al número de cartas reveladas en GameManager
        if (GameManager.Instance == null) return 0;
        return GameManager.Instance.GetShopCards()?.Count ?? 0;
    }
}
