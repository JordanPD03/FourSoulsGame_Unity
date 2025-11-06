# 📊 Configuración de PlayerStatsUI - Stats del Jugador

## 🎯 Descripción

Sistema de UI que muestra las estadísticas del jugador en tiempo real:
- ❤️ **Vida (Health)** - Formato `2/2` (vida actual / vida máxima)
- 💰 **Monedas (Coins)** - Formato `3` (cantidad actual)
- 👻 **Almas (Souls)** - Formato `0/4` (almas actuales / almas para ganar)

Se actualiza automáticamente suscribiéndose a eventos del GameManager.

---

## 🛠️ Configuración en Unity (PASO A PASO)

### **Paso 1: Crear la estructura de UI**

1. **En el Canvas**, crea la siguiente jerarquía:

```
Canvas
├── PlayerStatsPanel (Panel vacío)
│   ├── HealthGroup (GameObject vacío)
│   │   ├── HealthIcon (Image)
│   │   └── HealthText (TextMeshProUGUI)
│   ├── CoinsGroup (GameObject vacío)
│   │   ├── CoinIcon (Image)
│   │   └── CoinsText (TextMeshProUGUI)
│   └── SoulsGroup (GameObject vacío)
│       ├── SoulIcon (Image)
│       └── SoulsText (TextMeshProUGUI)
```

### **Paso 2: Configurar cada grupo**

#### **HealthGroup (Vida)**

1. **HealthIcon**:
   - Component: `Image`
   - Source Image: Arrastra tu sprite de corazón
   - Raycast Target: Desactivado
   - Tamaño recomendado: 64x64

2. **HealthText**:
   - Component: `TextMeshProUGUI`
   - Text: "2/2" (valor inicial con formato actual/máxima)
   - Font Size: 36-48
   - Alignment: Center
   - Color: Rojo (#FF0000) o blanco

#### **CoinsGroup (Monedas)**

1. **CoinIcon**:
   - Component: `Image`
   - Source Image: Arrastra tu sprite de moneda
   - Raycast Target: Desactivado
   - Tamaño recomendado: 64x64

2. **CoinsText**:
   - Component: `TextMeshProUGUI`
   - Text: "3" (valor inicial)
   - Font Size: 36-48
   - Alignment: Center
   - Color: Dorado (#FFD700) o blanco

#### **SoulsGroup (Almas)**

1. **SoulIcon**:
   - Component: `Image`
   - Source Image: Arrastra tu sprite de alma
   - Raycast Target: Desactivado
   - Tamaño recomendado: 64x64

2. **SoulsText**:
   - Component: `TextMeshProUGUI`
   - Text: "0/4" (valor inicial con formato actual/objetivo)
   - Font Size: 36-48
   - Alignment: Center
   - Color: Morado (#9C27B0) o blanco

---

### **Paso 3: Configurar Layout (Opcional)**

Para que se vean organizados horizontalmente:

1. Selecciona **PlayerStatsPanel**
2. Add Component → `Horizontal Layout Group`
   - Spacing: 20-30
   - Child Alignment: Middle Left
   - Child Force Expand: Width y Height desactivados

3. Para cada grupo (HealthGroup, CoinsGroup, SoulsGroup):
   - Add Component → `Horizontal Layout Group`
   - Spacing: 5-10
   - Child Alignment: Middle Center

---

### **Paso 4: Adjuntar el Script**

1. Selecciona **PlayerStatsPanel**
2. Add Component → Buscar **"Player Stats UI"**
3. El script `PlayerStatsUI.cs` se adjuntará

4. **Configurar referencias en el Inspector:**
   - **Health Icon**: Arrastra `HealthIcon` (Image)
   - **Coins Icon**: Arrastra `CoinIcon` (Image)
   - **Souls Icon**: Arrastra `SoulIcon` (Image)
   - **Health Text**: Arrastra `HealthText` (TextMeshProUGUI)
   - **Coins Text**: Arrastra `CoinsText` (TextMeshProUGUI)
   - **Souls Text**: Arrastra `SoulsText` (TextMeshProUGUI)
   - **Player Index**: 0 (para Player 1)
   - **Animate On Change**: ✅ (opcional, para animación)

---

## 🎨 Posicionamiento Recomendado

### **Opción A: Esquina Superior Izquierda**
```
Posición de PlayerStatsPanel:
- Anchor: Top-Left
- Position X: 20
- Position Y: -20
- Pivot: 0, 1
```

### **Opción B: Parte Superior Centro**
```
Posición de PlayerStatsPanel:
- Anchor: Top-Center
- Position X: 0
- Position Y: -20
- Pivot: 0.5, 1
```

---

## 📦 Sprites Necesarios

Necesitas 3 sprites (formato PNG recomendado):

1. **Heart/Corazón** (❤️) - Para la vida
2. **Coin/Moneda** (💰) - Para las monedas
3. **Soul/Alma** (👻) - Para las almas

**Dónde colocarlos:**
- `Assets/UI/Icons/` (crea esta carpeta)

**Configuración en Inspector del sprite:**
- Texture Type: **Sprite (2D and UI)**
- Max Size: 256 o 512
- Format: RGBA Compressed

---

## 🎮 Uso en el Juego

### **Actualización Automática**
El script se actualiza automáticamente cuando:
- ✅ El jugador recibe daño
- ✅ El jugador gana/pierde monedas
- ✅ El jugador recolecta almas

### **Actualización Manual (desde código)**

```csharp
// Obtener referencia al PlayerStatsUI
PlayerStatsUI statsUI = FindObjectOfType<PlayerStatsUI>();

// Modificar stats manualmente
statsUI.AddHealth(1);      // Suma 1 vida
statsUI.AddHealth(-1);     // Resta 1 vida
statsUI.AddCoins(5);       // Suma 5 monedas
statsUI.AddCoins(-2);      // Resta 2 monedas

// Agregar alma (requiere un CardData de tipo Soul)
CardData soul = new CardData(99, "Soul", CardType.Soul, "");
statsUI.AddSoul(soul);
```

---

## 🎬 Animación

Si `Animate On Change` está activado:
- Los iconos harán un efecto "punch" (escala) cuando cambien valores
- Duración: 0.3 segundos
- Escala máxima: 1.2x

**Requisito:** DOTween (opcional, tiene fallback sin DOTween)

---

## 🔗 Eventos del GameManager

El script se suscribe a estos eventos:

```csharp
GameManager.Instance.OnPlayerDamaged += HandlePlayerDamaged;
GameManager.Instance.OnCardDrawn += HandleCardDrawn;
```

**Para agregar más eventos:**
Edita `PlayerStatsUI.cs` en los métodos `Start()` y `OnDestroy()`

---

## 🐛 Troubleshooting

### **Problema: Los valores no se actualizan**
- ✅ Verifica que el GameManager existe en la escena
- ✅ Verifica que `Player Index` coincide con el índice del jugador
- ✅ Verifica que todas las referencias (Icons y Texts) están asignadas

### **Problema: TextMeshProUGUI no aparece**
- ✅ Importa el paquete **TextMeshPro** desde Package Manager
- ✅ O usa `Text` normal en lugar de `TextMeshProUGUI`
- ✅ Cambia en el script: `using TMPro;` → `using UnityEngine.UI;`
- ✅ Cambia: `TextMeshProUGUI` → `Text`

### **Problema: "NullReferenceException"**
- ✅ Asegúrate de asignar TODAS las referencias en el Inspector
- ✅ Verifica que los GameObjects no estén desactivados

### **Problema: Las almas siempre muestran 0**
- Esto es normal al inicio, las almas se obtienen ganando combates
- Las almas se cuentan desde `player.activeItems` donde `cardType == CardType.Soul`

---

## 📊 Ejemplo de Layout Final

```
┌─────────────────────────────────────┐
│  ❤️ 2/2    💰 3    👻 0/4          │  ← PlayerStatsPanel
├─────────────────────────────────────┤
│                                     │
│        [Cartas en la mano]          │
│                                     │
└─────────────────────────────────────┘
```

---

## 🚀 Próximos Pasos

Una vez configurado el PlayerStatsUI:

1. **Crear sistema de daño** - Implementar combate con monstruos
2. **Crear sistema de compras** - Gastar monedas en tesoros
3. **Crear sistema de almas** - Recolectar almas al derrotar bosses
4. **Condición de victoria** - Ganar con 4 almas
5. **Multiplayer** - Mostrar stats de múltiples jugadores

---

¡Listo! 🎉 Ahora tienes un sistema de stats visual que se actualiza automáticamente.
