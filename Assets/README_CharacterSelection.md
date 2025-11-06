# Sistema de Selección de Personajes

## Descripción General

Sistema de selección de personajes que se ejecuta al inicio de la partida antes de comenzar el juego. Incluye animaciones, temporizador y asignación automática de stats y objetos eternos.

## Flujo del Sistema

### 1. Inicio de la Partida
- `GameManager.InitializeGame()` detecta `CharacterSelectionUI.Instance`
- Pausa la inicialización normal del juego
- Inicia el proceso de selección de personajes

### 2. Selección por Jugador (Secuencial)
Para cada jugador, en orden:

1. **Fade In**: Fondo oscuro (overlay) aparece gradualmente
2. **Cartas Boca Abajo**: 2 cartas de personaje aleatorias aparecen volteadas
3. **Revelación**: Las cartas se voltean con animación 3D (flip en eje X)
4. **Temporizador**: Cuenta regresiva de 30 segundos
5. **Selección**: 
   - Jugador hace clic en su personaje preferido
   - Si no selecciona antes del tiempo límite → selección aleatoria
6. **Objetos Eternos**: Muestra los ítems iniciales del personaje seleccionado
7. **Confirmación**: Espera 2 segundos para visualizar
8. **Fade Out**: Transición al siguiente jugador

### 3. Aplicación de Selecciones
Una vez todos los jugadores eligieron:
- Se asignan los `CharacterType` a cada `PlayerData`
- Se configuran stats iniciales (HP, monedas, ataque)
- Se agregan objetos eternos a inventarios de jugadores
- El juego continúa con la configuración normal

## Archivos Creados

### 1. CharacterDataSO.cs
ScriptableObject que define un personaje jugable:
```csharp
- characterType: CharacterType (enum)
- characterName: string
- characterCardFront: Sprite (carta revelada)
- characterCardBack: Sprite (carta oculta)
- startingHealth: int
- startingCoins: int
- startingAttack: int
- eternalItems: List<CardDataSO> (objetos iniciales)
- abilityDescription: string
```

### 2. CharacterSelectionUI.cs
Controlador principal del sistema de selección:

**Componentes Principales:**
- `selectionPanel`: GameObject contenedor de toda la UI
- `overlay`: Image para oscurecer el fondo
- `canvasGroup`: Para animaciones de fade
- `cardContainer`: Transform donde aparecen las cartas
- `eternalItemsContainer`: Transform para mostrar objetos eternos
- `statusText`: TMP_Text con instrucciones
- `timerText`: TMP_Text con cuenta regresiva
- `playerNameText`: TMP_Text con nombre del jugador actual

**Configuración:**
- `selectionTimeLimit`: float (default 30s)
- `availableCharacters`: List<CharacterDataSO> (pool de personajes)

**Métodos Públicos:**
- `StartCharacterSelection(players, onComplete)`: Inicia el proceso
- `OnCharacterSelected(character)`: Callback al hacer clic en carta

### 3. CharacterCard.cs
Componente para cartas individuales clickeables:
- Maneja el evento `onClick` del botón
- Comunica selección a `CharacterSelectionUI`
- Feedback visual (escala pulsante)

## Integración en GameManager

```csharp
private void InitializeGame()
{
    EnsurePlayersInitialized();
    
    if (CharacterSelectionUI.Instance != null)
    {
        CharacterSelectionUI.Instance.StartCharacterSelection(
            players, 
            OnCharacterSelectionComplete
        );
    }
    else
    {
        ContinueGameSetup();
    }
}

private void OnCharacterSelectionComplete()
{
    ContinueGameSetup();
}

private void ContinueGameSetup()
{
    CreateTestCards();
    SetupStartingResources();
    RebuildCurrentPlayerHandUI();
    StartPlayerTurn(0);
}
```

## Configuración en Unity

### 1. Crear Prefabs
- **CharacterCardPrefab**: Carta con Image + Button
- **EternalItemCardPrefab**: Carta pequeña para ítems

### 2. Crear UI Canvas
Jerarquía recomendada:
```
CharacterSelectionCanvas
├── SelectionPanel
│   ├── Overlay (Image - negro semi-transparente)
│   ├── CardContainer (contenedor horizontal)
│   ├── EternalItemsContainer (contenedor horizontal)
│   ├── PlayerNameText (TMP)
│   ├── StatusText (TMP)
│   └── TimerText (TMP)
```

### 3. Configurar CharacterSelectionUI
1. Añadir componente `CharacterSelectionUI` al Canvas
2. Asignar referencias en el Inspector:
   - Selection Panel
   - Overlay
   - Canvas Group
   - Card Container
   - Eternal Items Container
   - Textos (Status, Timer, Player Name)
   - Prefabs (Character Card, Eternal Item)
3. Ajustar tiempo límite (default 30s)
4. Crear CharacterDataSO para cada personaje

### 4. Crear CharacterDataSO (ScriptableObjects)
Ruta: `Assets/Create/Four Souls/Character Data`

Por cada personaje:
1. Configurar nombre y tipo
2. Asignar sprites (frente/dorso)
3. Establecer stats iniciales
4. Agregar objetos eternos (CardDataSO)
5. Escribir descripción de habilidad

### 5. Asignar en CharacterSelectionUI
Agregar todos los CharacterDataSO al array `availableCharacters`

## Animaciones Incluidas

### Fade In/Out
- Duración: 0.5s
- Alpha: 0 → 1 → 0

### Aparición de Cartas
- Escala inicial: 0
- Escala final: 1
- Ease: OutBack
- Retraso entre cartas: 0.2s

### Flip de Cartas
1. Escala X: 1 → 0 (0.2s)
2. Cambio de sprite (dorso → frente)
3. Escala X: 0 → 1 (0.2s)
4. Retraso entre cartas: 0.5s

### Objetos Eternos
- Escala inicial: 0
- Escala final: 0.8
- Ease: OutBack
- Retraso entre ítems: 0.15s

### Selección (Feedback)
- Escala pulsante: 1 → 1.1 → 1
- Loops: 2 (Yoyo)
- Duración: 0.2s

## Advertencias de Tiempo

Cuando quedan ≤10 segundos:
- `timerText.color = Color.red`
- El texto se vuelve rojo para advertir

## Selección Aleatoria

Si `remainingTime <= 0` y no hay selección:
```csharp
selectedCharacter = availableCharacters[Random.Range(0, count)];
```

## Aplicación de Stats

Al finalizar la selección:
```csharp
player.character = charData.characterType;
player.health = charData.startingHealth;
player.maxHealth = charData.startingHealth;
player.coins = charData.startingCoins;
player.attackDamage = charData.startingAttack;

// Objetos eternos
foreach (itemSO in charData.eternalItems)
{
    CardData item = new CardData(itemSO);
    item.isEternal = true;
    
    if (item.cardType == CardType.ActiveItem)
        player.activeItems.Add(item);
    else if (item.cardType == CardType.PassiveItem)
        player.passiveItems.Add(item);
}
```

## Personalización

### Cambiar Tiempo Límite
En el Inspector de `CharacterSelectionUI`:
- `Selection Time Limit` (segundos)

### Cambiar Cantidad de Cartas
En `GetTwoRandomCharacters()`:
```csharp
for (int i = 0; i < 2; i++) // Cambiar 2 por cantidad deseada
```

### Espaciado entre Cartas
En el Inspector:
- `Card Spacing` (unidades UI)

### Personalizar Animaciones
Modificar duraciones/ease en métodos:
- `ShowCardsFlipped()`
- `RevealCards()`
- `ShowEternalItems()`

## Notas Técnicas

- Sistema completamente asíncrono (Coroutines)
- No bloquea el thread principal
- Singleton pattern para acceso global
- Soporta múltiples jugadores (secuencial)
- Las cartas son destruidas después de cada jugador
- El overlay se reutiliza entre jugadores
- Stats se aplican al final (no en tiempo real)

## Posibles Mejoras Futuras

1. **Multijugador Simultáneo**: Todos eligen a la vez en pantalla dividida
2. **Filtros**: Categorías de personajes (principiante, avanzado, etc.)
3. **Preview 3D**: Modelo 3D del personaje al hover
4. **Animaciones de Habilidad**: Mostrar efecto visual de habilidad especial
5. **Confirmación**: Botón "Confirmar" en vez de clic directo
6. **Historial**: Mostrar qué personajes eligieron otros jugadores
7. **Música/SFX**: Sonidos para flip, selección, tiempo agotándose
8. **Banning**: Permitir banear personajes antes de elegir
9. **Random All**: Botón para asignar todos aleatoriamente
