using UnityEngine;

/// <summary>
/// Componente helper que almacena las posiciones base de una carta
/// para que CardHover pueda calcular correctamente las posiciones de hover
/// </summary>
public class CardPositionTracker : MonoBehaviour
{
    [HideInInspector]
    public Vector2 basePosition;  // Posición sin ningún hover
    
    [HideInInspector]
    public Vector2 baseHoverPosition; // Posición con hover base de la mano (pero sin hover individual)
}
