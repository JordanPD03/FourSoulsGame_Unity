using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Registro sencillo para localizar componentes de UI por jugador.
/// Actualmente soporta:
/// - PlayerStatsUI (HUD legacy)
/// - PlayerBoardDisplay (World-space board UI)
/// </summary>
public class UIRegistry : MonoBehaviour
{
    public static UIRegistry Instance { get; private set; }

    private readonly Dictionary<int, PlayerStatsUI> playerStatsByIndex = new Dictionary<int, PlayerStatsUI>();
    private readonly Dictionary<int, PlayerBoardDisplay> playerBoardById = new Dictionary<int, PlayerBoardDisplay>();
    private readonly Dictionary<CardType, DiscardPileUI> discardPileByType = new Dictionary<CardType, DiscardPileUI>();

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

    public void RegisterPlayerStats(int playerIndex, PlayerStatsUI stats)
    {
        if (stats == null) return;
        playerStatsByIndex[playerIndex] = stats;
    }

    public void UnregisterPlayerStats(int playerIndex, PlayerStatsUI stats)
    {
        if (playerStatsByIndex.TryGetValue(playerIndex, out var existing) && existing == stats)
        {
            playerStatsByIndex.Remove(playerIndex);
        }
    }

    public bool TryGetPlayerStats(int playerIndex, out PlayerStatsUI stats)
    {
        return playerStatsByIndex.TryGetValue(playerIndex, out stats);
    }

    // World-space Board UI registration
    public void RegisterPlayerBoard(int playerId, PlayerBoardDisplay board)
    {
        if (board == null) return;
        playerBoardById[playerId] = board;
    }

    public void UnregisterPlayerBoard(int playerId, PlayerBoardDisplay board)
    {
        if (playerBoardById.TryGetValue(playerId, out var existing) && existing == board)
        {
            playerBoardById.Remove(playerId);
        }
    }

    public bool TryGetPlayerBoard(int playerId, out PlayerBoardDisplay board)
    {
        return playerBoardById.TryGetValue(playerId, out board);
    }

    public void RegisterDiscardPile(CardType type, DiscardPileUI pile)
    {
        if (pile == null) return;
        discardPileByType[type] = pile;
    }

    public void UnregisterDiscardPile(CardType type, DiscardPileUI pile)
    {
        if (discardPileByType.TryGetValue(type, out var existing) && existing == pile)
        {
            discardPileByType.Remove(type);
        }
    }

    public bool TryGetDiscardPile(CardType type, out DiscardPileUI pile)
    {
        return discardPileByType.TryGetValue(type, out pile);
    }
}
