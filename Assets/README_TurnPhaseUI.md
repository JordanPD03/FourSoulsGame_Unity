# 🔄 Sistema de Fases y Turnos - Four Souls

## 🎯 Descripción

Sistema completo de gestión de turnos basado en las reglas originales de Four Souls:

### **Fases del Turno:**
1. **🌟 Start** - Inicio del turno (efectos pasivos, ítems activos)
2. **📥 Draw** - Robo obligatorio de 1 carta de Loot
3. **⚡ Action** - Fase principal (comprar tesoros, atacar monstruos, jugar cartas, terminar turno)
4. **🏁 End** - Fin del turno (descarte por límite de mano, efectos de fin)

---

## 🛠️ Configuración en Unity

### **Paso 1: Crear la estructura UI**

En el Canvas, crea esta jerarquía:

```
Canvas/
└── TurnPhasePanel (Panel)
    ├── PhaseText (TextMeshProUGUI) ← Muestra fase actual
    ├── PlayerNameText (TextMeshProUGUI) ← Muestra jugador actual
    ├── DrawButton (Button) ← "Robar Carta"
    │   └── Text (TextMeshProUGUI) ← "Robar Carta"
    └── EndTurnButton (Button) ← "Terminar Turno"
        └── Text (TextMeshProUGUI) ← "Terminar Turno"
```

---

### **Paso 2: Configurar TurnPhasePanel**

**TurnPhasePanel:**
- Component: `Image` (Panel de fondo)
- Color: Blanco con alpha 200-255
- Posición sugerida: Esquina superior derecha o centro arriba
- Tamaño: 400x150 (ajusta según necesites)

**Ejemplo de posición:**
```
Anchor: Top-Right
Pos X: -220
Pos Y: -100
Width: 400
Height: 150
```

---

### **Paso 3: Configurar Textos**

**PhaseText** (Texto de fase):
```
Component: TextMeshProUGUI
Text: "Fase de Inicio\nEfectos de inicio de turno"
Font Size: 32
Alignment: Center (horizontal y vertical)
Color: Blanco o negro
Rich Text: ✅ Enabled (para usar <size>)
Auto Size: ✅ (Min: 20, Max: 40)
```

**PlayerNameText** (Nombre de jugador):
```
Component: TextMeshProUGUI
Text: "Player 1"
Font Size: 24
Alignment: Center
Color: Amarillo o blanco
```

---

### **Paso 4: Configurar Botones**

**DrawButton** (Botón de robo):
```
Component: Button
Transition: Color Tint
  Normal: Blanco
  Highlighted: Verde claro
  Pressed: Verde oscuro
  Disabled: Gris

Text hijo:
  Text: "Robar Carta"
  Font Size: 24
  Color: Negro o blanco
```

**EndTurnButton** (Botón de terminar turno):
```
Component: Button
Transition: Color Tint
  Normal: Blanco
  Highlighted: Amarillo claro
  Pressed: Amarillo oscuro

Text hijo:
  Text: "Terminar Turno"
  Font Size: 24
  Color: Negro
```

---

### **Paso 5: Adjuntar el Script**

1. Selecciona **TurnPhasePanel**
2. Add Component → Buscar **"Turn Phase UI"**
3. Configurar referencias en el Inspector:

```
Referencias de Texto:
  ✅ Phase Text: Arrastra PhaseText
  ✅ Player Name Text: Arrastra PlayerNameText

Botones de Fase:
  ✅ Draw Button: Arrastra DrawButton
  ✅ End Turn Button: Arrastra EndTurnButton

Configuración Visual:
  ✅ Start Phase Color: Azul (51, 153, 255)
  ✅ Draw Phase Color: Verde (77, 204, 77)
  ✅ Action Phase Color: Amarillo (255, 204, 51)
  ✅ End Phase Color: Rojo (204, 77, 77)

Panel de Fondo (Opcional):
  ✅ Background Panel: Arrastra TurnPhasePanel (Image)
```

---

## 🎮 Flujo del Turno

### **1️⃣ Fase Start (Automática)**
```
- Se ejecutan efectos de inicio de turno
- NO hay botones visibles
- Pasa automáticamente a fase Draw
```

### **2️⃣ Fase Draw**
```
- Robo AUTOMÁTICO de 1 carta de Loot (sin botones)
- El panel indicará "Robando 1 carta..."
- Al finalizar el robo, pasa automáticamente a fase Action
```

### **3️⃣ Fase Action**
```
- Botón "Terminar Turno" visible
- Player puede:
  ✅ Comprar tesoros (por implementar)
  ✅ Atacar monstruos (por implementar)
  ✅ Jugar cartas de Loot (por implementar)
  ✅ Terminar turno (botón)
```

### **4️⃣ Fase End (Automática)**
```
- Se verifica límite de mano (10 cartas)
- Se ejecutan efectos de fin de turno
- Pasa al siguiente jugador → Fase Start
```

---

## 🎨 Colores de Fase

Los colores del panel de fondo cambian según la fase:

| Fase | Color | Descripción |
|------|-------|-------------|
| **Start** | 🔵 Azul | Preparación |
| **Draw** | 🟢 Verde | Robo |
| **Action** | 🟡 Amarillo | Decisiones |
| **End** | 🔴 Rojo | Finalización |

---

## 🔍 Debugging

### **Logs en Consola:**
```
[GameManager] Phase changed to: Start
[GameManager] Processing start turn effects for Player 1
[GameManager] Phase changed to: Draw
[GameManager] Player 1 drew: Test Card 1
[GameManager] Phase changed to: Action
[TurnPhaseUI] Terminando turno...
[GameManager] Phase changed to: End
```

### **Teclas de Prueba:**
- **Espacio**: Robar carta (solo en fase Draw)
- **Botón GUI**: Robar carta (recomendado)
- **Botón GUI**: Terminar turno

---

## ⚙️ Personalización

### **Cambiar Mensajes de Fase:**

Edita `TurnPhaseUI.cs`, método `UpdatePhaseText()`:

```csharp
GamePhase.Start => "Inicio del Turno\n<size=70%>Activar efectos</size>",
GamePhase.Draw => "Robar Loot\n<size=70%>¡Saca una carta!</size>",
GamePhase.Action => "Tu Turno\n<size=70%>¿Qué harás?</size>",
```

### **Agregar Sonidos:**

En `TurnPhaseUI.cs`, en los event handlers:

```csharp
private void HandlePhaseChanged(GamePhase newPhase)
{
    // Reproducir sonido según fase
    AudioSource.PlayClipAtPoint(phaseChangeSFX, Camera.main.transform.position);
    
    UpdateUI();
}
```

---

## 🚀 Próximas Características

Una vez tengas el sistema de fases funcionando:

1. **Botón "Buy Treasure"** en fase Action
2. **Botón "Attack Monster"** en fase Action
3. **Sistema de descarte** en fase End (si tienes >10 cartas)
4. **Efectos de inicio/fin** de cartas específicas
5. **Multiplayer local** (turnos alternados entre jugadores)

---

## 🐛 Troubleshooting

### **Problema: Los botones no aparecen**
- ✅ Verifica que las referencias estén asignadas en el Inspector
- ✅ Verifica que GameManager existe en la escena
- ✅ Verifica que la fase actual es la correcta (usa Debug.Log)

### **Problema: No puedo robar carta**
- ✅ Verifica que estés en fase Draw (`currentPhase == GamePhase.Draw`)
- ✅ Verifica que `hasDrawnCard == false`
- ✅ Verifica que hay cartas en el mazo

### **Problema: El color no cambia**
- ✅ Asigna el `Background Panel` en el Inspector
- ✅ Verifica que el Image del panel está activo

### **Problema: "Ya robaste una carta" pero no robé**
- ✅ El evento `OnCardDrawn` se dispara antes de tiempo
- ✅ Verifica que solo llamas `TryDrawCardWithAnimation()` una vez

---

## 📊 Ejemplo Visual Final

```
┌─────────────────────────────────────┐
│          FASE DE ACCIÓN             │  ← PhaseText (grande)
│     Compra, ataca o termina turno   │  ← Descripción (pequeña)
│                                     │
│          Player 1                   │  ← PlayerNameText
│                                     │
│     [ Terminar Turno ]             │  ← EndTurnButton (visible solo en Action)
└─────────────────────────────────────┘
     Fondo: Amarillo (Action Phase)
```

---

¡Listo! 🎉 Ahora tienes un sistema completo de fases y turnos.
