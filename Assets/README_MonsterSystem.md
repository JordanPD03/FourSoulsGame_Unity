# Monster System

## Descripción General
Sistema de espacios para monstruos en el tablero que se llenan automáticamente al inicio del juego y se rellenan cuando son derrotados.

## Componentes Principales

### MonsterSlotManager (Singleton)
Gestiona todos los espacios de monstruos en el tablero.

**Campos Importantes:**
- `monsterSlots`: Lista de slots de monstruos
- `monsterCardPrefab`: Prefab para la carta visual del monstruo

**Métodos Principales:**
- `FillInitialSlots()`: Llena todos los slots vacíos al inicio (llamado 0.5s después del Start)
- `DrawMonsterToSlot(MonsterSlot slot)`: Roba un monstruo del mazo Monster y lo coloca en el slot
- `DefeatMonster(MonsterSlot slot, PlayerData playerWhoDefeated)`: Derrota un monstruo (otorga recompensas, limpia slot, rellena)
- `DamageMonster(MonsterSlot slot, int damage)`: Aplica daño a un monstruo (actualiza vida, verifica derrota)

### MonsterSlot
Representa un espacio individual para monstruo en el tablero.

**Campos:**
- `currentMonster`: CardData del monstruo actual (null si vacío)
- `monsterCardObject`: GameObject visual de la carta del monstruo
- `healthText/attackText`: Textos para mostrar vida y ataque
- `cardAnchor`: Transform donde se posiciona la carta del monstruo

**Métodos:**
- `SetMonster(CardData monsterCard, GameObject cardObject)`: Coloca un monstruo en el slot
- `ClearMonster()`: Limpia el slot (destruye visual, resetea datos)
- `UpdateHealthDisplay()`: Actualiza el texto de vida
- `UpdateAttackDisplay()`: Actualiza el texto de ataque
- `HasMonster()`: Verifica si hay un monstruo activo
- `GetMonsterData()`: Obtiene el CardData del monstruo actual

### PlayerStatsPanel
Panel UI que muestra las estadísticas de un jugador.

**Campos:**
- `playerIndex`: Índice del jugador (1, 2, 3)
- UI References: playerNameText, healthText, coinsText, lootCardsText, treasuresText, soulsText
- `backgroundImage`: Imagen de fondo (cambia de color en el turno activo)

**Características:**
- Suscripción automática a eventos de GameManager
- Actualización en tiempo real de todas las estadísticas
- Indicador visual del turno activo (color de fondo)

## Configuración en Unity

### Paso 1: MonsterSlotManager
1. Crear GameObject vacío en la escena: "MonsterSlotManager"
2. Añadir script `MonsterSlotManager`
3. Asignar el prefab de carta de monstruo a `monsterCardPrefab`

### Paso 2: Crear Prefab MonsterCardPrefab

#### Estructura del Prefab:
```
MonsterCardPrefab (GameObject raíz)
├── MonsterCard (SpriteRenderer - imagen de la carta)
└── StatsContainer (GameObject vacío)
    ├── Textos (GameObject vacío)
   │   ├── HealthText (TextMeshPro 3D - vida del monstruo)
   │   ├── DiceText (TextMeshPro 3D - requisito de dado para dañarlo, ej: 3+)
   │   └── AttackText (TextMeshPro 3D - daño de ataque del monstruo)
    └── Iconos (GameObject vacío)
        ├── HealthIcon (SpriteRenderer - icono de corazón)
        ├── DiceIcon (SpriteRenderer - icono de dado)
        └── AttackIcon (SpriteRenderer - icono de ataque)
```

#### Componentes del GameObject Raíz (MonsterCardPrefab):
- **BoxCollider2D**: Is Trigger = ✅ True (para detectar clicks)
- **CardUI**: Script para mostrar los datos de la carta

#### Configuración de Sprites (Orden de Renderizado):
```
MonsterCard (SpriteRenderer)
├─ Sorting Layer: Cards
├─ Order in Layer: 0
└─ Sprite: (imagen principal del monstruo)

HealthIcon (SpriteRenderer)
├─ Sorting Layer: Cards
├─ Order in Layer: 1
└─ Sprite: (icono de corazón)

DiceIcon (SpriteRenderer)
├─ Sorting Layer: Cards
├─ Order in Layer: 1
└─ Sprite: (icono de dado)

AttackIcon (SpriteRenderer)
├─ Sorting Layer: Cards
├─ Order in Layer: 1
└─ Sprite: (icono de espada/puño)
```

#### Configuración de Textos (TextMeshPro 3D):
**IMPORTANTE**: Usar **3D Object → Text - TextMeshPro**, NO UI

```
HealthText (TextMeshPro 3D)
├─ Text: "5" (placeholder)
├─ Font Size: 5-10 (ajustar según escala)
├─ Color: Rojo (#FF0000)
├─ Alignment: Centro
├─ Extra Settings
│   ├─ Sorting Layer: Cards
│   └─ Order in Layer: 2 (encima de iconos)
└─ Transform Scale: (0.1, 0.1, 1)

DiceText (TextMeshPro 3D)
├─ Text: "3+" (placeholder)
├─ Font Size: 5-10
├─ Color: Blanco (#FFFFFF)
├─ Alignment: Centro
├─ Extra Settings
│   ├─ Sorting Layer: Cards
│   └─ Order in Layer: 2
└─ Transform Scale: (0.1, 0.1, 1)

AttackText (TextMeshPro 3D)
├─ Text: "2" (placeholder)
├─ Font Size: 5-10
├─ Color: Naranja (#FFA500)
├─ Alignment: Centro
├─ Extra Settings
│   ├─ Sorting Layer: Cards
│   └─ Order in Layer: 2
└─ Transform Scale: (0.1, 0.1, 1)
```

**Nota sobre Order in Layer:**
- Layer 0: MonsterCard (fondo)
- Layer 1: Iconos (HealthIcon, DiceIcon, AttackIcon)
- Layer 2: Textos (HealthText, DiceText, AttackText) ← ENCIMA

#### Posiciones Sugeridas (ejemplo):
```
MonsterCardPrefab: (0, 0, 0)
├─ MonsterCard: (0, 0, 0)
└─ StatsContainer: (0, -1.5, 0) ← parte inferior de la carta
    ├─ Textos: (0, 0, 0)
    │   ├─ HealthText: (-0.8, 0, -0.1)
    │   ├─ DiceText: (0, 0, -0.1)
    │   └─ AttackText: (0.8, 0, -0.1)
    └─ Iconos: (0, 0.3, 0) ← ligeramente arriba de los textos
        ├─ HealthIcon: (-0.8, 0, 0)
        ├─ DiceIcon: (0, 0, 0)
        └─ AttackIcon: (0.8, 0, 0)
```

### Paso 3: Crear Prefab MonsterSlot

**IMPORTANTE**: Crear un prefab del MonsterSlot permite instanciar slots dinámicamente cuando cartas/efectos lo requieran.

#### Estructura del Prefab MonsterSlot:
```
MonsterSlot (GameObject raíz)
├── SlotBackground (SpriteRenderer - fondo del slot)
├── CardAnchor (GameObject vacío - donde aparece la carta)
└── SlotUI (GameObject vacío)
    ├─ HealthText (TextMeshPro 3D - opcional en el slot)
    ├─ DiceText (TextMeshPro 3D - opcional en el slot)
    └─ AttackText (TextMeshPro 3D - opcional en el slot)
```

#### Componentes del MonsterSlot (raíz):

1. **WorldTargetable**:
   - Target Type: **Monster**
   - Player Index: **-1**
   - Monster Card UI: (se asigna automáticamente)

2. **MonsterSlot** (script):
   - Card Anchor: Referencia al GameObject CardAnchor
   - Health Text: Referencia al HealthText (opcional)
   - Dice Text: Referencia al DiceText (opcional)
   - Attack Text: Referencia al AttackText (opcional)

3. **SpriteRenderer** (SlotBackground):
   - Sprite: Imagen de fondo del slot (ej: marco vacío)
   - Sorting Layer: Board o Cards
   - Order in Layer: -1 (debajo de todo)

#### Posicionamiento de Slots:

Los slots se posicionarán automáticamente en el `slotsContainer`. Puedes:

**Opción A**: Usar un Layout Group (Grid Layout)
- Añade `GridLayoutGroup` al contenedor
- Cell Size: Tamaño de cada slot
- Spacing: Espacio entre slots
- Start Axis: Horizontal

**Opción B**: Posicionamiento manual
- El MonsterSlotManager instancia slots
- Tú posicionas manualmente en la escena

#### Guardar Prefab:

1. Arrastra el GameObject **MonsterSlot** a `Assets/Prefabs/`
2. Nombrar: **"MonsterSlot.prefab"**
3. Eliminar de la Hierarchy (ya está guardado)

### Paso 4: Configurar MonsterSlotManager

1. Seleccionar el GameObject **MonsterSlotManager** en la escena
2. Configurar en el Inspector:

```
MonsterSlotManager (Script)
├─ Monster Slots: [] (vacío - se crean dinámicamente)
├─ Monster Slot Prefab: MonsterSlot.prefab ← ARRASTAR AQUÍ
├─ Slots Container: (GameObject vacío donde aparecen los slots)
├─ Initial Slot Count: 2 (slots al inicio)
├─ Max Slot Count: 4 (máximo permitido)
└─ Monster Card Prefab: MonsterCardPrefab.prefab ← ARRASTAR AQUÍ
```

3. **Crear Slots Container**:
   - Click derecho en Hierarchy → Create Empty
   - Nombrar: **"MonsterSlotsContainer"**
   - Posicionar en el tablero donde quieres los slots
   - (Opcional) Añadir `GridLayoutGroup` para auto-organizar

4. **Asignar Slots Container** al MonsterSlotManager

### Paso 5: Player Stats Panels
Para cada slot de monstruo (ej: 2 slots):

1. Crear GameObject en el tablero (world space, fuera del Canvas)
2. Añadir `SpriteRenderer` con sprite de fondo del slot
3. Añadir componente `WorldTargetable`:
   - Target Type: Monster
   - Player Index: -1
   - Monster Card UI: (dejar vacío, se asigna dinámicamente)
4. Añadir componente `MonsterSlot`:
   - Card Anchor: (crear child GameObject vacío como punto de anclaje)
   - Health Text: (opcional, TMP_Text para mostrar vida)
   - Attack Text: (opcional, TMP_Text para mostrar ataque)
5. Posicionar el slot en el tablero
6. Añadir el MonsterSlot a la lista `monsterSlots` del MonsterSlotManager

### Paso 3: Player Stats Panels
Para cada panel de jugador (3 paneles):

1. Seleccionar el panel UI del jugador
2. Añadir componente `PlayerStatsPanel`:
   - Player Index: 1, 2, o 3
   - Asignar todas las referencias UI (playerNameText, healthText, etc.)
   - Background Image: asignar el componente Image de fondo
   - Active Turn Color: color cuando es el turno del jugador (ej: amarillo)
   - Inactive Turn Color: color normal (ej: gris/blanco)
3. Añadir componente `Targetable` (para poder ser objetivo de cartas):
   - Target Type: Player
   - Player Index: (mismo que PlayerStatsPanel)

### Paso 4: Crear Cartas de Monstruo
Crear ScriptableObjects de tipo CardDataSO para monstruos:

1. Click derecho en Assets/Cards → Create → Card Data
2. Configurar (stats estandarizados para monstruos):
    - Card Name: nombre del monstruo (ej: "Gaper")
    - Card Type: Monster o Boss
    - Front/Back Sprite
    - Health: vida del monstruo (ej: 2)
    - Dice Requirement: mínimo para dañarlo (ej: 3 = 3+)
    - Attack Damage: daño que inflige el monstruo (ej: 1)
    - Recompensas al derrotar (opcional, si no se rellenan se usan defaults):
       - Reward Coins (Monster: 5, Boss: 15 por defecto)
       - Reward Loot Cards (Monster: 2 por defecto)
       - Reward Treasure (Boss: 1 por defecto)
       - Reward Souls Min/Max (Boss: 1-2 por defecto)
    - Combat Trigger (opcional):
       - Has Combat Trigger: On
       - Trigger Roll Value: valor exacto (ej: 5)
       - Attacker Damage: daño al atacante cuando ocurre (ej: 1)
3. (Opcional) Añadir efectos al monstruo si tiene habilidades especiales adicionales

### Paso 5: Crear Mazo de Monstruos
1. Click derecho en Assets/Cards → Create → Deck Configuration
2. Nombrar: "MonsterDeck"
3. Añadir entradas con los CardDataSO de monstruos y sus cantidades:
   - Ej: Gaper x5, Mulligan x3, Larry Jr x2
4. En GameManager, asignar este DeckConfiguration a `monsterDeckConfig`

## Flujo de Juego

### Inicio del Juego
1. GameManager llama a `LoadDeckFromConfiguration` para el mazo Monster
2. MonsterSlotManager crea `initialSlotCount` slots (default: 2) dinámicamente
3. `FillInitialSlots()` se ejecuta 0.5s después del Start
4. Cada slot vacío recibe un monstruo mediante `DrawMonsterToSlot()`
5. Las cartas visuales de monstruos se instancian en los `cardAnchor` de cada slot

### Añadir Slots Dinámicamente
1. Una carta con `AddMonsterSlotEffect` se juega
2. MonsterSlotManager verifica si `slotCount < maxSlotCount` (default max: 4)
3. Si hay espacio, instancia un nuevo MonsterSlot desde el prefab
4. El nuevo slot se añade a `slotsContainer`
5. Automáticamente se llena con un monstruo del mazo
6. Los jugadores ahora pueden enfrentar más monstruos simultáneamente

### Targeting de Cartas
1. Jugador selecciona una carta con daño (ej: "The Bomb")
2. Presiona botón "Use"
3. TargetingManager resalta todos los objetivos válidos (monstruos y jugadores)
4. Jugador hace clic en un monstruo
5. `DealDamageEffect` aplica daño mediante `MonsterSlotManager.DamageMonster()`
6. Si la vida del monstruo llega a ≤0, se derrota automáticamente

### Derrota de Monstruo
1. `MonsterSlotManager.DefeatMonster()` es llamado
2. Se otorgan recompensas al jugador (defaults si no se configuran):
   - Monstruo común: 5 monedas, roba 2 cartas de Loot, tira 1d6 y gana +X monedas
   - Boss: 15 monedas, +1 Tesoro, +1 a 2 Almas
3. Se limpia el slot con `ClearMonster()`
4. Se rellena inmediatamente con `DrawMonsterToSlot()`
5. El nuevo monstruo aparece en el slot

## Ejemplo de Carta de Monstruo

**Gaper**
- Tipo: Monster
- Health: 2
- Dice Requirement: 3+
- Attack Damage: 1
- Recompensas (defaults): 5¢, roba 2 Loot, +1d6 ¢
- Descripción: "Un simple Gaper. Fácil de derrotar."

**Monstro (Boss)**
- Tipo: Boss
- Health: 6
- Dice Requirement: 4+
- Attack Damage: 2
- Recompensas (defaults): 15¢, +1 Tesoro, +1-2 Almas
- Trigger de combate (opcional): si el atacante saca 5, recibe 1 de daño

## Testing

### Probar Sistema de Monstruos
1. Jugar → verificar que 2 monstruos aparecen en los slots
2. Seleccionar "The Bomb" → presionar "Use"
3. Verificar que los monstruos se resaltan (highlight)
4. Clic en un monstruo → verificar que recibe 2 de daño
5. Si el monstruo llega a 0 vida → verificar que desaparece y aparece uno nuevo
6. Verificar que el jugador recibe las monedas del monstruo derrotado

### Probar Targeting de Jugadores
1. Seleccionar "The Bomb" → presionar "Use"
2. Verificar que los paneles de jugadores se resaltan
3. Clic en un panel de jugador → verificar que pierde 2 de vida
4. Verificar que el texto de vida se actualiza en el panel

## Integración con Sistema de Efectos

Los efectos de cartas pueden interactuar con monstruos:

### DealDamageEffect (Dañar Monstruos)
```csharp
// DealDamageEffect detecta automáticamente el tipo de objetivo
public override TargetType[] AllowedTargets => new[] { TargetType.Player, TargetType.Monster };

// Al ejecutar, el efecto verifica el tipo y aplica daño:
// - Si es Player: GameManager.ChangePlayerHealth()
// - Si es Monster: MonsterSlotManager.DamageMonster()
```

### AddMonsterSlotEffect (Añadir Slots)
```csharp
// Efecto para cartas que expanden los espacios de monstruos
// Ejemplo: "Curse of the Tower" añade 1 slot de monstruo
// Uso: Crear CardDataSO → Añadir AddMonsterSlotEffect con slotsToAdd = 1

public class AddMonsterSlotEffect : CardEffect
{
    [SerializeField] private int slotsToAdd = 1;
    
    // No requiere objetivo, se ejecuta directamente
    // Llama a MonsterSlotManager.AddMonsterSlot()
    // Verifica límite máximo antes de añadir
}
```

### Ejemplos de Cartas con Efectos de Slots

**"Curse of the Tower"**
- Tipo: Loot
- Efecto: AddMonsterSlotEffect (slotsToAdd: 1)
- Descripción: "Añade un espacio de monstruo adicional"

**"The Harbingers"**
### Trigger de Combate (monstruos)

Puedes configurar en el CardDataSO del monstruo:

- Has Combat Trigger: On/Off
- Trigger Roll Value: valor exacto (p.e. 5)
- Attacker Damage: daño que recibe el atacante cuando ocurre

Hook disponible para procesarlo durante el combate:

```csharp
// Llama esto cuando el jugador tira el dado para atacar a un monstruo en un slot
MonsterSlotManager.Instance.ProcessCombatRoll(slot, attackerPlayer, diceValue);
```
- Tipo: Event
- Efecto: AddMonsterSlotEffect (slotsToAdd: 2)
- Descripción: "¡Aparecen 2 espacios de monstruos más!"

## Notas de Implementación

- Los slots se llenan automáticamente 0.5s después del Start para asegurar que GameManager esté inicializado
- WorldTargetable en cada slot permite targeting visual con highlight (color + escala)
- PlayerStatsPanel usa Invoke(0.5s) para inicialización retrasada
- El sistema soporta cualquier número de slots (actualmente 2)
- Las recompensas de monstruos (monedas/almas) están definidas en el CardDataSO del monstruo
- El mazo de monstruos se baraja al inicio y se roba desde arriba

## Próximos Pasos

1. Implementar sistema de combate automático (monstruos atacan al jugador al final de turno)
2. Añadir efectos especiales de monstruos
3. Implementar recompensas de tesoros por derrotar monstruos
4. Añadir animaciones de derrota/spawn de monstruos
5. Sistema de "souls" detallado (algunos monstruos otorgan almas, otros no)

---

## 🎯 Guía Rápida: Crear Sistema de Slots Dinámicos

### ✅ Prefabs Necesarios:

1. **MonsterCardPrefab.prefab**:
   ```
   MonsterCardPrefab (raíz + BoxCollider2D + CardUI)
   ├─ MonsterCard (SpriteRenderer)
   └─ StatsContainer
       ├─ Textos (HealthText, DiceText, AttackText - TMP 3D)
       └─ Iconos (HealthIcon, DiceIcon, AttackIcon - Sprites)
   ```

2. **MonsterSlot.prefab**:
   ```
   MonsterSlot (raíz + WorldTargetable + MonsterSlot script)
   ├─ SlotBackground (SpriteRenderer)
   ├─ CardAnchor (Empty - donde aparece la carta)
   └─ SlotUI (opcional - textos propios del slot)
   ```

### ✅ Configuración en Escena:

1. **Crear MonsterSlotManager**:
   - GameObject vacío: "MonsterSlotManager"
   - Añadir script `MonsterSlotManager`

2. **Crear SlotsContainer**:
   - GameObject vacío: "MonsterSlotsContainer"
   - Posicionar en el tablero
   - (Opcional) Añadir `GridLayoutGroup`

3. **Configurar MonsterSlotManager**:
   ```
   Monster Slot Prefab: MonsterSlot.prefab
   Slots Container: MonsterSlotsContainer
   Initial Slot Count: 2
   Max Slot Count: 4
   Monster Card Prefab: MonsterCardPrefab.prefab
   ```

### ✅ Crear Cartas que Añaden Slots:

1. Click derecho → Create → Card Data
2. Nombrar: "Curse of the Tower"
3. Card Type: Loot
4. Click derecho → Create → Card Effects → Add Monster Slot
5. Configurar: Slots To Add = 1
6. Asignar efecto a la carta

### ✅ Funciones del Sistema:

```csharp
// Añadir slot (devuelve true si tuvo éxito)
MonsterSlotManager.Instance.AddMonsterSlot();

// Remover slot específico
MonsterSlotManager.Instance.RemoveMonsterSlot(slot);

// Verificar si se pueden añadir más
bool canAdd = MonsterSlotManager.Instance.CanAddMoreSlots();

// Obtener número actual de slots
int count = MonsterSlotManager.Instance.GetSlotCount();
```

### ✅ Flujo Automático:

1. **Inicio**: Se crean 2 slots automáticamente
2. **Llenado**: Cada slot roba un monstruo del mazo
3. **Expansión**: Cartas/efectos añaden slots hasta max 4
4. **Derrota**: Monstruo muerto → slot se rellena automáticamente
5. **Targeting**: Todos los slots son targeteables con WorldTargetable
