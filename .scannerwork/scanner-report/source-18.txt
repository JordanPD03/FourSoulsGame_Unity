using UnityEngine;

/// <summary>
/// Clase base abstracta para todos los efectos de cartas.
/// Permite crear efectos modulares y reutilizables.
/// </summary>
public abstract class CardEffect : ScriptableObject
{
    [TextArea(2, 4)]
    [Tooltip("Descripción del efecto (para debugging)")]
    public string effectName = "Card Effect";

    /// <summary>
    /// Indica si este efecto requiere que el jugador seleccione un objetivo.
    /// </summary>
    public virtual bool RequiresTarget => false;

    /// <summary>
    /// Tipos de objetivos permitidos para este efecto (si RequiresTarget == true)
    /// </summary>
    public virtual TargetType[] AllowedTargets => System.Array.Empty<TargetType>();

    /// <summary>
    /// Ejecuta el efecto de la carta. Puede recibir un objetivo opcional.
    /// </summary>
    public void Execute(PlayerData player, CardData card, GameManager gameManager, TargetSelection target = null)
    {
        ExecuteInternal(player, card, gameManager, target);
    }

    /// <summary>
    /// Implementación concreta del efecto (con objetivo opcional)
    /// </summary>
    protected abstract void ExecuteInternal(PlayerData player, CardData card, GameManager gameManager, TargetSelection target);

    /// <summary>
    /// Verifica si el efecto puede ser ejecutado.
    /// </summary>
    public virtual bool CanExecute(PlayerData player, CardData card, GameManager gameManager)
    {
        return true;
    }

    /// <summary>
    /// Obtiene una descripción legible del efecto.
    /// </summary>
    public virtual string GetDescription()
    {
        return effectName;
    }
}
