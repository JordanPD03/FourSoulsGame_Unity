# 🎲 Sistema de Ataque al Mazo de Monstruos

## 📋 Descripción

Este sistema permite a los jugadores atacar directamente la carta superior del mazo de Monstruos y colocarla como **overlay** (superposición) sobre un slot de monstruo existente.

---

## 🎮 Flujo de Juego

### 1️⃣ Click en el Mazo de Monstruos
- Durante tu fase de acción, haz **click en el mazo de Monstruos** del tablero
- Esto revelará la carta superior del mazo
- El juego te pedirá que elijas un slot donde colocarla

**Requisitos:**
- Estar en una fase que permita atacar (Action)
- Tener al menos 1 ataque disponible

---

### 2️⃣ Selección de Slot
- Aparecerá un mensaje: **"Elige un slot de monstruo para colocar encima"**
- El temporizador de turno se pausará mientras eliges
- Haz **click en cualquier slot de monstruo** para confirmar

---

### 3️⃣ Colocación del Overlay
- El monstruo revelado se **coloca encima** del monstruo actual del slot
- Se reproduce una **animación de entrada** (caída + rebote)
- El overlay se convierte en el **monstruo activo** del slot
- El monstruo original permanece **debajo** como backup

---

### 4️⃣ Preview del Monstruo
- Automáticamente se muestra el **preview** del monstruo overlay
- Puedes ver sus estadísticas (vida, ataque, dado, recompensas)
- El preview se cierra automáticamente después de ~1.2 segundos

---

### 5️⃣ Combate Automático
- El **combate inicia automáticamente** contra el monstruo overlay
- El **dado se lanza** sin necesidad de presionar nada
- El sistema de combate normal continúa (tiradas sucesivas con doble-click)

---

## 🃏 Comportamiento del Overlay

### ¿Qué es un Overlay?
Un overlay es un monstruo colocado **encima** de otro monstruo en el mismo slot:

```
┌─────────────────┐
│  OVERLAY        │ ← Monstruo activo (en combate)
│  (Top Card)     │
├─────────────────┤
│  BASE MONSTER   │ ← Monstruo oculto (esperando debajo)
│  (Hidden)       │
└─────────────────┘
```

### Reglas del Overlay

| Situación | Resultado |
|-----------|-----------|
| **Derrotas el overlay** | El overlay desaparece, el monstruo base queda activo en el slot |
| **No derrotas el overlay** | El overlay permanece activo hasta ser derrotado o reemplazado |
| **Colocas otro overlay** | El overlay anterior se reemplaza por el nuevo |
| **Slot sin monstruo base** | El overlay funciona como monstruo normal |

---

## 🎯 Ventajas Estratégicas

✅ **Evitar monstruos difíciles**: Coloca un overlay sobre un monstruo peligroso para enfrentarlo después  
✅ **Guardar monstruos fáciles**: Protege monstruos con buenas recompensas colocando overlays encima  
✅ **Control del tablero**: Decide qué monstruos están disponibles para otros jugadores  

---

## 🛠️ Configuración en Unity

### Paso 1: Configurar el Mazo de Monstruos

1. Localiza el objeto **MonsterDeck** en la jerarquía de tu escena
2. Agrega el componente **`MonsterDeckUI`**:

```
┌─────────────────────────────────────┐
│ MONSTER DECK UI                     │
├─────────────────────────────────────┤
│ Deck Renderer: (asignar el         │
│                 SpriteRenderer      │
│                 del dorso)          │
│ Add Collider If Missing: ✓          │
│ Enable Hover: ✓                     │
│ Hover Scale: 1.05                   │
│ Hover Tween Duration: 0.1           │
└─────────────────────────────────────┘
```

3. Asegúrate de que el objeto tenga un **SpriteRenderer** con el sprite del dorso del mazo
4. El componente añadirá automáticamente un **BoxCollider2D** para recibir clicks

---

### Paso 2: Verificar MonsterSlotManager

Asegúrate de que tu escena tenga un **MonsterSlotManager** configurado:

```
┌─────────────────────────────────────┐
│ MONSTER SLOT MANAGER                │
├─────────────────────────────────────┤
│ Monster Card Prefab: (prefab       │
│                       CardUI)       │
│ Monster Slots: (lista de slots)    │
└─────────────────────────────────────┘
```

---

### Paso 3: Verificar GameManager

El `GameManager` ya tiene los métodos necesarios:
- `BeginAttackDeckTop(PlayerData)` → Llamado por MonsterDeckUI al hacer click
- `ConfirmDeckOverlayPlacement(MonsterSlot)` → Llamado al seleccionar slot
- `IsAwaitingDeckOverlayPlacement()` → Estado de selección activo

---

## 🧪 Pruebas

### Test 1: Ataque Básico al Mazo
1. Inicia el juego y espera a la fase Action
2. Click en el **MonsterDeck**
3. Verás el mensaje: "Elige un slot de monstruo para colocar encima"
4. Click en un slot
5. Deberías ver:
   - Animación de colocación del overlay
   - Preview del monstruo (~1.2s)
   - Lanzamiento automático del dado
   - Combate iniciado

---

### Test 2: Derrotar Overlay
1. Completa el combate y derrota el overlay
2. El monstruo base debería quedar activo en el slot
3. Puedes atacarlo normalmente (doble-click)

---

### Test 3: No Derrotar Overlay
1. Inicia combate contra overlay
2. No logres derrotarlo (pierde el combate)
3. El overlay debería permanecer en el slot
4. El monstruo base sigue oculto debajo

---

## 📝 Notas Técnicas

- El overlay se renderiza con **sorting order 20** (por encima de la base = 10)
- Las animaciones de combate (daño, ataque, trigger) se aplican al overlay
- Los textos de estadísticas se vinculan automáticamente al overlay
- El sistema de preview y double-click funcionan normalmente con overlays

---

## 🐛 Troubleshooting

### "No puedes atacar durante la fase..."
- Verifica que estés en fase Action
- Asegúrate de tener al menos 1 ataque disponible

### "No hay MonsterSlotManager para colocar overlay"
- Verifica que MonsterSlotManager esté en la escena con tag correcto
- Confirma que esté inicializado antes de atacar el mazo

### El click en el mazo no funciona
- Verifica que MonsterDeckUI esté asignado al objeto
- Confirma que el objeto tenga un BoxCollider2D
- Asegúrate de que el objeto no esté detrás de otro con collider

### El overlay no aparece
- Verifica que MonsterSlotManager tenga el prefab de carta asignado
- Confirma que el mazo de Monstruos no esté vacío
- Revisa la consola para errores de instanciación

---

¡Listo para estrategias avanzadas! 🚀
