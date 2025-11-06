# Configuración de Targeting para Tablero 3D/2D (Fuera del Canvas)

## 🎯 Sistema de Objetivos para Objetos del Mundo

Este sistema funciona con **SpriteRenderer** y **Collider2D**, perfecto para tableros que están fuera del Canvas.

---

## 📋 Paso 1: Configurar TargetingManager (2 min)

### 1.1 Crear el GameObject

1. En la **Hierarchy** (puede estar fuera o dentro del Canvas)
2. Click derecho → `Create Empty`
3. Nómbralo: **"TargetingManager"**
4. Agrégale el componente: **`Targeting Manager`**

### 1.2 Configurar en Inspector

```
┌─────────────────────────────────────┐
│ TARGETING MANAGER                   │
├─────────────────────────────────────┤
│ Allow Cancel: ✅ TRUE               │
│  (Permite cancelar con ESC o        │
│   click derecho)                    │
└─────────────────────────────────────┘
```

---

## 📋 Paso 2: Configurar Mazos de Descarte (5 min)

### 2.1 Descarte de Loot

1. En la **Hierarchy**, selecciona tu **prefab/objeto de descarte de Loot**
2. Asegúrate de que tenga:
   - ✅ **SpriteRenderer** (ya lo tiene)
   - ✅ **Collider2D** (BoxCollider2D o similar)
3. Agrégale el componente: **`World Targetable`**
4. Configura en el **Inspector**:

```
┌─────────────────────────────────────┐
│ WORLD TARGETABLE                    │
├─────────────────────────────────────┤
│ TARGET CONFIGURATION                │
│  Target Type: Discard Pile          │
│  Player Index: -1                   │
│  Monster Card UI: None              │
│                                     │
│ VISUAL FEEDBACK                     │
│  Normal Color: Blanco (#FFFFFF)     │
│  Highlight Color: Amarillo (#FFFF00)│
│  Highlight Scale: 1.1               │
│  Highlight Duration: 0.2            │
└─────────────────────────────────────┘
```

### 2.2 Otros Descartes (Repetir para cada uno)

Repite el proceso para:

- **Descarte de Tesoros**
  - Target Type: `Discard Pile`
  - Highlight Color: Cyan (#00FFFF)

- **Descarte de Monstruos**
  - Target Type: `Discard Pile`
  - Highlight Color: Rojo (#FF0000)

- **Descarte de Habitaciones**
  - Target Type: `Discard Pile`
  - Highlight Color: Verde (#00FF00)

---

## 📋 Paso 3: Configurar Mazos de Robo (Opcional)

Si quieres que los mazos de robo también sean targetables (por ejemplo, para efectos como "Roba una carta del mazo de tesoros"):

1. Selecciona cada **mazo de robo**
2. Agrégale **`World Targetable`**
3. Configura:
   - Target Type: `Custom` (o crea un nuevo tipo `DrawPile` si lo necesitas)

**Nota:** Por ahora, si solo necesitas los descartes, puedes saltar este paso.

---

## 📋 Paso 4: Configurar Slots de Monstruos (Si los tienes)

Si ya tienes slots donde aparecen los monstruos activos:

1. Selecciona cada **slot de monstruo** en el tablero
2. Agrégale **`World Targetable`**
3. Configura:

```
┌─────────────────────────────────────┐
│ WORLD TARGETABLE                    │
├─────────────────────────────────────┤
│ Target Type: Monster                │
│ Player Index: -1                    │
│ Monster Card UI: (asignar si hay    │
│                   CardUI del        │
│                   monstruo)         │
│ Highlight Color: Naranja (#FFA500)  │
└─────────────────────────────────────┘
```

---

## 📋 Paso 5: Configurar Áreas de Jugadores (Si están en el tablero)

Si tienes zonas/avatares de jugadores en el tablero (fuera del Canvas):

1. Selecciona cada **zona de jugador**
2. Agrégale **`World Targetable`**
3. Configura:

```
┌─────────────────────────────────────┐
│ WORLD TARGETABLE                    │
├─────────────────────────────────────┤
│ Target Type: Player                 │
│ Player Index: 0 (o 1, 2, 3...)      │
│ Highlight Color: Rojo (#FF0000)     │
│ Highlight Scale: 1.15               │
└─────────────────────────────────────┘
```

**Si los jugadores están en UI (Canvas):** Usa el componente `Targetable` normal en lugar de `WorldTargetable`.

---

## 🎮 Cómo Funciona

### Flujo de Targeting

1. **Jugador selecciona carta** (ej: "The Bomb")
2. **Presiona botón "Usar"** en el preview
3. **Sistema detecta que requiere objetivo**
4. **Mazos/objetos válidos se iluminan**:
   - Cambian de color
   - Aumentan ligeramente de tamaño (1.1x)
5. **Jugador hace click en el objetivo**
6. **Efecto se ejecuta** (ej: daño al jugador)
7. **Carta se descarta**

### Cancelar Selección

- **ESC**: Cancela el targeting
- **Click derecho**: Cancela el targeting
- Las áreas vuelven a su color/tamaño normal

---

## 🔧 Verificación de Colliders

### ¿Por qué necesito Collider2D?

El componente `WorldTargetable` usa `OnMouseDown()` para detectar clicks. Esto **solo funciona** con objetos que tienen un **Collider2D**.

### Auto-detección

Si tu objeto **no tiene** Collider2D, `WorldTargetable` agregará automáticamente un **BoxCollider2D** al iniciar.

Verás este mensaje en consola:
```
[WorldTargetable] NombreDelObjeto no tenía Collider2D. BoxCollider2D agregado automáticamente.
```

### Ajustar Collider Manualmente

Si quieres ajustar el área clickeable:

1. Selecciona el objeto
2. En el **Inspector**, busca el componente **Box Collider 2D**
3. Ajusta:
   - **Offset**: Posición del collider
   - **Size**: Tamaño del área clickeable

---

## 🎨 Personalización Visual

### Colores de Resaltado por Tipo

Recomendación de colores para facilitar identificación:

| Tipo de Objetivo | Color Sugerido | Hex Code |
|------------------|----------------|----------|
| Discard Pile (Loot) | Amarillo | #FFFF00 |
| Discard Pile (Treasure) | Cyan | #00FFFF |
| Discard Pile (Monster) | Rojo Oscuro | #CC0000 |
| Player | Rojo Brillante | #FF0000 |
| Monster (activo) | Naranja | #FFA500 |

### Escala de Resaltado

- **1.05 - 1.1**: Sutil, profesional
- **1.15 - 1.2**: Más visible, casual
- **1.3+**: Muy obvio (bueno para tutoriales)

---

## 🐛 Solución de Problemas

### "Los objetos no se iluminan"

**Posibles causas:**

1. **No hay TargetingManager en la escena**
   - Verifica que existe y tiene el script asignado

2. **El objeto no tiene Collider2D**
   - Revisa el Inspector → debería tener BoxCollider2D
   - Si no, agrégalo manualmente

3. **El Target Type no está permitido**
   - Verifica que la carta requiere ese tipo
   - Ej: "The Bomb" permite `Player` y `Monster`, no `DiscardPile`

### "Hago click pero no pasa nada"

**Posibles causas:**

1. **El Collider2D es muy pequeño**
   - Aumenta el **Size** del BoxCollider2D en el Inspector

2. **Hay otro objeto encima bloqueando el click**
   - Ajusta el **Sorting Order** del SpriteRenderer
   - O mueve el objeto más al frente (Z position)

3. **La cámara no es la correcta**
   - `OnMouseDown()` usa la **Main Camera**
   - Asegura que tu cámara tenga el tag **"MainCamera"**

### "El color no cambia"

- Verifica que el objeto tiene **SpriteRenderer** activo
- Asegura que el material del SpriteRenderer es **Sprites/Default**
- Revisa que `Normal Color` y `Highlight Color` sean diferentes

---

## 📦 Estructura Final en Unity

```
Scene Hierarchy:
├── Main Camera (tag: MainCamera) ← IMPORTANTE
├── Canvas
│   └── ... (tu UI)
├── TargetingManager (script: TargetingManager)
│
└── Board (tablero fuera del Canvas)
    ├── LootDeck
    ├── LootDiscard ← WorldTargetable (DiscardPile)
    │   ├── SpriteRenderer
    │   └── BoxCollider2D
    ├── TreasureDeck
    ├── TreasureDiscard ← WorldTargetable (DiscardPile)
    ├── MonsterDeck
    ├── MonsterDiscard ← WorldTargetable (DiscardPile)
    ├── RoomDeck
    ├── RoomDiscard ← WorldTargetable (DiscardPile)
    │
    ├── MonsterSlot_1 ← WorldTargetable (Monster)
    ├── MonsterSlot_2 ← WorldTargetable (Monster)
    │
    └── PlayerZones (si están en el tablero)
        ├── Player1Zone ← WorldTargetable (Player, index=0)
        └── Player2Zone ← WorldTargetable (Player, index=1)
```

---

## ✅ Checklist de Configuración

- [ ] TargetingManager creado en la escena
- [ ] Descarte de Loot tiene WorldTargetable (DiscardPile)
- [ ] Descarte de Tesoros tiene WorldTargetable (DiscardPile)
- [ ] Descarte de Monstruos tiene WorldTargetable (DiscardPile)
- [ ] Descarte de Habitaciones tiene WorldTargetable (DiscardPile)
- [ ] Todos los objetos targetables tienen Collider2D
- [ ] Main Camera tiene tag "MainCamera"
- [ ] Slots de monstruos tienen WorldTargetable (Monster) - si aplica
- [ ] Zonas de jugadores tienen WorldTargetable (Player) - si están en el tablero

---

## 🚀 Prueba Rápida

1. **Play** en Unity
2. Selecciona **"The Bomb"** de tu mano
3. Presiona **"Usar"**
4. Los **descarte de Loot** y **jugadores/monstruos** deben **iluminarse en amarillo/rojo/naranja**
5. Haz **click** en uno
6. Debería ejecutarse el efecto y la carta descartarse

---

## 🎯 Próximos Pasos

Una vez configurado el targeting básico, puedes:

1. **Crear más cartas con objetivos**:
   - Cartas de curación (target: Player)
   - Cartas de robo forzado (target: Player)
   - Cartas de destrucción de tesoro (target: Treasure/Player)

2. **Agregar efectos visuales**:
   - Partículas al seleccionar objetivo
   - Sonidos de confirmación
   - Animaciones de impacto

3. **Mejorar feedback**:
   - Flechas apuntando desde la carta al objetivo
   - Preview del resultado antes de confirmar
   - Indicadores de rango/distancia

---

**¿Todo listo?** ¡Ahora tienes un sistema completo de targeting para tu tablero! 🎴✨
