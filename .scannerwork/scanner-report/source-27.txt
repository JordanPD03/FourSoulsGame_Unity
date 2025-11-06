/// <summary>
/// Enumeración de las fases del turno en Four Souls
/// </summary>
public enum GamePhase
{
    Start,          // Inicio del turno (efectos pasivos de inicio, ítems activos)
    Draw,           // Fase de robo (robar 1 carta de loot obligatoriamente)
    Action,         // Fase de acción (comprar tesoros, atacar monstruos, jugar cartas, terminar turno)
    End             // Fin del turno (descarte por límite de mano, efectos de fin)
}
