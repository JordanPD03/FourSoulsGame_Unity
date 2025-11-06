using UnityEngine;

/// <summary>
/// Efecto que permite tirar un dado.
/// </summary>
[CreateAssetMenu(fileName = "RollDiceEffect", menuName = "Four Souls/Effects/Roll Dice")]
public class RollDiceEffect : CardEffect
{
    [Header("Roll Dice Settings")]
    [Tooltip("Número de caras del dado (por defecto 6)")]
    public int diceSides = 6;

    [Tooltip("Descripción de qué pasa con cada resultado (para referencia)")]
    [TextArea(3, 5)]
    public string resultDescription = "1-2: Nada\n3-4: +1 moneda\n5-6: +2 monedas";

    protected override void ExecuteInternal(PlayerData player, CardData card, GameManager gameManager, TargetSelection target)
    {
        if (player == null || gameManager == null)
        {
            Debug.LogWarning("RollDiceEffect: player o gameManager es null");
            return;
        }

        Debug.Log($"[RollDiceEffect] {player.playerName} tira un dado de {diceSides} caras");

        gameManager.RollDice(diceSides, (result) =>
        {
            Debug.Log($"[RollDiceEffect] Resultado: {result}");
            // Los efectos específicos basados en el resultado se implementarán
            // con efectos más específicos o en las cartas individuales
            ProcessDiceResult(result, player, card, gameManager);
        });
    }

    /// <summary>
    /// Procesa el resultado del dado.
    /// Override este método en efectos personalizados para lógica específica.
    /// </summary>
    protected virtual void ProcessDiceResult(int result, PlayerData player, CardData card, GameManager gameManager)
    {
        Debug.Log($"[RollDiceEffect] Procesando resultado {result} (sin efecto específico)");
        // Las cartas específicas pueden tener su propia lógica
    }

    public override string GetDescription()
    {
        return $"Tira un dado de {diceSides}";
    }
}
