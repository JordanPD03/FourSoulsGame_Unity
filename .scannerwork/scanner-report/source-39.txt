using UnityEngine;

/// <summary>
/// Tipos de objetivos posibles para efectos que requieren selección
/// </summary>
public enum TargetType
{
    None = 0,
    DiscardPile = 1,
    Player = 2,
    Monster = 3,
}

/// <summary>
/// Información del objetivo seleccionado por el jugador
/// </summary>
[System.Serializable]
public class TargetSelection
{
    public TargetType targetType = TargetType.None;
    public PlayerData targetPlayer;     // Para objetivos de jugador
    public CardData targetMonsterCard;  // Para objetivos de monstruo (si se usa carta como proxy)

    public static TargetSelection ForPlayer(PlayerData p)
    {
        return new TargetSelection { targetType = TargetType.Player, targetPlayer = p };
    }

    public static TargetSelection ForMonster(CardData m)
    {
        return new TargetSelection { targetType = TargetType.Monster, targetMonsterCard = m };
    }

    public static TargetSelection ForDiscardPile()
    {
        return new TargetSelection { targetType = TargetType.DiscardPile };
    }
}
