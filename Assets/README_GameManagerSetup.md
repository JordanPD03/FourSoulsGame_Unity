# 🎮 Configuración del GameManager y Sistema de Turnos

## ✅ Cambios Realizados

### 1. **Nueva Arquitectura Modular**
Se creó un sistema centralizado preparado para multiplayer:

- **`Core/PlayerData.cs`**: Datos del jugador (vida, monedas, mano, etc.)
- **`Core/CardData.cs`**: Datos de cartas (separados de la vista)
- **`Core/GamePhase.cs`**: Fases del turno (StartTurn → Draw → Action → Combat → End)
- **`Core/DeckType.cs`**: Tipos de mazos (Loot, Treasure, Monster)
- **`Core/GameManager.cs`**: Controlador centralizado con eventos **y animaciones de robo**

### 2. **Refactorización de Scripts Existentes**

#### `CardUI.cs`
- ✅ Agregado método `SetCardData(CardData, Sprite, Sprite)` con actualización visual forzada
- ✅ Método `GetCardData()` para obtener los datos
- ✅ Debug logs para sprites que no cargan
- ✅ Compatibilidad con el método antiguo `SetCard()` (no rompe nada existente)

#### `DrawCardController.cs`
- ❌ **ELIMINADO** - Toda la lógica se movió al GameManager
- ✅ Ahora el GameManager maneja Input, validación y animación

#### `PlayerHandUI.cs`
- ✅ Suscrito al evento `OnCardDrawn` del GameManager
- ✅ Recibe notificaciones cuando se roban cartas

---

## 🔧 Configuración en Unity (PASO A PASO)

### **Paso 1: Crear el GameManager en la Escena**

1. **Clic derecho en la Jerarquía** → `Create Empty`
2. Renombrar a **"GameManager"**
3. **IMPORTANTE**: Debe estar **AL NIVEL RAÍZ** (NO dentro del Canvas)

```
Hierarchy:
├── GameManager          ← AQUÍ (GameObject vacío)
├── Main Camera
├── Directional Light
├── Canvas
│   ├── PlayerHandUI
│   ├── CardPreview
│   └── (DrawCardController ya NO existe)
```

4. Seleccionar "GameManager" → Inspector → `Add Component` → Buscar **"Game Manager"**
5. El script `GameManager.cs` se adjuntará automáticamente

6. **CONFIGURAR REFERENCIAS EN EL INSPECTOR:**
   - **Player Hand UI**: Arrastra el GameObject `PlayerHandUI` desde la Jerarquía
   - **Card Prefab**: Arrastra tu prefab de carta desde `Assets/Prefabs/`
   - **Card Back Sprite**: Arrastra el sprite del dorso de carta
   - **Animation Layer**: Arrastra el `Canvas` (para la capa de animación)

---

### **Paso 2: Configurar Sprites de Prueba**

⚠️ **IMPORTANTE**: Los sprites **DEBEN** estar dentro de una carpeta llamada `Resources` para que `Resources.Load()` funcione.

✅ **Ya están configurados automáticamente** en:
```
Assets/
└── Resources/              ← OBLIGATORIO para Resources.Load()
    └── Cards/
        └── Front/
            └── Loot/
                ├── card0.png
                ├── card1.png
                ├── card2.png
                └── card3.png
```

El GameManager crea 20 cartas de prueba que rotan entre estos 4 sprites.

**Si necesitas cambiar la ubicación:**
Edita `CreateTestCards()` en GameManager.cs y ajusta el path:
```csharp
spritePath: $"Cards/Front/Loot/card{i % 4}"
//           ^^^ SIN "Resources/" y SIN extensión .png
```

---

### **Paso 3: Eliminar GameObject antiguo (si existe)**

Si tenías un GameObject "DrawCardController" en la escena:
1. Selecciónalo en la Jerarquía
2. Presiona `Delete`
3. El script ya fue eliminado, toda la lógica está en GameManager

---

### **Paso 4: Verificar que todo esté conectado**

Asegúrate de que en el **GameManager Inspector**:
- ✅ `Player Hand UI` tiene referencia a PlayerHandUI
- ✅ `Card Prefab` tiene referencia al prefab de carta
- ✅ `Card Back Sprite` tiene el sprite del dorso
- ✅ `Animation Layer` tiene referencia al Canvas
- ✅ El prefab de carta tiene el componente `CardUI.cs` adjunto

---

## 🎮 Cómo Usar el Sistema

### **Robar Cartas**
1. Presiona **Espacio** en Play Mode
2. El GameManager validará:
   - ✅ ¿Es la fase de Draw?
   - ✅ ¿Hay cartas en el mazo?
   - ✅ ¿Es el turno del jugador correcto?
3. Si es válido, roba la carta y cambia a fase de Acción

### **Validación de Acciones**
El GameManager controla qué puedes hacer en cada fase:

```csharp
GamePhase.Draw      → Solo robar cartas
GamePhase.Action    → Jugar cartas, usar objetos
GamePhase.Combat    → Atacar monstruos
GamePhase.End       → Terminar turno
```

### **Eventos del GameManager**
Puedes suscribirte desde cualquier script UI:

```csharp
void Start()
{
    GameManager.Instance.OnCardDrawn += (player, card) => {
        Debug.Log($"{player.playerName} robó {card.cardName}");
    };
    
    GameManager.Instance.OnPhaseChanged += (phase) => {
        Debug.Log($"Fase cambiada a: {phase}");
    };
}
```

---

## 🔍 Debugging

### **Problema: "GameManager no encontrado"**
- ✅ Verifica que el GameObject "GameManager" existe en la escena
- ✅ Verifica que tiene el script `GameManager.cs` adjunto
- ✅ Verifica que está **fuera del Canvas**

### **Problema: "No se cargan los sprites"**
- ✅ Verifica que la carpeta sea `Assets/Resources/Cards/Front/`
- ✅ Los sprites deben estar en una carpeta llamada **"Resources"**
- ✅ Verifica los nombres: `card0.png`, `card1.png`, etc.
- ✅ En la consola verás warnings si un sprite no carga: `[CardUI] No se pudo cargar sprite: Cards/Front/cardX`
- ✅ Verifica que los sprites tengan **Texture Type: Sprite (2D and UI)** en el Inspector

### **Problema: "No puedes robar cartas durante la fase X"**
- ✅ Esto es normal, el GameManager está validando las fases
- ✅ Solo puedes robar en fase `Draw`
- ✅ Después de robar, cambia automáticamente a fase `Action`

---

## 📊 Logs Útiles

Si todo está bien configurado, verás en la consola:

```
[GameManager] Created 20 test cards
[GameManager] Phase changed to: StartTurn
[GameManager] Processing start turn effects for Player 1
[GameManager] Phase changed to: Draw
[GameManager] Player 1 drew: Test Card 1
[GameManager] Phase changed to: Action
[PlayerHandUI] Player 1 drew Test Card 1
```

---

## 🚀 Próximos Pasos

Una vez configurado:

1. **Crear ScriptableObjects** para cartas reales (reemplazar cartas de prueba)
2. **Implementar botones de UI** para cambiar de fase
3. **Sistema de combate** (atacar monstruos)
4. **Sistema de tesoros** (objetos activos/pasivos)
5. **Multiplayer con Netcode** (cuando la lógica core esté completa)

---

## 🎯 Preguntas Frecuentes

**P: ¿Puedo seguir usando el sistema antiguo de sprites?**  
R: Sí, el método `SetCard(sprite, faceUp)` todavía funciona por compatibilidad.

**P: ¿Necesito otro prefab para las cartas?**  
R: NO. El mismo prefab `CardUI` sirve para todas las cartas. Solo cambian los datos (`CardData`).

**P: ¿Dónde va el GameManager en la jerarquía?**  
R: **AL NIVEL RAÍZ**, fuera del Canvas. No es un elemento de UI, es el controlador del juego.

**P: ¿Cómo agrego más fases personalizadas?**  
R: Edita el enum `GamePhase.cs` y agrega tu nueva fase.

---

¡Listo! 🎉 El sistema modular de turnos está configurado y preparado para multiplayer.
