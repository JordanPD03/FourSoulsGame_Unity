# 🎺 Turn Announcer UI ("Turno de <Jugador>")

Muestra un banner centrado al comienzo del turno con un pequeño efecto de zoom y desvanecimiento.

## ✨ Resultado esperado
- Texto en el centro: "Turno de Player 1"
- Aparece con fade in y zoom suave
- Se mantiene ~1s
- Desaparece con fade out

---

## 🛠️ Configuración en la escena

1) En tu Canvas, crea esta jerarquía:
```
Canvas/
└── TurnAnnouncer (Empty or Panel)
    ├── (Add Component) CanvasGroup
    └── Text (TextMeshProUGUI)
```

2) Ajusta el RectTransform del objeto `TurnAnnouncer`:
```
Anchor: Middle Center
Pos X/Y: 0 / 0
Width/Height: 1000 x 200   (ajústalo a tu gusto)
```

3) Configura el `Text (TextMeshProUGUI)`:
```
Text: "Turno de Player 1"
Font Size: 64 (o Auto Size 40-80)
Alignment: Center / Middle
Color: Blanco (con contorno negro opcional)
Rich Text: ✅
```
Tip: Puedes agregar sombra (TMP: Material con Outline) o un panel de fondo semitransparente si quieres más contraste.

4) Agrega el script `TurnAnnouncerUI.cs` al objeto `TurnAnnouncer` y asigna referencias:
```
TurnAnnouncerUI (Component):
  - Turn Text: (arrastra Text TMP)
  - Canvas Group: (arrastra el CanvasGroup del mismo objeto)
  - Container: (arrastra el RectTransform del mismo objeto)

Apariencia (valores sugeridos):
  - Message Format: "Turno de {0}"
  - Fade In: 0.25
  - Punch Duration: 0.25
  - Hold: 0.9
  - Fade Out: 0.45
  - Start Scale: 0.85
  - Punch Scale: 1.12
```

5) Asegúrate de tener un `GameManager` en la escena (singleton) con jugadores cargados.

---

## ▶️ ¿Cuándo se reproduce?
- Automáticamente al iniciar la partida (si `Play On Game Start` está activo)
- Automáticamente cada vez que cambia el turno (`GameManager.OnPlayerTurnChanged`)

---

## 🧪 Prueba rápida
- Dale Play a la escena
- Usa el botón "Terminar Turno" en la UI de Fases (o el flujo normal)
- Deberías ver el banner: "Turno de Player X" en cada cambio

---

## ⚙️ Personalización
- Cambia el `Message Format` a algo como: "Le toca a {0}!" o "Turn for {0}"
- Ajusta `Start Scale` y `Punch Scale` para más/menos impacto
- Añade un `Image` de fondo al objeto `TurnAnnouncer` para un recuadro translúcido
- Cambia colores de texto por personaje o por jugador

---

## 🐛 Troubleshooting
- No aparece nada: verifica referencias (Text, CanvasGroup, Container)
- No cambia el nombre: asegúrate que `PlayerData.playerName` está configurado
- No se llama el evento: `GameManager.StartPlayerTurn` debe invocar `OnPlayerTurnChanged`
- Error de DOTween: asegúrate que DOTween está instalado (el proyecto ya lo usa en `GameManager`)

---

¡Listo! Tu juego ahora anuncia estilosamente de quién es el turno. 🎉
